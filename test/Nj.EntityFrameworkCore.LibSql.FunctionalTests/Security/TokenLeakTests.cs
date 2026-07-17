using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nj.EntityFrameworkCore.LibSql;
using Nj.LibSql.Data;
using Nj.LibSql.Data.Exceptions;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Security;

public class TokenLeakTests
{
    private const string Secret = "super-secret-token-value-wp13";

    [Fact]
    public void Open_failure_exception_ToString_does_not_leak_token()
    {
        using var connection = new LibSqlConnection(
            $"Data Source=http://127.0.0.1:1;Auth Token={Secret}");
        var ex = Assert.ThrowsAny<Exception>(() => connection.Open());
        Assert.DoesNotContain(Secret, ex.ToString(), StringComparison.Ordinal);
        if (ex is LibSqlConnectionException connectionException)
        {
            Assert.DoesNotContain(Secret, connectionException.ConnectionString ?? "", StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Ef_LogFragment_does_not_leak_token()
    {
        var options = new DbContextOptionsBuilder<LeakDbContext>()
            .UseLibSql($"Data Source=:memory:;Auth Token={Secret}")
            .Options;

        var extension = options.Extensions.OfType<Infrastructure.Internal.LibSqlOptionsExtension>().Single();
        Assert.DoesNotContain(Secret, extension.Info.LogFragment, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ef_default_command_logging_does_not_leak_parameter_values()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-leak-" + Guid.NewGuid().ToString("N") + ".db");
        var log = new StringBuilder();
        try
        {
            await using var context = new LeakDbContext(
                new DbContextOptionsBuilder<LeakDbContext>()
                    .UseLibSql($"Data Source={path}")
                    .LogTo(s => log.AppendLine(s), LogLevel.Information)
                    // Default: sensitive data logging OFF — parameter values must not appear.
                    .Options);

            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            context.Items.Add(new LeakItem { Name = Secret });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.DoesNotContain(Secret, log.ToString(), StringComparison.Ordinal);
        }
        finally
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
                // best-effort
            }
        }
    }

    private sealed class LeakDbContext(DbContextOptions<LeakDbContext> options) : DbContext(options)
    {
        public DbSet<LeakItem> Items => Set<LeakItem>();
    }

    private sealed class LeakItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
