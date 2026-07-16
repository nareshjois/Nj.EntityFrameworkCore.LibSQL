using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.DependencyInjection;
using Nj.EntityFrameworkCore.LibSql.Design.Internal;
using Nj.EntityFrameworkCore.LibSql.Internal;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Scaffolding;

public sealed class ScaffoldingGoldenTests
{
    [Fact]
    public async Task Reverse_engineer_emits_UseLibSql_and_entities()
    {
        var connectionString = ScaffoldingTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new ScaffoldProbeDbContext(
                ScaffoldingTestHelpers.Configure(connectionString).Options);
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            try
            {
                await ScaffoldingTestHelpers.ApplySchemaAsync(
                    context,
                    TestContext.Current.CancellationToken);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }

            using var root = CreateFullDesignServices();
            using var scope = root.CreateScope();
            var scaffolder = scope.ServiceProvider.GetRequiredService<IReverseEngineerScaffolder>();
            var scaffolded = scaffolder.ScaffoldModel(
                connectionString,
                new DatabaseModelFactoryOptions(),
                new ModelReverseEngineerOptions(),
                new ModelCodeGenerationOptions
                {
                    ContextName = "GoldenContext",
                    ConnectionString = connectionString,
                    ModelNamespace = "Nj.Goldens",
                    ContextNamespace = "Nj.Goldens",
                    RootNamespace = "Nj.Goldens",
                    Language = "C#",
                    UseDataAnnotations = false,
                    UseNullableReferenceTypes = true,
                });

            var contextCode = scaffolded.ContextFile.Code;
            Assert.Contains("UseLibSql", contextCode, StringComparison.Ordinal);
            Assert.Contains("class GoldenContext", contextCode, StringComparison.Ordinal);

            var allCode = string.Join(
                "\n",
                new[] { contextCode }.Concat(scaffolded.AdditionalFiles.Select(f => f.Code)));
            Assert.Contains("Parent", allCode, StringComparison.Ordinal);
            Assert.Contains("Child", allCode, StringComparison.Ordinal);
            Assert.Contains("DbSet<", contextCode, StringComparison.Ordinal);
            Assert.Contains("OnModelCreating", contextCode, StringComparison.Ordinal);
        }
        finally
        {
            await ScaffoldingTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    [Fact]
    public async Task Migration_create_script_contains_expected_ddl()
    {
        var connectionString = ScaffoldingTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new MigrationProbeDbContext(
                new DbContextOptionsBuilder<MigrationProbeDbContext>()
                    .UseLibSql(connectionString)
                    .Options);

            var script = context.Database.GenerateCreateScript();
            Assert.Contains("CREATE TABLE", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"Widgets\"", script, StringComparison.Ordinal);
            Assert.Contains("\"Name\"", script, StringComparison.Ordinal);
        }
        finally
        {
            await ScaffoldingTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    [Fact]
    public async Task Idempotent_migration_script_helpers_throw()
    {
        var connectionString = ScaffoldingTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new MigrationProbeDbContext(
                new DbContextOptionsBuilder<MigrationProbeDbContext>()
                    .UseLibSql(connectionString)
                    .Options);

            var history = context.GetService<IHistoryRepository>();
            var ex1 = Assert.Throws<NotSupportedException>(
                () => history.GetBeginIfNotExistsScript("20260101000000_Test"));
            Assert.Contains(
                "not currently supported",
                ex1.Message,
                StringComparison.OrdinalIgnoreCase);

            var ex2 = Assert.Throws<NotSupportedException>(
                () => history.GetBeginIfExistsScript("20260101000000_Test"));
            Assert.Contains(
                "not currently supported",
                ex2.Message,
                StringComparison.OrdinalIgnoreCase);

            var ex3 = Assert.Throws<NotSupportedException>(() => history.GetEndIfScript());
            Assert.Contains(
                "not currently supported",
                ex3.Message,
                StringComparison.OrdinalIgnoreCase);

            Assert.Contains(
                "not currently supported",
                LibSqlStrings.MigrationScriptGenerationNotSupported,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await ScaffoldingTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    [Fact]
    public async Task Virtual_tables_are_not_scaffolded()
    {
        var connectionString = ScaffoldingTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new ScaffoldProbeDbContext(
                ScaffoldingTestHelpers.Configure(connectionString).Options);
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    """CREATE TABLE "Regular" ("Id" INTEGER NOT NULL PRIMARY KEY, "Name" TEXT NOT NULL)""",
                    TestContext.Current.CancellationToken);
                await context.Database.ExecuteSqlRawAsync(
                    """CREATE VIRTUAL TABLE "DocsFts" USING fts5(content)""",
                    TestContext.Current.CancellationToken);

                var model = ScaffoldingTestHelpers.CreateModel(context);
                Assert.Contains(model.Tables, t => t.Name == "Regular");
                Assert.DoesNotContain(model.Tables, t => t.Name == "DocsFts");
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            await ScaffoldingTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    private static ServiceProvider CreateFullDesignServices()
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkDesignTimeServices();
        new LibSqlDesignTimeServices().ConfigureDesignTimeServices(services);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private sealed class MigrationProbeDbContext(DbContextOptions<MigrationProbeDbContext> options)
        : DbContext(options)
    {
        public DbSet<Widget> Widgets => Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Widget>(e =>
            {
                e.Property(x => x.Name).IsRequired();
            });
        }
    }

    private sealed class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
