using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Nj.LibSql.DriverContractTests.Infrastructure;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

/// <summary>
/// Turso Cloud over HTTP Hrana (<c>libsql://</c> → <c>https://</c>).
/// Turso rejects WebSocket upgrades; WSS large-result gate lives on Testcontainers sqld.
/// </summary>
[Collection(TursoDriverCollection.Name)]
public sealed class TursoRemoteTests
{
    private const int LargeRowCount = 5_000;

    private readonly TursoDriverFixture _fixture;

    public TursoRemoteTests(TursoDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task Turso_select_one()
    {
        EnsureAvailable();

        await using var connection = await _fixture.CreateOpenConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        Assert.Equal(
            1L,
            Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public void Turso_short_transaction()
    {
        EnsureAvailable();

        using var connection = _fixture.CreateOpenConnection();
        var table = _fixture.TablePrefix + "_txn";
        using (var setup = connection.CreateCommand())
        {
            setup.CommandText =
                $"CREATE TABLE IF NOT EXISTS {table}(id INTEGER PRIMARY KEY, n INTEGER NOT NULL)";
            setup.ExecuteNonQuery();
        }

        using (var transaction = connection.BeginTransaction())
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = $"INSERT INTO {table}(n) VALUES(1)";
            command.ExecuteNonQuery();
            command.CommandText = $"INSERT INTO {table}(n) VALUES(2)";
            command.ExecuteNonQuery();
            transaction.Commit();
        }

        using var verify = connection.CreateCommand();
        verify.CommandText = $"SELECT COUNT(*) FROM {table}";
        Assert.Equal(2L, Convert.ToInt64(verify.ExecuteScalar()));

        using var drop = connection.CreateCommand();
        drop.CommandText = $"DROP TABLE IF EXISTS {table}";
        drop.ExecuteNonQuery();
    }

    [Fact]
    public void Turso_large_result_over_http()
    {
        EnsureAvailable();

        using var connection = _fixture.CreateOpenConnection();
        var table = _fixture.TablePrefix + "_large";

        using (var setup = connection.CreateCommand())
        {
            setup.CommandText =
                $"CREATE TABLE IF NOT EXISTS {table}(id INTEGER PRIMARY KEY, payload TEXT NOT NULL)";
            setup.ExecuteNonQuery();
            setup.CommandText = $"DELETE FROM {table}";
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

        using var drop = connection.CreateCommand();
        drop.CommandText = $"DROP TABLE IF EXISTS {table}";
        drop.ExecuteNonQuery();
    }

    private void EnsureAvailable()
    {
        if (_fixture.IsAvailable)
        {
            return;
        }

        if (TestEnvironment.TursoTestsRequired)
        {
            Assert.Fail(
                "Turso required but unavailable: "
                + (_fixture.UnavailableReason ?? "unknown"));
        }

        Assert.Skip(
            "Turso unavailable: "
            + (_fixture.UnavailableReason ?? "unknown")
            + $". Set {TestEnvironment.RemoteUrlEnvironmentVariable} and "
            + $"{TestEnvironment.AuthTokenEnvironmentVariable}.");
    }
}
