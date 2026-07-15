using System.Data;
using Nelknet.LibSQL.Data;
using Nelknet.LibSQL.Data.Exceptions;
using Nj.EntityFrameworkCore.LibSql.DriverContractTests.Infrastructure;
using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.DriverContractTests;

[Collection(LocalDriverCollection.Name)]
public sealed class ConnectionLifecycleLocalTests
{
    private readonly LocalDriverFixture _fixture;

    public ConnectionLifecycleLocalTests(LocalDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public void Open_Close_Dispose_and_reopen_work()
    {
        using var connection = new LibSQLConnection(_fixture.CreateConnectionString());
        Assert.Equal(ConnectionState.Closed, connection.State);

        connection.Open();
        Assert.Equal(ConnectionState.Open, connection.State);

        connection.Close();
        Assert.Equal(ConnectionState.Closed, connection.State);

        connection.Open();
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Fact]
    public async Task OpenAsync_works()
    {
        await using var connection = await _fixture.CreateOpenConnectionAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    [Fact]
    public void Invalid_connection_string_fails_on_open()
    {
        using var connection = new LibSQLConnection("Data Source=");
        var ex = Assert.ThrowsAny<Exception>(() => connection.Open());
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void Concurrent_open_and_dispose_does_not_deadlock()
    {
        var connectionString = _fixture.CreateConnectionString();
        Parallel.For(0, 8, _ =>
        {
            using var connection = new LibSQLConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
        });
    }

    [Fact]
    public async Task Cancellation_before_open_is_honored_when_token_already_canceled()
    {
        using var connection = new LibSQLConnection(_fixture.CreateConnectionString());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => connection.OpenAsync(cts.Token));
    }

    [Fact]
    public void CommandTimeout_property_is_round_tripped()
    {
        using var connection = _fixture.CreateOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandTimeout = 7;
        Assert.Equal(7, command.CommandTimeout);
    }

    [Fact]
    public void ServerVersion_and_native_library_are_reported()
    {
        using var connection = _fixture.CreateOpenConnection();
        Assert.False(string.IsNullOrWhiteSpace(connection.ServerVersion));
        Assert.True(LibSQLVersion.IsLibraryLoaded());
        Assert.False(string.IsNullOrWhiteSpace(LibSQLVersion.GetVersionInfo()));
    }
}

[Collection(RemoteDriverCollection.Name)]
public sealed class ConnectionLifecycleRemoteTests
{
    private readonly RemoteDriverFixture _fixture;

    public ConnectionLifecycleRemoteTests(RemoteDriverFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task Remote_open_select_one_when_available()
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await using var connection = await _fixture.CreateOpenConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        Assert.Equal(
            1L,
            Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public void Unreachable_remote_host_fails_open()
    {
        using var connection = new LibSQLConnection(
            "Data Source=http://127.0.0.1:1");
        Assert.ThrowsAny<Exception>(() => connection.Open());
    }
}

public sealed class ProviderFactoryTests
{
    [Fact]
    public void LibSQLFactory_creates_connection_command_and_parameter()
    {
        Assert.NotNull(LibSQLFactory.Instance);
        Assert.Equal("Nelknet.LibSQL.Data", LibSQLFactory.ProviderInvariantName);

        var connection = Assert.IsType<LibSQLConnection>(LibSQLFactory.Instance.CreateConnection());
        connection.ConnectionString = TestEnvironment.InMemoryConnectionString;
        connection.Open();

        var command = Assert.IsType<LibSQLCommand>(LibSQLFactory.Instance.CreateCommand());
        command.Connection = connection;
        command.CommandText = "SELECT 1";
        Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));

        Assert.IsType<LibSQLParameter>(LibSQLFactory.Instance.CreateParameter());
        Assert.IsType<LibSQLConnectionStringBuilder>(
            LibSQLFactory.Instance.CreateConnectionStringBuilder());
    }

    [Fact]
    public void RegisterFactory_does_not_throw()
    {
        LibSQLFactory.RegisterFactory();
        LibSQLFactory.UnregisterFactory();
    }
}
