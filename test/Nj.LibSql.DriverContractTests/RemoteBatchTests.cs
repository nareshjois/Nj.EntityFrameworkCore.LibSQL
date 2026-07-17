using Nj.LibSql.Data;
using Nj.LibSql.DriverContractTests.Infrastructure;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

[Collection(RemoteDriverCollection.Name)]
public sealed class RemoteBatchTests
{
    private readonly RemoteDriverFixture _fixture;

    public RemoteBatchTests(RemoteDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task CreateBatch_ExecuteReaderAsync_runs_multiple_statements_one_round_trip()
    {
        EnsureRemoteAvailable();
        var ct = TestContext.Current.CancellationToken;

        await using var connection = await _fixture.CreateOpenConnectionAsync(ct);
        var table = "batch_" + Guid.NewGuid().ToString("N")[..8];

        await using (var setup = connection.CreateCommand())
        {
            setup.CommandText = $"CREATE TABLE {table}(id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
            await setup.ExecuteNonQueryAsync(ct);
        }

        await using var batch = connection.CreateBatch();
        var insert = batch.CreateBatchCommand();
        insert.CommandText = $"INSERT INTO {table}(id, name) VALUES(@id, @name)";
        insert.Parameters.Add(new LibSqlParameter("@id", 1));
        insert.Parameters.Add(new LibSqlParameter("@name", "Ada"));
        batch.BatchCommands.Add(insert);

        var select = batch.CreateBatchCommand();
        select.CommandText = $"SELECT name FROM {table} WHERE id = $id";
        select.Parameters.Add(new LibSqlParameter("@id", 1));
        batch.BatchCommands.Add(select);

        await using var reader = await batch.ExecuteReaderAsync(ct);
        Assert.Equal(1, insert.RecordsAffected);
        Assert.False(reader.Read());
        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal("Ada", reader.GetString(0));
        Assert.False(reader.Read());
        Assert.False(reader.NextResult());
    }

    [Fact]
    public async Task CreateBatch_ExecuteNonQueryAsync_sums_affected_rows()
    {
        EnsureRemoteAvailable();
        var ct = TestContext.Current.CancellationToken;

        await using var connection = await _fixture.CreateOpenConnectionAsync(ct);
        var table = "batch_nq_" + Guid.NewGuid().ToString("N")[..8];

        await using (var setup = connection.CreateCommand())
        {
            setup.CommandText = $"CREATE TABLE {table}(id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
            await setup.ExecuteNonQueryAsync(ct);
        }

        await using var batch = connection.CreateBatch();
        foreach (var (id, name) in new[] { (1, "a"), (2, "b") })
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = $"INSERT INTO {table}(id, name) VALUES({id}, '{name}')";
            batch.BatchCommands.Add(cmd);
        }

        Assert.Equal(2, await batch.ExecuteNonQueryAsync(ct));
        Assert.Equal(1, ((LibSqlBatchCommand)batch.BatchCommands[0]).RecordsAffected);
        Assert.Equal(1, ((LibSqlBatchCommand)batch.BatchCommands[1]).RecordsAffected);
    }

    private void EnsureRemoteAvailable()
    {
        if (!_fixture.IsAvailable)
        {
            throw new InvalidOperationException(
                _fixture.UnavailableReason ?? "Remote sqld fixture is unavailable.");
        }
    }
}
