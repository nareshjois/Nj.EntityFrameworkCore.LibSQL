using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Storage;
using Nj.EntityFrameworkCore.LibSql.Scaffolding.Internal;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

public sealed class ScaffoldingColumnFacetsTests
{
    [Fact]
    public async Task DatabaseModelFactory_reads_collation_and_autoincrement_from_create_sql()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-scaffold-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using (var setup = new ScaffoldDbContext(
                             new DbContextOptionsBuilder<ScaffoldDbContext>()
                                 .UseLibSql($"Data Source={path}")
                                 .Options))
            {
                await setup.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
                await setup.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TABLE "ColumnsWithFacets" (
                      "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                      "DefaultCollation" TEXT,
                      "NonDefaultCollation" TEXT COLLATE NOCASE
                    );
                    """,
                    TestContext.Current.CancellationToken);
                await setup.Database.CloseConnectionAsync();
            }

            await using var context = new ScaffoldDbContext(
                new DbContextOptionsBuilder<ScaffoldDbContext>()
                    .UseLibSql($"Data Source={path}")
                    .Options);

            var factory = new LibSqlDatabaseModelFactory(
                context.GetService<IDiagnosticsLogger<DbLoggerCategory.Scaffolding>>(),
                context.GetService<IRelationalTypeMappingSource>());

            var model = factory.Create(
                $"Data Source={path}",
                new DatabaseModelFactoryOptions());

            var table = Assert.Single(model.Tables, t => t.Name == "ColumnsWithFacets");
            Assert.Equal(ValueGenerated.OnAdd, table.Columns.Single(c => c.Name == "Id").ValueGenerated);
            Assert.Null(table.Columns.Single(c => c.Name == "DefaultCollation").Collation);
            Assert.Equal("NOCASE", table.Columns.Single(c => c.Name == "NonDefaultCollation").Collation);
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private sealed class ScaffoldDbContext(DbContextOptions<ScaffoldDbContext> options) : DbContext(options);
}
