using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Nj.EntityFrameworkCore.LibSql.Design.Internal;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;
using Nj.EntityFrameworkCore.LibSql.Scaffolding.Internal;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Scaffolding;

internal static class ScaffoldingTestHelpers
{
    public static string LocalConnectionString()
        => $"Data Source={Path.Combine(Path.GetTempPath(), "nj-libsql-s-" + Guid.NewGuid().ToString("N") + ".db")}";

    public static DbContextOptionsBuilder<ScaffoldProbeDbContext> Configure(string connectionString)
        => new DbContextOptionsBuilder<ScaffoldProbeDbContext>()
            .UseLibSql(connectionString);

    public static async Task ApplySchemaAsync(ScaffoldProbeDbContext context, CancellationToken cancellationToken)
    {
        // Nelknet may not execute multi-statement batches as one ADO.NET command.
        foreach (var statement in ScaffoldingSampleSchema.Statements)
        {
            await context.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }

        // Seed one row so InferClrTypes sampling is well-defined on empty remotes.
        await context.Database.ExecuteSqlRawAsync(
            """INSERT INTO "Parents" ("Name", "Label") VALUES ('seed', 'Seed')""",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            """INSERT INTO "Children" ("ParentId", "Code") VALUES (1, 'C1')""",
            cancellationToken);

        context.ChangeTracker.Clear();
    }

    public static IDatabaseModelFactory CreateFactory(ScaffoldProbeDbContext context)
        => new LibSqlDatabaseModelFactory(
            context.GetService<IDiagnosticsLogger<DbLoggerCategory.Scaffolding>>(),
            context.GetService<IRelationalTypeMappingSource>());

    public static DatabaseModel CreateModel(ScaffoldProbeDbContext context)
    {
        var factory = CreateFactory(context);
        var connection = context.Database.GetDbConnection();
        return factory.Create(connection, new DatabaseModelFactoryOptions());
    }

    public static ServiceProvider CreateDesignTimeServices()
    {
        var services = new ServiceCollection();
        new LibSqlDesignTimeServices().ConfigureDesignTimeServices(services);
        return services.BuildServiceProvider();
    }

    public static Task TryDeleteLocalFileAsync(string connectionString)
        => QueryTestHelpers.TryDeleteLocalFileAsync(connectionString);
}

internal sealed class ScaffoldProbeDbContext(DbContextOptions<ScaffoldProbeDbContext> options) : DbContext(options);
