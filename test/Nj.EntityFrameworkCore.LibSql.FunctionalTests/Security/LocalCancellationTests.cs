using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Security;

public class LocalCancellationTests
{
    [Fact]
    public async Task Pre_cancelled_token_throws_before_local_execute()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-cancel-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using var connection = new LibSqlConnection($"Data Source={path}");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => command.ExecuteScalarAsync(cts.Token));
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
}
