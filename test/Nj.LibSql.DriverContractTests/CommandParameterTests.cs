using System.Data;
using System.Text;
using Nj.LibSql.Data;
using Nj.LibSql.DriverContractTests.Infrastructure;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

[Collection(LocalDriverCollection.Name)]
public sealed class CommandParameterLocalTests
{
    private readonly LocalDriverFixture _fixture;

    public CommandParameterLocalTests(LocalDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public void Named_parameters_insert_and_select()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE people (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL,
                score REAL,
                blob BLOB,
                guid TEXT,
                created TEXT
            )
            """;
        command.ExecuteNonQuery();

        command.CommandText =
            """
            INSERT INTO people (name, active, score, blob, guid, created)
            VALUES (@name, @active, @score, @blob, @guid, @created)
            """;
        var guid = Guid.NewGuid();
        var payload = Encoding.UTF8.GetBytes("hello-世界");
        command.Parameters.AddWithValue("@name", "Ada");
        command.Parameters.AddWithValue("@active", true);
        command.Parameters.AddWithValue("@score", 3.5);
        command.Parameters.AddWithValue("@blob", payload);
        command.Parameters.AddWithValue("@guid", guid.ToString());
        command.Parameters.AddWithValue("@created", "2026-07-15T00:00:00.0000000Z");
        Assert.Equal(1, command.ExecuteNonQuery());

        command.Parameters.Clear();
        command.CommandText = "SELECT last_insert_rowid()";
        Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));

        command.CommandText = "SELECT name, active, score, blob, guid FROM people WHERE id = 1";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("Ada", reader.GetString(0));
        Assert.Equal(1L, reader.GetInt64(1));
        Assert.Equal(3.5, reader.GetDouble(2), precision: 5);
        var buffer = new byte[payload.Length];
        Assert.Equal(payload.Length, reader.GetBytes(3, 0, buffer, 0, buffer.Length));
        Assert.Equal(payload, buffer);
        Assert.Equal(guid.ToString(), reader.GetString(4));
        Assert.False(reader.Read());
    }

    [Fact]
    public void Null_parameter_round_trips()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY, note TEXT)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO t(note) VALUES(@note)";
        command.Parameters.AddWithValue("@note", DBNull.Value);
        command.ExecuteNonQuery();

        command.Parameters.Clear();
        command.CommandText = "SELECT note FROM t WHERE id = 1";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.True(reader.IsDBNull(0));
    }

    [Fact]
    public void Dollar_named_parameter_accepts_at_prefix_on_parameter()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT $v";
        command.Parameters.AddWithValue("@v", 7);
        Assert.Equal(7L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [Fact]
    public void Repeated_named_parameter_reference_works()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT @v AS a, @v AS b";
        command.Parameters.AddWithValue("@v", 42);
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(42L, reader.GetInt64(0));
        Assert.Equal(42L, reader.GetInt64(1));
    }

    [Fact]
    public void Prepare_and_statement_caching_reuse_command()
    {
        using var connection = _fixture.CreateOpenConnection();
        connection.EnableStatementCaching = true;
        using var command = connection.CreateCommand();
        command.EnableStatementCaching = true;
        command.CommandText = "SELECT @n";
        command.Parameters.AddWithValue("@n", 1);
        command.Prepare();
        Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
        command.Parameters["@n"].Value = 2;
        Assert.Equal(2L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [Fact]
    public void ExecuteNonQuery_scalar_reader_async_equivalents()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE nums(n INTEGER)";
        Assert.Equal(0, command.ExecuteNonQuery());

        command.CommandText = "INSERT INTO nums(n) VALUES (7)";
        Assert.Equal(1, command.ExecuteNonQuery());

        command.CommandText = "SELECT n FROM nums";
        Assert.Equal(7L, Convert.ToInt64(command.ExecuteScalar()));

        using var reader = command.ExecuteReader();
        Assert.True(reader.HasRows);
        Assert.Equal(1, reader.FieldCount);
        Assert.Equal("n", reader.GetName(0));
        Assert.Equal(0, reader.GetOrdinal("n"));
        Assert.True(reader.Read());
        Assert.Equal(7L, reader.GetInt64(0));
    }

    [Fact]
    public async Task Async_command_apis_work()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = await _fixture.CreateOpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE async_t(id INTEGER PRIMARY KEY, v TEXT)";
        await command.ExecuteNonQueryAsync(ct);
        command.CommandText = "INSERT INTO async_t(v) VALUES('x')";
        Assert.Equal(1, await command.ExecuteNonQueryAsync(ct));
        command.CommandText = "SELECT v FROM async_t";
        Assert.Equal("x", await command.ExecuteScalarAsync(ct));
        await using var reader = await command.ExecuteReaderAsync(ct);
        Assert.True(await reader.ReadAsync(ct));
        Assert.Equal("x", reader.GetString(0));
    }

    [Fact]
    public void Last_insert_rowid_is_per_connection()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE ids(id INTEGER PRIMARY KEY, v TEXT)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO ids(v) VALUES('a')";
        command.ExecuteNonQuery();
        command.CommandText = "SELECT last_insert_rowid()";
        Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
        command.CommandText = "INSERT INTO ids(v) VALUES('b')";
        command.ExecuteNonQuery();
        command.CommandText = "SELECT last_insert_rowid()";
        Assert.Equal(2L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [Fact]
    public void Reader_typed_getters_and_sequential_reads()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE vals(i INTEGER, s TEXT, d REAL)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO vals VALUES (1, 'one', 1.5), (2, 'two', 2.5)";
        command.ExecuteNonQuery();
        command.CommandText = "SELECT i, s, d FROM vals ORDER BY i";
        using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("one", reader.GetString(1));
        Assert.Equal(1.5, reader.GetDouble(2), 5);
        Assert.True(reader.Read());
        Assert.Equal(2, reader.GetInt32(0));
        Assert.False(reader.Read());
    }

    [Fact]
    public void Integer_widths_and_decimal_round_trip_as_supported()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE nums(i64 INTEGER, r REAL)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO nums(i64, r) VALUES (@i64, @r)";
        command.Parameters.AddWithValue("@i64", long.MaxValue);
        command.Parameters.AddWithValue("@r", 123.45);
        command.ExecuteNonQuery();
        command.Parameters.Clear();
        command.CommandText = "SELECT i64, r FROM nums";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(long.MaxValue, reader.GetInt64(0));
        Assert.Equal(123.45, reader.GetDouble(1), 5);
    }
}
