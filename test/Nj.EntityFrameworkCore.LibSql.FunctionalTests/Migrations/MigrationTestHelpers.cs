using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

internal static class MigrationTestHelpers
{
    public const string AddWidgetsMigrationId = "20260715000000_AddWidgets";

    public static string LocalConnectionString()
        => $"Data Source={Path.Combine(Path.GetTempPath(), "nj-libsql-m-" + Guid.NewGuid().ToString("N") + ".db")}";

    public static DbContextOptionsBuilder<MigrationDbContext> Configure(string connectionString)
        => new DbContextOptionsBuilder<MigrationDbContext>()
            .UseLibSql(
                connectionString,
                b => b.MigrationsAssembly(typeof(MigrationDbContext).Assembly.GetName().Name!));

    public static async Task EnsureSchemaViaCreateAsync(MigrationDbContext context, CancellationToken cancellationToken)
    {
        var creator = context.GetService<IRelationalDatabaseCreator>();
        await creator.EnsureCreatedAsync(cancellationToken);

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = 'Widgets';""";
        var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        if (count == 0)
        {
            await creator.CreateTablesAsync(cancellationToken);
        }
    }

    public static async Task ResetMigrationArtifactsAsync(MigrationDbContext context, CancellationToken cancellationToken)
    {
        // Best-effort wipe so remote shared DBs do not pollute cases.
        await context.Database.ExecuteSqlRawAsync(
            """
            DROP TABLE IF EXISTS "Widgets";
            DROP TABLE IF EXISTS "__EFMigrationsHistory";
            DROP TABLE IF EXISTS "__EFMigrationsLock";
            """,
            cancellationToken);
        context.ChangeTracker.Clear();
    }

    public static async Task<long> TableCountAsync(
        MigrationDbContext context,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            $"""SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = '{tableName}';""";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public static async Task<long> HistoryRowCountAsync(MigrationDbContext context, CancellationToken cancellationToken)
    {
        if (await TableCountAsync(context, "__EFMigrationsHistory", cancellationToken) == 0)
        {
            return 0;
        }

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """SELECT COUNT(*) FROM "__EFMigrationsHistory";""";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public static async Task<long> LockRowCountAsync(MigrationDbContext context, CancellationToken cancellationToken)
    {
        if (await TableCountAsync(context, "__EFMigrationsLock", cancellationToken) == 0)
        {
            return 0;
        }

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """SELECT COUNT(*) FROM "__EFMigrationsLock";""";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public static Task TryDeleteLocalFileAsync(string connectionString)
        => QueryTestHelpers.TryDeleteLocalFileAsync(connectionString);
}
