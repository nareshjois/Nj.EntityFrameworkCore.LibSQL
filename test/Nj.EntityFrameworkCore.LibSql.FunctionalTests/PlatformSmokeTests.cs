using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

/// <summary>
/// WP-12 Preview G11: local open + short EF smoke on the runner RID
/// (<c>linux-x64</c> / <c>win-x64</c> in CI; <c>osx-arm64</c> maintainer-validated).
/// </summary>
public sealed class PlatformSmokeTests
{
    [Fact]
    public void Driver_opens_memory_and_selects_one()
    {
        using var connection = new LibSqlConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [Fact]
    public async Task Ef_local_file_ensure_created_and_save_changes()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "nj-platform-smoke-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using var context = new SmokeContext(
                new DbContextOptionsBuilder<SmokeContext>()
                    .UseLibSql($"Data Source={path}")
                    .Options);

            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            context.Items.Add(new SmokeItem { Name = "platform-" + RuntimeInformation.OSDescription });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, await context.Items.CountAsync(TestContext.Current.CancellationToken));
        }
        finally
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

    private sealed class SmokeContext(DbContextOptions<SmokeContext> options) : DbContext(options)
    {
        public DbSet<SmokeItem> Items => Set<SmokeItem>();
    }

    private sealed class SmokeItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
