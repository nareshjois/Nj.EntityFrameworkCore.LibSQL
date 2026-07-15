using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteSelectOneTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteSelectOneTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task UseLibSql_remote_sqld_can_execute_select_one()
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await using var context = new SmokeDbContext(
            new DbContextOptionsBuilder<SmokeDbContext>()
                .UseLibSql(_fixture.ConnectionString)
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

    private sealed class SmokeDbContext(DbContextOptions<SmokeDbContext> options) : DbContext(options);
}

public sealed class RemoteLibSqlFixture : IAsyncLifetime
{
    private IContainer? _sqld;

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public string ConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        if (TestEnvironment.RemoteTestsDisabled)
        {
            UnavailableReason = $"{TestEnvironment.DisableRemoteEnvironmentVariable}=1";
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(TestEnvironment.ExternalRemoteSqldUrl))
            {
                ConnectionString = TestEnvironment.RemoteConnectionStringFromUrl(
                    TestEnvironment.ExternalRemoteSqldUrl);
            }
            else if (TestEnvironment.TestcontainersDisabled)
            {
                UnavailableReason =
                    $"{TestEnvironment.DisableTestcontainersEnvironmentVariable}=1 and no {TestEnvironment.RemoteUrlEnvironmentVariable}.";
                return;
            }
            else
            {
                _sqld = new ContainerBuilder(TestEnvironment.SqldImage)
                    .WithEnvironment("SQLD_NODE", "primary")
                    .WithPortBinding(8080, assignRandomHostPort: true)
                    .WithWaitStrategy(
                        Wait.ForUnixContainer()
                            .UntilHttpRequestIsSucceeded(request => request
                                .ForPath("/v2")
                                .ForPort(8080)))
                    .Build();

                await _sqld.StartAsync();
                ConnectionString = TestEnvironment.RemoteConnectionStringFromUrl(
                    $"http://{_sqld.Hostname}:{_sqld.GetMappedPublicPort(8080)}");
            }

            await using var context = new ProbeDbContext(
                new DbContextOptionsBuilder<ProbeDbContext>()
                    .UseLibSql(ConnectionString)
                    .Options);
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            await context.Database.CloseConnectionAsync();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            UnavailableReason = ex.GetType().Name + ": " + ex.Message;
            IsAvailable = false;
            if (_sqld is not null)
            {
                try
                {
                    await _sqld.DisposeAsync();
                }
                catch
                {
                    // Ignore teardown flakes.
                }

                _sqld = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqld is not null)
        {
            try
            {
                await _sqld.DisposeAsync();
            }
            catch
            {
                // Ignore teardown flakes.
            }

            _sqld = null;
        }
    }

    private sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options);
}

[CollectionDefinition(Name)]
public sealed class RemoteLibSqlCollection : ICollectionFixture<RemoteLibSqlFixture>
{
    public const string Name = "RemoteLibSqlFunctional";
}
