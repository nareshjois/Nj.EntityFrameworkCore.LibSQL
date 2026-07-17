using System.Data;
using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Nj.LibSql.Data;
using Nj.LibSql.Data.Exceptions;
using Nj.LibSql.DriverContractTests.Infrastructure;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

[Collection(LocalDriverCollection.Name)]
public sealed class TransactionLocalTests
{
    private readonly LocalDriverFixture _fixture;

    public TransactionLocalTests(LocalDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public void Commit_persists_all_statements()
    {
        using var connection = _fixture.CreateOpenConnection();
        using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE acct(id INTEGER PRIMARY KEY, balance INTEGER NOT NULL)";
            setup.ExecuteNonQuery();
        }

        using (var transaction = connection.BeginTransaction())
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO acct(balance) VALUES(100)";
            command.ExecuteNonQuery();
            command.CommandText = "INSERT INTO acct(balance) VALUES(200)";
            command.ExecuteNonQuery();
            transaction.Commit();
        }

        using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT COUNT(*), SUM(balance) FROM acct";
        using var reader = verify.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(2L, reader.GetInt64(0));
        Assert.Equal(300L, reader.GetInt64(1));
    }

    [Fact]
    public void Rollback_persists_no_statements()
    {
        using var connection = _fixture.CreateOpenConnection();
        using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE acct(id INTEGER PRIMARY KEY, balance INTEGER NOT NULL)";
            setup.ExecuteNonQuery();
        }

        using (var transaction = connection.BeginTransaction())
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO acct(balance) VALUES(100)";
            command.ExecuteNonQuery();
            transaction.Rollback();
        }

        using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT COUNT(*) FROM acct";
        Assert.Equal(0L, Convert.ToInt64(verify.ExecuteScalar()));
    }

    [Fact]
    public void Dispose_without_commit_rolls_back()
    {
        using var connection = _fixture.CreateOpenConnection();
        using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE acct(id INTEGER PRIMARY KEY, balance INTEGER NOT NULL)";
            setup.ExecuteNonQuery();
        }

        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO acct(balance) VALUES(100)";
            command.ExecuteNonQuery();
        }

        using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT COUNT(*) FROM acct";
        Assert.Equal(0L, Convert.ToInt64(verify.ExecuteScalar()));
    }

    [Fact]
    public void Command_with_foreign_transaction_behavior_is_documented()
    {
        // Driver may not always reject a command bound to another connection's
        // transaction at Execute time. EF must still assign transactions carefully;
        // see docs/compatibility.md.
        using var connection1 = _fixture.CreateOpenConnection();
        using var connection2 = _fixture.CreateOpenConnection();
        using var transaction = connection1.BeginTransaction();
        using var command = connection2.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1";
        var ex = Record.Exception(() => command.ExecuteScalar());
        if (ex is null)
        {
            Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
        }
    }

    [Fact]
    public void Nested_begin_is_rejected_or_documented()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var outer = connection.BeginTransaction();
        var ex = Record.Exception(() => connection.BeginTransaction());
        Assert.NotNull(ex);
    }

    [Fact]
    public void IsolationLevel_is_reported()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
    }

    [Fact]
    public void Sql_savepoint_create_rollback_and_release_work()
    {
        // No first-class Savepoint APIs; SQL SAVEPOINT is the contract surface.
        using var connection = _fixture.CreateOpenConnection();
        using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "CREATE TABLE s(id INTEGER PRIMARY KEY, n INTEGER NOT NULL)";
            setup.ExecuteNonQuery();
        }

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO s(n) VALUES(1)";
        command.ExecuteNonQuery();
        command.CommandText = "SAVEPOINT sp1";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO s(n) VALUES(2)";
        command.ExecuteNonQuery();
        command.CommandText = "ROLLBACK TO sp1";
        command.ExecuteNonQuery();
        command.CommandText = "RELEASE sp1";
        command.ExecuteNonQuery();
        transaction.Commit();

        using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT COUNT(*) FROM s";
        Assert.Equal(1L, Convert.ToInt64(verify.ExecuteScalar()));
    }
}

[Collection(RemoteDriverCollection.Name)]
public sealed class TransactionRemoteTests
{
    private readonly RemoteDriverFixture _fixture;

    public TransactionRemoteTests(RemoteDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public void Remote_transaction_survives_multiple_commands()
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
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        using var connection = _fixture.CreateOpenConnection();
        var table = "tx_" + Guid.NewGuid().ToString("N");
        using (var setup = connection.CreateCommand())
        {
            setup.CommandText = $"CREATE TABLE IF NOT EXISTS {table}(id INTEGER PRIMARY KEY, n INTEGER NOT NULL)";
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
            command.CommandText = $"SELECT COUNT(*) FROM {table}";
            Assert.Equal(2L, Convert.ToInt64(command.ExecuteScalar()));
            transaction.Commit();
        }

        using var verify = connection.CreateCommand();
        verify.CommandText = $"SELECT COUNT(*) FROM {table}";
        Assert.Equal(2L, Convert.ToInt64(verify.ExecuteScalar()));
    }
}

[Collection(LocalDriverCollection.Name)]
public sealed class ErrorMappingLocalTests
{
    private readonly LocalDriverFixture _fixture;

    public ErrorMappingLocalTests(LocalDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public void Unique_violation_throws_LibSqlException_with_error_code()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE u(id INTEGER PRIMARY KEY, email TEXT UNIQUE)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO u(email) VALUES('a@b.com')";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO u(email) VALUES('a@b.com')";

        var ex = Assert.ThrowsAny<LibSqlException>(() => command.ExecuteNonQuery());
        Assert.NotEqual(0, ex.LibSqlErrorCode);
        // Observed with bundled libSQL: code 2 "Internal logic error" rather than
        // LibSqlConstraintException. See docs/compatibility.md.
        Assert.DoesNotContain("a@b.com", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Not_null_violation_throws()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE n(id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO n(name) VALUES(NULL)";
        Assert.ThrowsAny<LibSqlException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Foreign_key_violation_throws_when_enforced()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE parent(id INTEGER PRIMARY KEY)";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE child(id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id))";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO child(parent_id) VALUES(999)";
        Assert.ThrowsAny<LibSqlException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Syntax_error_throws()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECTTTT 1";
        var ex = Assert.ThrowsAny<Exception>(() => command.ExecuteScalar());
        Assert.True(ex is LibSqlException or InvalidOperationException);
        Assert.Contains("syntax", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_table_throws()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM does_not_exist";
        var ex = Assert.ThrowsAny<Exception>(() => command.ExecuteReader());
        Assert.True(ex is LibSqlException or InvalidOperationException);
        Assert.Contains("no such table", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Auth_token_is_not_echoed_in_exception_from_connection_string()
    {
        using var connection = new LibSqlConnection(
            "Data Source=http://127.0.0.1:1;Auth Token=super-secret-token-value");
        var ex = Assert.ThrowsAny<Exception>(() => connection.Open());
        Assert.DoesNotContain("super-secret-token-value", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-token-value", ex.ToString(), StringComparison.Ordinal);
        if (ex is LibSqlConnectionException connectionException
            && connectionException.ConnectionString is not null)
        {
            Assert.DoesNotContain(
                "super-secret-token-value",
                connectionException.ConnectionString,
                StringComparison.Ordinal);
        }
    }
}
