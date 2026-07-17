using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.ConnectionModes;

[Collection(ConnectionModesTursoCollection.Name)]
public sealed class TursoConnectionModeTests
{
    private readonly ConnectionModesTursoFixture _fixture;

    public TursoConnectionModeTests(ConnectionModesTursoFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task Open_select_one_and_ensure_created()
    {
        EnsureAvailable();

        await using var context = CreateContext(_fixture.ConnectionString);
        await EnsureModeItemsTableAsync(context);
        var name = "turso-" + Guid.NewGuid().ToString("N")[..8];
        context.Items.Add(new ModeItem { Name = name });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(
            1,
            await context.Items.CountAsync(i => i.Name == name, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EnsureDeleted_throws()
    {
        EnsureAvailable();

        await using var context = CreateContext(_fixture.ConnectionString);
        await Assert.ThrowsAsync<NotSupportedException>(
            () => context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Bad_token_does_not_leak_secret()
    {
        EnsureAvailable();

        const string badToken = "turso-bad-token-secret-value";
        var url = TestEnvironment.ExternalRemoteSqldUrl!;
        var cs = $"Data Source={url};Auth Token={badToken}";
        using var connection = new LibSqlConnection(cs);
        var ex = Assert.ThrowsAny<Exception>(() => connection.Open());
        Assert.DoesNotContain(badToken, ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Short_transaction_affinity()
    {
        EnsureAvailable();

        await using var context = CreateContext(_fixture.ConnectionString);
        await EnsureModeItemsTableAsync(context);
        await using var tx = await context.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        context.Items.Add(new ModeItem { Name = "txn-a-" + Guid.NewGuid().ToString("N")[..6] });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.Items.Add(new ModeItem { Name = "txn-b-" + Guid.NewGuid().ToString("N")[..6] });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await tx.CommitAsync(TestContext.Current.CancellationToken);
    }

    private static async Task EnsureModeItemsTableAsync(ModeDbContext context)
    {
#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "ModeItems"(
              "Id" INTEGER NOT NULL CONSTRAINT "PK_ModeItems" PRIMARY KEY AUTOINCREMENT,
              "Name" TEXT NOT NULL)
            """,
            TestContext.Current.CancellationToken);
#pragma warning restore EF1002
    }

    private void EnsureAvailable()
    {
        if (_fixture.IsAvailable)
        {
            return;
        }

        if (TestEnvironment.TursoTestsRequired)
        {
            Assert.Fail("Turso required but unavailable: " + (_fixture.UnavailableReason ?? "unknown"));
        }

        Assert.Skip(
            "Turso unavailable: "
            + (_fixture.UnavailableReason ?? "unknown")
            + $". Set {TestEnvironment.RemoteUrlEnvironmentVariable} and "
            + $"{TestEnvironment.AuthTokenEnvironmentVariable}.");
    }

    private static ModeDbContext CreateContext(string cs)
        => new(new DbContextOptionsBuilder<ModeDbContext>().UseLibSql(cs).Options);
}

public sealed class ConnectionModesTursoFixture : IAsyncLifetime
{
    public bool IsAvailable { get; private set; }
    public string? UnavailableReason { get; private set; }
    public string ConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        var url = TestEnvironment.ExternalRemoteSqldUrl;
        var token = TestEnvironment.AuthToken;
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
        {
            UnavailableReason =
                $"Set {TestEnvironment.RemoteUrlEnvironmentVariable} and "
                + $"{TestEnvironment.AuthTokenEnvironmentVariable}.";
            return;
        }

        try
        {
            ConnectionString = TestEnvironment.RemoteConnectionStringFromUrl(url.Trim(), token);
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
        }
    }

    public ValueTask DisposeAsync() => default;
}

[CollectionDefinition(Name)]
public sealed class ConnectionModesTursoCollection : ICollectionFixture<ConnectionModesTursoFixture>
{
    public const string Name = "ConnectionModesTurso";
}
