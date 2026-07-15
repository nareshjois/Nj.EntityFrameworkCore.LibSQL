using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

public sealed class LocalSelectOneTests
{
    [Fact]
    public async Task UseLibSql_local_file_can_execute_select_one()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-func-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using var context = new SmokeDbContext(
                new DbContextOptionsBuilder<SmokeDbContext>()
                    .UseLibSql($"Data Source={path}")
                    .Options);

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
                // Best-effort cleanup.
            }
        }
    }

    private sealed class SmokeDbContext(DbContextOptions<SmokeDbContext> options) : DbContext(options);
}
