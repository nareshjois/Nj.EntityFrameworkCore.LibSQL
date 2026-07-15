using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task AddDbContext_can_select_one()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-di-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var services = new ServiceCollection();
            services.AddDbContext<SmokeDbContext>(o => o.UseLibSql($"Data Source={path}"));
            await using var provider = services.BuildServiceProvider(validateScopes: true);
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<SmokeDbContext>();

            await AssertSelectOneAsync(context);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task AddDbContextFactory_can_select_one()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-fac-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var services = new ServiceCollection();
            services.AddDbContextFactory<SmokeDbContext>(o => o.UseLibSql($"Data Source={path}"));
            await using var provider = services.BuildServiceProvider(validateScopes: true);
            var factory = provider.GetRequiredService<IDbContextFactory<SmokeDbContext>>();
            await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

            await AssertSelectOneAsync(context);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task AddPooledDbContextFactory_can_select_one()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-pool-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var services = new ServiceCollection();
            services.AddPooledDbContextFactory<SmokeDbContext>(o => o.UseLibSql($"Data Source={path}"));
            await using var provider = services.BuildServiceProvider(validateScopes: true);
            var factory = provider.GetRequiredService<IDbContextFactory<SmokeDbContext>>();
            await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

            await AssertSelectOneAsync(context);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void UseSpatialite_throws_not_supported()
    {
        Assert.Throws<NotSupportedException>(() =>
            new DbContextOptionsBuilder()
                .UseLibSql("Data Source=:memory:", b => b.UseSpatialite()));
    }

    private static async Task AssertSelectOneAsync(DbContext context)
    {
        _ = context.Model;
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1L, Convert.ToInt64(result));
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort.
        }
    }

    private sealed class SmokeDbContext(DbContextOptions<SmokeDbContext> options) : DbContext(options);
}
