using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class RemoteLibSqlComplianceCollection : ICollectionFixture<RemoteLibSqlComplianceFixture>
{
    public const string Name = "RemoteLibSqlCompliance";
}

public sealed class RemoteLibSqlComplianceFixture : IAsyncLifetime
{
    private IContainer? _sqld;

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public string ConnectionString { get; private set; } = "";

    public static string? SharedConnectionString { get; private set; }

    public async Task InitializeAsync()
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
                    $"http://127.0.0.1:{_sqld.GetMappedPublicPort(8080)}");
            }

            await using var context = new ProbeDbContext(
                new DbContextOptionsBuilder<ProbeDbContext>()
                    .UseLibSql(ConnectionString)
                    .Options);
            await context.Database.OpenConnectionAsync();
            await context.Database.CloseConnectionAsync();
            IsAvailable = true;
            SharedConnectionString = ConnectionString;
        }
        catch (Exception ex)
        {
            UnavailableReason = ex.GetType().Name + ": " + ex.Message;
            IsAvailable = false;
            SharedConnectionString = null;
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

    public async Task DisposeAsync()
    {
        SharedConnectionString = null;
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

public static class RemoteComplianceAssert
{
    public static void SkipIfUnavailable(RemoteLibSqlComplianceFixture fixture, string waiverId = "C-REMOTE")
    {
        Skip.If(
            !fixture.IsAvailable,
            $"[{waiverId}] Remote sqld unavailable: {fixture.UnavailableReason ?? "unknown"}");
    }
}

public sealed class RemoteLibSqlTestStoreFactorySingleton : RelationalTestStoreFactory
{
    public static RemoteLibSqlTestStoreFactorySingleton Instance { get; } = new();

    public override TestStore Create(string storeName)
    {
        var cs = RemoteLibSqlComplianceFixture.SharedConnectionString
            ?? throw new InvalidOperationException("Remote compliance connection string is not initialized.");
        return new RemoteLibSqlTestStore(storeName, cs);
    }

    public override TestStore GetOrCreate(string storeName)
        => Create(storeName);

    public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkLibSql();
}
