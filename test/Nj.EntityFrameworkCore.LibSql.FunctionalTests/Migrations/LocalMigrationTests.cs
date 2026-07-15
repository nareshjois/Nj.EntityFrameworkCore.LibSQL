using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

public sealed class LocalMigrationTests
{
    [Fact]
    public Task EnsureCreated_creates_schema()
        => RunAsync(MigrationCases.EnsureCreated_creates_schema);

    [Fact]
    public async Task EnsureDeleted_local_removes_file()
    {
        // C-005: Nelknet/libSQL keeps the local file locked after Close on Windows,
        // so File.Delete inside EnsureDeleted fails on GitHub windows-latest.
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip(
                "C-005: local EnsureDeleted file delete is locked by Nelknet after Close on Windows.");
        }

        var connectionString = MigrationTestHelpers.LocalConnectionString();
        try
        {
            await MigrationCases.EnsureDeleted_local_removes_file(
                connectionString,
                TestContext.Current.CancellationToken);
        }
        finally
        {
            await MigrationTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    [Fact]
    public Task Migrate_up_applies_history()
        => RunAsync(MigrationCases.Migrate_up_applies_history);

    [Fact]
    public Task Migrate_down_reverts_to_empty()
        => RunAsync(MigrationCases.Migrate_down_reverts_to_empty);

    [Fact]
    public Task Migrate_releases_lock_row()
        => RunAsync(MigrationCases.Migrate_releases_lock_row);

    private static async Task RunAsync(Func<MigrationDbContext, CancellationToken, Task> body)
    {
        var connectionString = MigrationTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new MigrationDbContext(MigrationTestHelpers.Configure(connectionString).Options);
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
        finally
        {
            await MigrationTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }
}
