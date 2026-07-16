using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Nj.LibSql.Data;
using Nj.LibSql.DriverContractTests.Infrastructure;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

/// <summary>
/// WSS hard gate against self-hosted sqld (Testcontainers).
/// Turso Cloud returns <c>protocol upgrade not supported (websocket)</c>.
/// </summary>
[Collection(RemoteDriverCollection.Name)]
public sealed class RemoteWssTests
{
    private const int LargeRowCount = 5_000;

    private readonly RemoteDriverFixture _fixture;

    public RemoteWssTests(RemoteDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public void Sqld_select_one_over_websocket()
    {
        EnsureWsAvailable();

        using var connection = new LibSqlConnection(_fixture.CreateWsConnectionString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
    }

    /// <summary>
    /// Hard gate: large SELECT via Hrana cursor streaming over WebSocket.
    /// </summary>
    [Fact]
    public void Sqld_large_result_over_websocket_cursor()
    {
        EnsureWsAvailable();

        using var connection = new LibSqlConnection(_fixture.CreateWsConnectionString());
        connection.Open();
        var table = "ws_large_" + Guid.NewGuid().ToString("N")[..8];

        using (var setup = connection.CreateCommand())
        {
            setup.CommandText =
                $"CREATE TABLE {table}(id INTEGER PRIMARY KEY, payload TEXT NOT NULL)";
            setup.ExecuteNonQuery();
        }

        const int batchSize = 250;
        for (var start = 1; start <= LargeRowCount; start += batchSize)
        {
            var end = Math.Min(start + batchSize - 1, LargeRowCount);
            using var insert = connection.CreateCommand();
            var values = string.Join(
                ",",
                Enumerable.Range(start, end - start + 1)
                    .Select(i => $"({i}, 'row-{i}-" + new string('x', 64) + "')"));
            insert.CommandText = $"INSERT INTO {table}(id, payload) VALUES {values}";
            insert.CommandTimeout = 120;
            insert.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandTimeout = 180;
            command.CommandText = $"SELECT id, payload FROM {table} ORDER BY id";
            using var reader = command.ExecuteReader();
            var count = 0;
            long expectedId = 1;
            while (reader.Read())
            {
                Assert.Equal(expectedId, reader.GetInt64(0));
                Assert.False(reader.IsDBNull(1));
                expectedId++;
                count++;
            }

            Assert.Equal(LargeRowCount, count);
        }
    }

    private void EnsureWsAvailable()
    {
        if (!_fixture.IsAvailable)
        {
            if (TestEnvironment.RemoteTestsRequired)
            {
                Assert.Fail(
                    "Remote sqld required but unavailable: "
                    + (_fixture.UnavailableReason ?? "unknown"));
            }

            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown"));
        }

        if (!_fixture.IsWebSocketAvailable)
        {
            if (TestEnvironment.RemoteTestsRequired)
            {
                Assert.Fail(
                    "WebSocket endpoint required (Testcontainers sqld) but unavailable. "
                    + "External LIBSQL_TEST_URL endpoints are HTTP-only.");
            }

            Assert.Skip(
                "WebSocket endpoint not available (external HTTP-only URL). "
                + "Unset LIBSQL_TEST_URL to use Testcontainers sqld with ws://.");
        }
    }
}
