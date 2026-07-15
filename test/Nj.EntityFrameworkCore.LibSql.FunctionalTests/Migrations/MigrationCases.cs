using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

/// <summary>
/// Shared migrations matrix exercised by local and remote hosts.
/// </summary>
internal static class MigrationCases
{
    public static async Task EnsureCreated_creates_schema(MigrationDbContext context, CancellationToken ct)
    {
        await MigrationTestHelpers.ResetMigrationArtifactsAsync(context, ct);
        await MigrationTestHelpers.EnsureSchemaViaCreateAsync(context, ct);

        Assert.Equal(1, await MigrationTestHelpers.TableCountAsync(context, "Widgets", ct));
    }

    public static async Task EnsureDeleted_local_removes_file(string connectionString, CancellationToken ct)
    {
        await using (var context = new MigrationDbContext(MigrationTestHelpers.Configure(connectionString).Options))
        {
            await context.Database.OpenConnectionAsync(ct);
            try
            {
                await MigrationTestHelpers.EnsureSchemaViaCreateAsync(context, ct);
                Assert.Equal(1, await MigrationTestHelpers.TableCountAsync(context, "Widgets", ct));
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }

        // Fresh context so any prior ADO handles are released before file delete (Windows).
        GC.Collect();
        GC.WaitForPendingFinalizers();

        await using (var context = new MigrationDbContext(MigrationTestHelpers.Configure(connectionString).Options))
        {
            await context.Database.EnsureDeletedAsync(ct);
        }

        var path = ExtractLocalPath(connectionString);
        Assert.False(string.IsNullOrEmpty(path));
        Assert.False(File.Exists(path), "Local EnsureDeleted should remove the database file.");

        // Reopen on a fresh file should not carry over Widgets until created again.
        await using var verify = new MigrationDbContext(MigrationTestHelpers.Configure(connectionString).Options);
        await verify.Database.OpenConnectionAsync(ct);
        try
        {
            Assert.Equal(0, await MigrationTestHelpers.TableCountAsync(verify, "Widgets", ct));
        }
        finally
        {
            await verify.Database.CloseConnectionAsync();
        }
    }

    public static async Task EnsureDeleted_remote_throws(MigrationDbContext context, CancellationToken ct)
    {
        await MigrationTestHelpers.ResetMigrationArtifactsAsync(context, ct);
        await MigrationTestHelpers.EnsureSchemaViaCreateAsync(context, ct);

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => context.Database.EnsureDeletedAsync(ct));
        Assert.Contains("remote", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task Migrate_up_applies_history(MigrationDbContext context, CancellationToken ct)
    {
        await MigrationTestHelpers.ResetMigrationArtifactsAsync(context, ct);

        await context.Database.MigrateAsync(ct);

        Assert.Equal(1, await MigrationTestHelpers.TableCountAsync(context, "Widgets", ct));
        Assert.Equal(1, await MigrationTestHelpers.TableCountAsync(context, "__EFMigrationsHistory", ct));
        Assert.Equal(1, await MigrationTestHelpers.HistoryRowCountAsync(context, ct));

        var applied = await context.Database.GetAppliedMigrationsAsync(ct);
        Assert.Contains(MigrationTestHelpers.AddWidgetsMigrationId, applied);
    }

    public static async Task Migrate_down_reverts_to_empty(MigrationDbContext context, CancellationToken ct)
    {
        await MigrationTestHelpers.ResetMigrationArtifactsAsync(context, ct);
        await context.Database.MigrateAsync(ct);
        Assert.Equal(1, await MigrationTestHelpers.TableCountAsync(context, "Widgets", ct));

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("0", ct);

        Assert.Equal(0, await MigrationTestHelpers.TableCountAsync(context, "Widgets", ct));
        Assert.Equal(0, await MigrationTestHelpers.HistoryRowCountAsync(context, ct));
    }

    public static async Task Migrate_releases_lock_row(MigrationDbContext context, CancellationToken ct)
    {
        await MigrationTestHelpers.ResetMigrationArtifactsAsync(context, ct);
        await context.Database.MigrateAsync(ct);

        Assert.Equal(0, await MigrationTestHelpers.LockRowCountAsync(context, ct));
    }

    private static string? ExtractLocalPath(string connectionString)
    {
        const string prefix = "Data Source=";
        var idx = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        var path = connectionString[(idx + prefix.Length)..].Trim();
        var semi = path.IndexOf(';');
        return semi >= 0 ? path[..semi] : path;
    }
}
