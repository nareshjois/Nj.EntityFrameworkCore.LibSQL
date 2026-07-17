using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Nj.LibSql.Data;
using Nj.LibSql.Data.Exceptions;
using Nj.LibSql.DriverContractTests.Infrastructure;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

/// <summary>
/// Embedded replica sync against dedicated Testcontainers <c>sqld</c>.
/// Turso Cloud Sync currently hangs with the pinned native client — see C-019.
/// </summary>
[Collection(SqldReplicaCollection.Name)]
public sealed class EmbeddedReplicaTests
{
    private readonly SqldReplicaFixture _fixture;
    private readonly string _replicaDirectory =
        Path.Combine(Path.GetTempPath(), "nj-libsql-replica-" + Guid.NewGuid().ToString("N"));

    public EmbeddedReplicaTests(SqldReplicaFixture fixture)
        => _fixture = fixture;

    [Fact]
    public void Sync_on_local_connection_throws()
    {
        using var connection = new LibSqlConnection(TestEnvironment.InMemoryConnectionString);
        connection.Open();
        var ex = Assert.Throws<InvalidOperationException>(() => connection.Sync());
        Assert.Contains("embedded-replica", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Open_replica_with_remote_data_source_throws()
    {
        EnsureAvailable();

        var cs = $"Data Source={_fixture.PrimaryHttpUrl};Sync URL={_fixture.PrimaryHttpUrl}";
        using var connection = new LibSqlConnection(cs);
        var ex = Assert.Throws<LibSqlConnectionException>(() => connection.Open());
        Assert.Contains("local file path", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Replica_sync_sees_primary_write()
    {
        EnsureAvailable();

        Directory.CreateDirectory(_replicaDirectory);
        var table = "repl_" + Guid.NewGuid().ToString("N")[..8];
        var marker = Guid.NewGuid().ToString("N");

        using (var primary = _fixture.CreateOpenConnection())
        {
            using var setup = primary.CreateCommand();
            setup.CommandText =
                $"CREATE TABLE {table}(id INTEGER PRIMARY KEY, payload TEXT NOT NULL)";
            setup.ExecuteNonQuery();
            setup.CommandText = $"INSERT INTO {table}(payload) VALUES(@p)";
            setup.Parameters.Add(new LibSqlParameter("@p", marker));
            setup.ExecuteNonQuery();
        }

        var replicaPath = Path.Combine(_replicaDirectory, "sync-see.db");
        var replicaCs = TestEnvironment.EmbeddedReplicaConnectionString(
            replicaPath,
            _fixture.PrimaryHttpUrl,
            authToken: null,
            readYourWrites: false);

        using var replica = new LibSqlConnection(replicaCs);
        replica.Open();
        Assert.True(replica.IsEmbeddedReplica);

        var syncResult = replica.Sync();
        Assert.True(syncResult.FrameNo >= 0);

        using var verify = replica.CreateCommand();
        verify.CommandText = $"SELECT payload FROM {table} ORDER BY id DESC LIMIT 1";
        Assert.Equal(marker, Convert.ToString(verify.ExecuteScalar()));
    }

    [Fact]
    public async Task Replica_sync_async_round_trip()
    {
        EnsureAvailable();

        Directory.CreateDirectory(_replicaDirectory);
        var table = "repl_async_" + Guid.NewGuid().ToString("N")[..8];
        var marker = Guid.NewGuid().ToString("N");

        await using (var primary = await _fixture.CreateOpenConnectionAsync(
                         TestContext.Current.CancellationToken))
        {
            await using var setup = primary.CreateCommand();
            setup.CommandText =
                $"CREATE TABLE {table}(id INTEGER PRIMARY KEY, payload TEXT NOT NULL)";
            await setup.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            setup.CommandText = $"INSERT INTO {table}(payload) VALUES(@p)";
            setup.Parameters.Add(new LibSqlParameter("@p", marker));
            await setup.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var replicaPath = Path.Combine(_replicaDirectory, "sync-async.db");
        var replicaCs = TestEnvironment.EmbeddedReplicaConnectionString(
            replicaPath,
            _fixture.PrimaryHttpUrl,
            authToken: null,
            readYourWrites: false);

        await using var replica = new LibSqlConnection(replicaCs);
        await replica.OpenAsync(TestContext.Current.CancellationToken);
        _ = await replica.SyncAsync(TestContext.Current.CancellationToken);

        await using var verify = replica.CreateCommand();
        verify.CommandText = $"SELECT COUNT(*) FROM {table} WHERE payload = @p";
        verify.Parameters.Add(new LibSqlParameter("@p", marker));
        Assert.Equal(
            1L,
            Convert.ToInt64(await verify.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
    }

    private void EnsureAvailable()
    {
        if (_fixture.IsAvailable)
        {
            return;
        }

        if (TestEnvironment.RemoteTestsRequired)
        {
            Assert.Fail(
                "sqld replica primary required but unavailable: "
                + (_fixture.UnavailableReason ?? "unknown"));
        }

        Assert.Skip(
            "sqld replica primary unavailable: "
            + (_fixture.UnavailableReason ?? "unknown"));
    }
}
