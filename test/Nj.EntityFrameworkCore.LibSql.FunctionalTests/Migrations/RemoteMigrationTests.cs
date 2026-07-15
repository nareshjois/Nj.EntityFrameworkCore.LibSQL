using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteMigrationTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteMigrationTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public Task EnsureCreated_creates_schema()
        => RunAsync(MigrationCases.EnsureCreated_creates_schema);

    [Fact]
    public Task EnsureDeleted_remote_throws()
        => RunAsync(MigrationCases.EnsureDeleted_remote_throws);

    [Fact]
    public Task Migrate_up_applies_history()
        => RunAsync(MigrationCases.Migrate_up_applies_history);

    [Fact]
    public Task Migrate_down_reverts_to_empty()
        => RunAsync(MigrationCases.Migrate_down_reverts_to_empty);

    [Fact]
    public Task Migrate_releases_lock_row()
        => RunAsync(MigrationCases.Migrate_releases_lock_row);

    private async Task RunAsync(Func<MigrationDbContext, CancellationToken, Task> body)
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await using var context = new MigrationDbContext(
            MigrationTestHelpers.Configure(_fixture.ConnectionString).Options);

        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await body(context, TestContext.Current.CancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
