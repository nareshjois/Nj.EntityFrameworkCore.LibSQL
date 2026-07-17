using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.ConnectionModes;

[Collection(ConnectionModesSqldCollection.Name)]
public sealed class RemoteSqldConnectionModeTests
{
    private readonly ConnectionModesSqldFixture _fixture;

    public RemoteSqldConnectionModeTests(ConnectionModesSqldFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task Open_select_one_and_ensure_created()
    {
        EnsureAvailable();

        await using var context = CreateContext(_fixture.ConnectionString);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        context.Items.Add(new ModeItem { Name = "sqld" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, await context.Items.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EnsureDeleted_throws()
    {
        EnsureAvailable();

        await using var context = CreateContext(_fixture.ConnectionString);
        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken));
        Assert.Contains("remote", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Transaction_session_affinity()
    {
        EnsureAvailable();

        await using var context = CreateContext(_fixture.ConnectionString);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        context.Items.Add(new ModeItem { Name = "t1" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.Items.Add(new ModeItem { Name = "t2" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await tx.CommitAsync(TestContext.Current.CancellationToken);
        Assert.True(await context.Items.CountAsync(TestContext.Current.CancellationToken) >= 2);
    }

    [Fact]
    public async Task Cancelled_SaveChangesAsync_surfaces_without_auto_retry()
    {
        EnsureAvailable();

        await using var context = CreateContext(_fixture.ConnectionString);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Pre-cancelled token: must fail before/during execute with OCE, not silent success.
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        context.Items.Add(new ModeItem { Name = "cancel-me" });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.SaveChangesAsync(cts.Token));
    }

    [Fact]
    public async Task Cancelled_remote_command_ExecuteScalarAsync_throws()
    {
        EnsureAvailable();

        await using var connection = new LibSqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => command.ExecuteScalarAsync(cts.Token));
    }

    private void EnsureAvailable()
    {
        if (_fixture.IsAvailable)
        {
            return;
        }

        if (TestEnvironment.RemoteTestsRequired)
        {
            Assert.Fail("sqld required but unavailable: " + (_fixture.UnavailableReason ?? "unknown"));
        }

        Assert.Skip("sqld unavailable: " + (_fixture.UnavailableReason ?? "unknown"));
    }

    private static ModeDbContext CreateContext(string cs)
        => new(new DbContextOptionsBuilder<ModeDbContext>().UseLibSql(cs).Options);
}

/// <summary>
/// Fault-injection against a dedicated sqld container so stopping it cannot
/// poison the shared <see cref="ConnectionModesSqldCollection"/> suite.
/// </summary>
[Collection(ConnectionModesSqldFaultCollection.Name)]
public sealed class RemoteSqldFaultInjectionTests
{
    private readonly ConnectionModesSqldFixture _fixture;

    public RemoteSqldFaultInjectionTests(ConnectionModesSqldFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task Kill_sqld_mid_commit_is_ambiguous_without_auto_retry()
    {
        if (!_fixture.IsAvailable)
        {
            if (TestEnvironment.RemoteTestsRequired)
            {
                Assert.Fail("sqld required but unavailable: " + (_fixture.UnavailableReason ?? "unknown"));
            }

            Assert.Skip("sqld unavailable: " + (_fixture.UnavailableReason ?? "unknown"));
        }

        Assert.True(_fixture.CanStopContainer, "Fault injection requires Testcontainers-managed sqld.");

        await using var context = new ModeDbContext(
            new DbContextOptionsBuilder<ModeDbContext>().UseLibSql(_fixture.ConnectionString).Options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await using var tx = await context.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        context.Items.Add(new ModeItem { Name = "fault" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _fixture.StopContainerAsync();

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => tx.CommitAsync(TestContext.Current.CancellationToken));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        // No automatic retry — single failure surfaces to the caller.
    }
}

public sealed class ConnectionModesSqldFixture : IAsyncLifetime
{
    private IContainer? _sqld;

    public bool IsAvailable { get; private set; }
    public string? UnavailableReason { get; private set; }
    public string ConnectionString { get; private set; } = "";
    public bool CanStopContainer => _sqld is not null;

    public async ValueTask InitializeAsync()
    {
        if (TestEnvironment.RemoteTestsDisabled)
        {
            UnavailableReason = $"{TestEnvironment.DisableRemoteEnvironmentVariable}=1";
            return;
        }

        // Prefer a dedicated Testcontainers instance so Turso env does not steal this suite.
        if (TestEnvironment.TestcontainersDisabled)
        {
            UnavailableReason =
                $"{TestEnvironment.DisableTestcontainersEnvironmentVariable}=1";
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

            await using var probe = new ModeDbContext(
                new DbContextOptionsBuilder<ModeDbContext>().UseLibSql(ConnectionString).Options);
            await probe.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            await probe.Database.CloseConnectionAsync();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            UnavailableReason = ex.GetType().Name + ": " + ex.Message;
            await DisposeQuietlyAsync();
        }
    }

    public async Task StopContainerAsync()
    {
        if (_sqld is null)
        {
            return;
        }

        await _sqld.StopAsync();
    }

    public async ValueTask DisposeAsync()
        => await DisposeQuietlyAsync();

    private async ValueTask DisposeQuietlyAsync()
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
            // ignore
        }
        finally
        {
            _sqld = null;
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ConnectionModesSqldCollection : ICollectionFixture<ConnectionModesSqldFixture>
{
    public const string Name = "ConnectionModesSqld";
}

[CollectionDefinition(Name)]
public sealed class ConnectionModesSqldFaultCollection : ICollectionFixture<ConnectionModesSqldFixture>
{
    public const string Name = "ConnectionModesSqldFault";
}
