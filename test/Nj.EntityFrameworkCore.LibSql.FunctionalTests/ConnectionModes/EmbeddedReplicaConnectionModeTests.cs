using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.ConnectionModes;

[Collection(ConnectionModesReplicaCollection.Name)]
public sealed class EmbeddedReplicaConnectionModeTests
{
    private readonly ConnectionModesReplicaFixture _fixture;
    private readonly string _replicaDir =
        Path.Combine(Path.GetTempPath(), "nj-ef-replica-" + Guid.NewGuid().ToString("N"));

    public EmbeddedReplicaConnectionModeTests(ConnectionModesReplicaFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task Database_sync_sees_primary_write()
    {
        EnsureAvailable();
        Directory.CreateDirectory(_replicaDir);

        var marker = "ef-" + Guid.NewGuid().ToString("N")[..8];
        await using (var primary = CreateContext(_fixture.ConnectionString))
        {
            await primary.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            primary.Items.Add(new ModeItem { Name = marker });
            await primary.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var replicaPath = Path.Combine(_replicaDir, "ef-replica.db");
        var replicaCs = TestEnvironment.EmbeddedReplicaConnectionString(
            replicaPath,
            _fixture.PrimaryHttpUrl,
            authToken: null,
            readYourWrites: false);

        await using var replica = CreateContext(replicaCs);
        var sync = await replica.Database.SyncAsync(TestContext.Current.CancellationToken);
        Assert.True(sync.FrameNo >= 0);
        Assert.Contains(
            marker,
            await replica.Items.Select(i => i.Name).ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EnsureDeleted_throws_for_replica()
    {
        EnsureAvailable();
        Directory.CreateDirectory(_replicaDir);
        var replicaPath = Path.Combine(_replicaDir, "ef-del.db");
        var replicaCs = TestEnvironment.EmbeddedReplicaConnectionString(
            replicaPath,
            _fixture.PrimaryHttpUrl,
            readYourWrites: false);

        await using var context = CreateContext(replicaCs);
        // Open/sync once so the replica file exists.
        _ = await context.Database.SyncAsync(TestContext.Current.CancellationToken);
        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken));
        Assert.Contains("embedded-replica", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureAvailable()
    {
        if (_fixture.IsAvailable)
        {
            return;
        }

        if (TestEnvironment.RemoteTestsRequired)
        {
            Assert.Fail("Replica sqld required but unavailable: " + (_fixture.UnavailableReason ?? "unknown"));
        }

        Assert.Skip("Replica sqld unavailable: " + (_fixture.UnavailableReason ?? "unknown"));
    }

    private static ModeDbContext CreateContext(string cs)
        => new(new DbContextOptionsBuilder<ModeDbContext>().UseLibSql(cs).Options);
}

public sealed class ConnectionModesReplicaFixture : IAsyncLifetime
{
    private IContainer? _sqld;

    public bool IsAvailable { get; private set; }
    public string? UnavailableReason { get; private set; }
    public string ConnectionString { get; private set; } = "";

    public string PrimaryHttpUrl
        => new LibSqlConnectionStringBuilder(ConnectionString).DataSource
           ?? throw new InvalidOperationException("No primary URL.");

    public async ValueTask InitializeAsync()
    {
        if (TestEnvironment.RemoteTestsDisabled || TestEnvironment.TestcontainersDisabled)
        {
            UnavailableReason = "Remote/Testcontainers disabled.";
            return;
        }

        try
        {
            _sqld = new ContainerBuilder(TestEnvironment.SqldImage)
                .WithEnvironment("SQLD_NODE", "primary")
                .WithPortBinding(8080, assignRandomHostPort: true)
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilHttpRequestIsSucceeded(r => r.ForPath("/v2").ForPort(8080)))
                .Build();
            await _sqld.StartAsync();
            ConnectionString = TestEnvironment.RemoteConnectionStringFromUrl(
                $"http://127.0.0.1:{_sqld.GetMappedPublicPort(8080)}");
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            UnavailableReason = ex.GetType().Name + ": " + ex.Message;
            if (_sqld is not null)
            {
                try { await _sqld.DisposeAsync(); } catch { /* ignore */ }
                _sqld = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqld is null)
        {
            return;
        }

        try { await _sqld.DisposeAsync(); } catch { /* ignore */ }
        _sqld = null;
    }
}

[CollectionDefinition(Name)]
public sealed class ConnectionModesReplicaCollection : ICollectionFixture<ConnectionModesReplicaFixture>
{
    public const string Name = "ConnectionModesReplica";
}
