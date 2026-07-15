using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Updates;

internal static class UpdateTestHelpers
{
    public static string LocalConnectionString()
        => $"Data Source={Path.Combine(Path.GetTempPath(), "nj-libsql-u-" + Guid.NewGuid().ToString("N") + ".db")}";

    public static DbContextOptionsBuilder<UpdateDbContext> Configure(
        string connectionString,
        SqlCaptureLogger? sqlCapture = null)
    {
        var builder = new DbContextOptionsBuilder<UpdateDbContext>()
            .UseLibSql(connectionString);

        if (sqlCapture is not null)
        {
            builder.LogTo(sqlCapture.Write, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
                .EnableSensitiveDataLogging();
        }

        return builder;
    }

    public static async Task EnsureSchemaAsync(UpdateDbContext context, CancellationToken cancellationToken)
    {
        var creator = context.GetService<IRelationalDatabaseCreator>();
        await creator.EnsureCreatedAsync(cancellationToken);

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = 'Accounts';""";
        var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        if (count == 0)
        {
            await creator.CreateTablesAsync(cancellationToken);
        }
    }

    public static async Task ResetAsync(UpdateDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "Accounts";
            DELETE FROM "VersionedItems";
            """,
            cancellationToken);
        context.ChangeTracker.Clear();
    }

    public static Task TryDeleteLocalFileAsync(string connectionString)
        => QueryTestHelpers.TryDeleteLocalFileAsync(connectionString);
}
