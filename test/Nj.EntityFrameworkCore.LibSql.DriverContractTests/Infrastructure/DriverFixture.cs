using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Nelknet.LibSQL.Data;
using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.DriverContractTests.Infrastructure;

public enum DriverConnectionMode
{
    LocalFile,
    RemoteSqld,
}

/// <summary>
/// Creates disposable Nelknet connections for a given mode.
/// </summary>
public abstract class DriverFixture : IAsyncLifetime
{
    public abstract DriverConnectionMode Mode { get; }

    public abstract string CreateConnectionString();

    public LibSQLConnection CreateOpenConnection()
    {
        var connection = new LibSQLConnection(CreateConnectionString());
        connection.Open();
        return connection;
    }

    public async Task<LibSQLConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new LibSQLConnection(CreateConnectionString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public virtual ValueTask InitializeAsync() => default;

    public virtual ValueTask DisposeAsync() => default;
}

public sealed class LocalDriverFixture : DriverFixture
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "nj-libsql-driver-" + Guid.NewGuid().ToString("N"));

    public override DriverConnectionMode Mode => DriverConnectionMode.LocalFile;

    public override ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        return default;
    }

    public override string CreateConnectionString()
    {
        var path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".db");
        return TestEnvironment.LocalConnectionString(path);
    }

    public override ValueTask DisposeAsync()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for temp files.
        }

        return default;
    }
}

/// <summary>
/// Starts a pinned <c>libsql-server</c> via Testcontainers unless an external
/// <c>LIBSQL_TEST_URL</c> is provided.
/// </summary>
public sealed class RemoteDriverFixture : DriverFixture
{
    private IContainer? _sqld;
    private string? _connectionString;

    public override DriverConnectionMode Mode => DriverConnectionMode.RemoteSqld;

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public override string CreateConnectionString()
        => _connectionString
           ?? throw new InvalidOperationException(
               "Remote fixture was not initialized. " + (UnavailableReason ?? string.Empty));

    public override async ValueTask InitializeAsync()
    {
        if (TestEnvironment.RemoteTestsDisabled)
        {
            UnavailableReason = $"Set {TestEnvironment.DisableRemoteEnvironmentVariable}=0 to enable.";
            IsAvailable = false;
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(TestEnvironment.ExternalRemoteSqldUrl))
            {
                _connectionString = TestEnvironment.RemoteConnectionStringFromUrl(
                    TestEnvironment.ExternalRemoteSqldUrl);
            }
            else if (TestEnvironment.TestcontainersDisabled)
            {
                UnavailableReason =
                    $"{TestEnvironment.DisableTestcontainersEnvironmentVariable}=1 and no {TestEnvironment.RemoteUrlEnvironmentVariable}.";
                IsAvailable = false;
                return;
            }
            else
            {
                _sqld = new ContainerBuilder(TestEnvironment.SqldImage)
                    .WithEnvironment("SQLD_NODE", "primary")
                    .WithPortBinding(8080, assignRandomHostPort: true)
                    // Multi-arch index digest — let Docker select native amd64/arm64.
                    // GET / returns 404; /v2 and /health return 200 when ready.
                    .WithWaitStrategy(
                        Wait.ForUnixContainer()
                            .UntilHttpRequestIsSucceeded(request => request
                                .ForPath("/v2")
                                .ForPort(8080)))
                    .Build();

                await _sqld.StartAsync();
                var host = _sqld.Hostname;
                var port = _sqld.GetMappedPublicPort(8080);
                _connectionString = TestEnvironment.RemoteConnectionStringFromUrl(
                    $"http://{host}:{port}");
            }

            await using var connection = await CreateOpenConnectionAsync(
                TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            _ = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            UnavailableReason = ex.GetType().Name + ": " + ex.Message;
            await DisposeContainerQuietlyAsync();
        }
    }

    public override async ValueTask DisposeAsync()
        => await DisposeContainerQuietlyAsync();

    private async ValueTask DisposeContainerQuietlyAsync()
    {
        if (_sqld is null)
        {
            return;
        }

        try
        {
            await _sqld.DisposeAsync();
        }
        catch
        {
            // Docker Desktop can flake on teardown; never fail the suite for that.
        }
        finally
        {
            _sqld = null;
        }
    }
}

[CollectionDefinition(Name)]
public sealed class LocalDriverCollection : ICollectionFixture<LocalDriverFixture>
{
    public const string Name = "LocalDriver";
}

[CollectionDefinition(Name)]
public sealed class RemoteDriverCollection : ICollectionFixture<RemoteDriverFixture>
{
    public const string Name = "RemoteDriver";
}
