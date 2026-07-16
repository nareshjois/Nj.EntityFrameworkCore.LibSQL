using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

internal static class ExtendedMigrationTestHelpers
{
    public const string AddGizmosMigrationId = "20260716000001_AddGizmos";
    public const string AddGadgetsMigrationId = "20260716000002_AddGadgets";
    public const string AddGadgetPriorityMigrationId = "20260716000003_AddGadgetPriority";

    public static DbContextOptionsBuilder<ExtendedMigrationDbContext> Configure(string connectionString)
        => new DbContextOptionsBuilder<ExtendedMigrationDbContext>()
            .UseLibSql(
                connectionString,
                b => b.MigrationsAssembly(typeof(ExtendedMigrationDbContext).Assembly.GetName().Name!));

    public static async Task ResetExtendedArtifactsAsync(ExtendedMigrationDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            DROP TABLE IF EXISTS "Gadgets";
            DROP TABLE IF EXISTS "Gizmos";
            DROP TABLE IF EXISTS "__EFMigrationsHistory";
            DROP TABLE IF EXISTS "__EFMigrationsLock";
            """,
            cancellationToken);
        context.ChangeTracker.Clear();
    }

    public static async Task<long> TableCountAsync(
        ExtendedMigrationDbContext context,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            $"""SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = '{tableName}';""";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public static async Task<long> IndexCountAsync(
        ExtendedMigrationDbContext context,
        string indexName,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            $"""SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'index' AND "name" = '{indexName}';""";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public static async Task<long> ColumnCountAsync(
        ExtendedMigrationDbContext context,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        if (await TableCountAsync(context, tableName, cancellationToken) == 0)
        {
            return 0;
        }

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"""PRAGMA table_info("{tableName}");""";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var count = 0L;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    public static async Task<long> ForeignKeyCountAsync(
        ExtendedMigrationDbContext context,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"""PRAGMA foreign_key_list("{tableName}");""";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var count = 0L;
        while (await reader.ReadAsync(cancellationToken))
        {
            count++;
        }

        return count;
    }

    public static async Task<long> HistoryRowCountAsync(ExtendedMigrationDbContext context, CancellationToken cancellationToken)
    {
        if (await TableCountAsync(context, "__EFMigrationsHistory", cancellationToken) == 0)
        {
            return 0;
        }

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """SELECT COUNT(*) FROM "__EFMigrationsHistory";""";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public static async Task<long> LockRowCountAsync(ExtendedMigrationDbContext context, CancellationToken cancellationToken)
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
