using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Updates;

/// <summary>
/// Shared update / transaction matrix exercised by local and remote hosts.
/// Assertions check store state after Clear / new context where required.
/// </summary>
internal static class UpdateCases
{
    public static async Task Crud_SaveChanges(UpdateDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        await UpdateTestHelpers.ResetAsync(context, ct);
        sql.Clear();

        var account = new Account { Name = "Ada", Code = "A1", Balance = 100 };
        context.Accounts.Add(account);
        Assert.Equal(1, await context.SaveChangesAsync(ct));
        Assert.True(account.Id > 0);
        sql.AssertContainsSql("INSERT", "RETURNING");

        context.ChangeTracker.Clear();
        var loaded = await context.Accounts.SingleAsync(ct);
        Assert.Equal("Ada", loaded.Name);
        Assert.Equal(100, loaded.Balance);

        sql.Clear();
        loaded.Balance = 150;
        Assert.Equal(1, await context.SaveChangesAsync(ct));
        sql.AssertContainsSql("UPDATE");

        context.ChangeTracker.Clear();
        Assert.Equal(150, (await context.Accounts.SingleAsync(ct)).Balance);

        sql.Clear();
        context.Accounts.Remove(await context.Accounts.SingleAsync(ct));
        Assert.Equal(1, await context.SaveChangesAsync(ct));
        sql.AssertContainsSql("DELETE");

        context.ChangeTracker.Clear();
        Assert.Empty(await context.Accounts.ToListAsync(ct));
    }

    public static async Task Multi_entity_auto_transaction_rolls_back(UpdateDbContext context, SqlCaptureLogger _, CancellationToken ct)
    {
        await UpdateTestHelpers.ResetAsync(context, ct);

        context.Accounts.Add(new Account { Name = "One", Code = "DUP", Balance = 1 });
        context.Accounts.Add(new Account { Name = "Two", Code = "DUP", Balance = 2 });

        await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync(ct));

        context.ChangeTracker.Clear();
        Assert.Empty(await context.Accounts.ToListAsync(ct));
    }

    public static async Task Explicit_transaction_commit(UpdateDbContext context, SqlCaptureLogger _, CancellationToken ct)
    {
        await UpdateTestHelpers.ResetAsync(context, ct);

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        context.Accounts.Add(new Account { Name = "Committed", Code = "C1", Balance = 10 });
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        context.ChangeTracker.Clear();
        Assert.Equal("Committed", (await context.Accounts.SingleAsync(ct)).Name);
    }

    public static async Task Explicit_transaction_rollback(UpdateDbContext context, SqlCaptureLogger _, CancellationToken ct)
    {
        await UpdateTestHelpers.ResetAsync(context, ct);

        await using (var tx = await context.Database.BeginTransactionAsync(ct))
        {
            context.Accounts.Add(new Account { Name = "Rolled", Code = "R1", Balance = 10 });
            await context.SaveChangesAsync(ct);
            await tx.RollbackAsync(ct);
        }

        context.ChangeTracker.Clear();
        Assert.Empty(await context.Accounts.ToListAsync(ct));
    }

    public static async Task Optimistic_concurrency_conflict(UpdateDbContext context, SqlCaptureLogger _, CancellationToken ct)
    {
        await UpdateTestHelpers.ResetAsync(context, ct);

        context.VersionedItems.Add(new VersionedItem { Name = "v0", Version = 1 });
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();

        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Expected a connection string.");

        // Release the host connection so sibling contexts can open the same local file.
        await context.Database.CloseConnectionAsync();

        await using var winner = new UpdateDbContext(UpdateTestHelpers.Configure(connectionString).Options);
        await using var loser = new UpdateDbContext(UpdateTestHelpers.Configure(connectionString).Options);
        await winner.Database.OpenConnectionAsync(ct);
        await loser.Database.OpenConnectionAsync(ct);
        try
        {
            var a = await winner.VersionedItems.SingleAsync(ct);
            var b = await loser.VersionedItems.SingleAsync(ct);

            a.Name = "winner";
            a.Version++;
            Assert.Equal(1, await winner.SaveChangesAsync(ct));

            b.Name = "loser";
            b.Version++;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => loser.SaveChangesAsync(ct));

            await using var verify = new UpdateDbContext(UpdateTestHelpers.Configure(connectionString).Options);
            await verify.Database.OpenConnectionAsync(ct);
            try
            {
                var stored = await verify.VersionedItems.SingleAsync(ct);
                Assert.Equal("winner", stored.Name);
                Assert.Equal(2, stored.Version);
            }
            finally
            {
                await verify.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            await winner.Database.CloseConnectionAsync();
            await loser.Database.CloseConnectionAsync();
            await context.Database.OpenConnectionAsync(ct);
        }
    }

    public static async Task ExecuteUpdate_and_ExecuteDelete(UpdateDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        await UpdateTestHelpers.ResetAsync(context, ct);

        context.Accounts.AddRange(
            new Account { Name = "A", Code = "E1", Balance = 10 },
            new Account { Name = "B", Code = "E2", Balance = 20 });
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();

        sql.Clear();
        var updated = await context.Accounts
            .Where(a => a.Code == "E1")
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, 99), ct);
        Assert.Equal(1, updated);
        sql.AssertContainsSql("UPDATE");

        context.ChangeTracker.Clear();
        Assert.Equal(99, (await context.Accounts.SingleAsync(a => a.Code == "E1", ct)).Balance);

        sql.Clear();
        var deleted = await context.Accounts
            .Where(a => a.Balance == 20)
            .ExecuteDeleteAsync(ct);
        Assert.Equal(1, deleted);
        sql.AssertContainsSql("DELETE");

        context.ChangeTracker.Clear();
        Assert.Single(await context.Accounts.ToListAsync(ct));
        Assert.Equal("E1", (await context.Accounts.SingleAsync(ct)).Code);
    }

    public static async Task Constraint_violation_after_prior_commit_isolated(
        UpdateDbContext context,
        SqlCaptureLogger _,
        CancellationToken ct)
    {
        await UpdateTestHelpers.ResetAsync(context, ct);

        context.Accounts.Add(new Account { Name = "Keep", Code = "K1", Balance = 1 });
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();

        context.Accounts.Add(new Account { Name = "Clash", Code = "K1", Balance = 2 });
        await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync(ct));

        context.ChangeTracker.Clear();
        var kept = await context.Accounts.ToListAsync(ct);
        Assert.Single(kept);
        Assert.Equal("Keep", kept[0].Name);
        Assert.Equal("K1", kept[0].Code);
    }
}
