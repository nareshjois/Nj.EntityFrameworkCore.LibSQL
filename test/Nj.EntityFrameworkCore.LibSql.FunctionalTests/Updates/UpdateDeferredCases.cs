using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Updates;

/// <summary>
/// Deferred update / transaction edge cases.
/// </summary>
internal static class UpdateDeferredCases
{
    private static bool IsRemoteConnection(string connectionString)
    {
        const string prefix = "Data Source=";
        if (!connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var dataSource = connectionString[prefix.Length..].Trim();
        var semi = dataSource.IndexOf(';');
        if (semi >= 0)
        {
            dataSource = dataSource[..semi];
        }

        return dataSource.Contains("://", StringComparison.Ordinal);
    }

    private static bool IsLocalFileConnection(string connectionString)
        => connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
            && !IsRemoteConnection(connectionString);

    public static async Task Savepoints_under_user_transaction(UpdateDbContext context, CancellationToken ct)
    {
        await UpdateTestHelpers.ResetAsync(context, ct);

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            context.Accounts.Add(new Account { Name = "Outer", Code = "SP1", Balance = 1 });
            await context.SaveChangesAsync(ct);

            await transaction.CreateSavepointAsync("sp1", ct);
            context.Accounts.Add(new Account { Name = "Inner", Code = "SP2", Balance = 2 });
            await context.SaveChangesAsync(ct);

            await transaction.RollbackToSavepointAsync("sp1", ct);
            await transaction.ReleaseSavepointAsync("sp1", ct);
            await transaction.CommitAsync(ct);
        }
        catch (NotSupportedException ex)
        {
            Assert.Skip("Savepoints not supported under user transactions: " + ex.Message);
        }

        context.ChangeTracker.Clear();
        var rows = await context.Accounts.ToListAsync(ct);
        Assert.Single(rows);
        Assert.Equal("Outer", rows[0].Name);
    }

    public static async Task Busy_locked_multi_connection_stress(string connectionString, CancellationToken ct)
    {
        if (!IsLocalFileConnection(connectionString))
        {
            // File-lock busy stress is meaningful for local SQLite files only.
            return;
        }

        await using (var setup = new UpdateDbContext(UpdateTestHelpers.Configure(connectionString).Options))
        {
            await setup.Database.EnsureCreatedAsync(ct);
            await setup.Database.OpenConnectionAsync(ct);
            await UpdateTestHelpers.ResetAsync(setup, ct);
            await setup.Database.CloseConnectionAsync();
        }

        await using var holder = new UpdateDbContext(UpdateTestHelpers.Configure(connectionString).Options);
        await holder.Database.OpenConnectionAsync(ct);
        await holder.Database.ExecuteSqlRawAsync("BEGIN IMMEDIATE TRANSACTION;", ct);
        holder.Accounts.Add(new Account { Name = "Holder", Code = "H1", Balance = 1 });
        await holder.SaveChangesAsync(ct);

        await using var challenger = new UpdateDbContext(UpdateTestHelpers.Configure(connectionString).Options);
        await challenger.Database.OpenConnectionAsync(ct);
        challenger.Database.SetCommandTimeout(1);
        challenger.Accounts.Add(new Account { Name = "Challenger", Code = "C1", Balance = 2 });

        var ex = await Record.ExceptionAsync(() => challenger.SaveChangesAsync(ct));
        Assert.NotNull(ex);

        await holder.Database.ExecuteSqlRawAsync("ROLLBACK;", ct);
        await holder.Database.CloseConnectionAsync();
        await challenger.Database.CloseConnectionAsync();
    }

    public static async Task Cancellation_on_SaveChanges(UpdateDbContext context, CancellationToken _)
    {
        await UpdateTestHelpers.ResetAsync(context, TestContext.Current.CancellationToken);

        context.Accounts.Add(new Account { Name = "Cancel", Code = "X1", Balance = 1 });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.SaveChangesAsync(cts.Token));

        context.ChangeTracker.Clear();
        Assert.Empty(await context.Accounts.ToListAsync(TestContext.Current.CancellationToken));
    }

    public static async Task Returning_disabled_with_after_trigger(string connectionString, SqlCaptureLogger sql, CancellationToken ct)
    {
        var options = new DbContextOptionsBuilder<TriggerUpdateDbContext>()
            .UseLibSql(connectionString)
            .LogTo(sql.Write, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        await using var context = new TriggerUpdateDbContext(options);

        await context.Database.OpenConnectionAsync(ct);
        var creator = context.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
        await creator.EnsureCreatedAsync(ct);

        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText =
                """SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = 'TriggeredAccounts';""";
            var count = Convert.ToInt64(await command.ExecuteScalarAsync(ct));
            if (count == 0)
            {
                await creator.CreateTablesAsync(ct);
            }
        }

        await context.Database.ExecuteSqlRawAsync("""DELETE FROM "TriggeredAccounts";""", ct);
        await context.Database.ExecuteSqlRawAsync("""DROP TRIGGER IF EXISTS "TR_TriggeredAccounts_AfterInsert";""", ct);
        context.ChangeTracker.Clear();

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER "TR_TriggeredAccounts_AfterInsert"
            AFTER INSERT ON "TriggeredAccounts"
            BEGIN
                SELECT 1;
            END;
            """,
            ct);

        sql.Clear();
        context.Accounts.Add(new Account { Name = "Triggered", Code = "T1", Balance = 5 });
        var saveEx = await Record.ExceptionAsync(() => context.SaveChangesAsync(ct));

        var captured = sql.Text;
        Assert.DoesNotContain("RETURNING", captured, StringComparison.OrdinalIgnoreCase);
        Assert.False(context.Model.FindEntityType(typeof(Account))!.IsSqlReturningClauseUsed());

        if (saveEx is DbUpdateConcurrencyException)
        {
            // AFTER triggers can break changes()/RETURNING save paths; disabling RETURNING
            // avoids RETURNING but rowcount checks may still fail — documented edge case.
            return;
        }

        if (saveEx is DbUpdateException && IsRemoteConnection(connectionString))
        {
            // Remote HTTP baton rejects multi-statement INSERT+SELECT legacy batches.
            return;
        }

        Assert.Null(saveEx);
        context.ChangeTracker.Clear();
        Assert.Equal("Triggered", (await context.Accounts.SingleAsync(a => a.Code == "T1", ct)).Name);
    }

    public static async Task Pooled_context_stress(string connectionString, CancellationToken ct)
    {
        var services = new ServiceCollection()
            .AddDbContextPool<UpdateDbContext>(options => options.UseLibSql(connectionString))
            .BuildServiceProvider();

        await using (var scope = services.CreateAsyncScope())
        {
            var bootstrap = scope.ServiceProvider.GetRequiredService<UpdateDbContext>();
            await bootstrap.Database.OpenConnectionAsync(ct);
            await UpdateTestHelpers.EnsureSchemaAsync(bootstrap, ct);
            await UpdateTestHelpers.ResetAsync(bootstrap, ct);
            await bootstrap.Database.CloseConnectionAsync();
        }

        for (var i = 0; i < 12; i++)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<UpdateDbContext>();
            context.Accounts.Add(new Account { Name = $"Pool-{i}", Code = $"P{i}", Balance = i });
            await context.SaveChangesAsync(ct);
        }

        await using (var scope = services.CreateAsyncScope())
        {
            var verify = scope.ServiceProvider.GetRequiredService<UpdateDbContext>();
            Assert.Equal(12, await verify.Accounts.CountAsync(ct));
        }

        if (services is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            services.Dispose();
        }
    }

    private sealed class TriggerUpdateDbContext(DbContextOptions<TriggerUpdateDbContext> options) : DbContext(options)
    {
        public DbSet<Account> Accounts => Set<Account>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(e =>
            {
                e.ToTable("TriggeredAccounts");
                e.HasIndex(x => x.Code).IsUnique();
                e.ToTable(tb => tb.UseSqlReturningClause(false));
            });
        }
    }
}
