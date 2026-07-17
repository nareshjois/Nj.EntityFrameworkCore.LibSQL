using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.ConnectionModes;

public sealed class LocalConnectionModeTests
{
    [Fact]
    public async Task File_crud_round_trip()
    {
        var path = TempDbPath();
        try
        {
            await using (var context = CreateContext(path))
            {
                await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                context.Items.Add(new ModeItem { Name = "alpha" });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using (var context = CreateContext(path))
            {
                Assert.Equal("alpha", await context.Items.Select(i => i.Name).SingleAsync(
                    TestContext.Current.CancellationToken));
            }

            Assert.True(File.Exists(path));
        }
        finally
        {
            Cleanup(path);
        }
    }

    [Fact]
    public async Task Memory_databases_are_isolated()
    {
        await using (var first = CreateContext(":memory:"))
        {
            await first.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            try
            {
                await first.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                first.Items.Add(new ModeItem { Name = "only-first" });
                await first.SaveChangesAsync(TestContext.Current.CancellationToken);
                Assert.Equal(1, await first.Items.CountAsync(TestContext.Current.CancellationToken));
            }
            finally
            {
                await first.Database.CloseConnectionAsync();
            }
        }

        await using var second = CreateContext(":memory:");
        await second.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await second.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            Assert.Empty(await second.Items.ToListAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            await second.Database.CloseConnectionAsync();
        }
    }

    [Fact]
    public async Task Missing_directory_fails_clearly()
    {
        var missingDir = Path.Combine(Path.GetTempPath(), "nj-missing-" + Guid.NewGuid().ToString("N"), "nope.db");
        await using var context = CreateContext(missingDir);
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public async Task Multi_context_same_file_interleaved_writes()
    {
        var path = TempDbPath();
        try
        {
            await using (var bootstrap = CreateContext(path))
            {
                await bootstrap.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            }

            await using var a = CreateContext(path);
            await using var b = CreateContext(path);
            a.Items.Add(new ModeItem { Name = "a" });
            await a.SaveChangesAsync(TestContext.Current.CancellationToken);
            b.Items.Add(new ModeItem { Name = "b" });
            await b.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, await a.Items.CountAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            Cleanup(path);
        }
    }

    [Fact]
    public async Task Pooled_factory_reuses_and_clear_pool_after_ensure_deleted()
    {
        var path = TempDbPath();
        try
        {
            var services = new ServiceCollection()
                .AddDbContextPool<ModeDbContext>(o => o.UseLibSql($"Data Source={path}"))
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ModeDbContext>();
                await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                context.Items.Add(new ModeItem { Name = "pooled" });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ModeDbContext>();
                Assert.Equal(1, await context.Items.CountAsync(TestContext.Current.CancellationToken));
                await context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
                Assert.False(await context.Database.CanConnectAsync(TestContext.Current.CancellationToken)
                    && await context.Items.AnyAsync(TestContext.Current.CancellationToken));
            }

            LibSqlConnection.ClearPool(new LibSqlConnection($"Data Source={path}"));
        }
        finally
        {
            Cleanup(path);
        }
    }

    private static ModeDbContext CreateContext(string dataSource)
        => new(new DbContextOptionsBuilder<ModeDbContext>()
            .UseLibSql($"Data Source={dataSource}")
            .Options);

    private static string TempDbPath()
        => Path.Combine(Path.GetTempPath(), "nj-modes-local-" + Guid.NewGuid().ToString("N") + ".db");

    private static void Cleanup(string path)
    {
        try
        {
            LibSqlConnection.ClearPool(new LibSqlConnection($"Data Source={path}"));
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best-effort
        }
    }
}
