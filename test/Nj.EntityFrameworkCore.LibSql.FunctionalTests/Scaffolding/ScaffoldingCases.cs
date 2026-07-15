using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Nj.EntityFrameworkCore.LibSql.Scaffolding.Internal;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Scaffolding;

/// <summary>
/// Shared scaffolding matrix exercised by local and remote hosts.
/// </summary>
internal static class ScaffoldingCases
{
    public static async Task Tables_and_columns(ScaffoldProbeDbContext context, string connectionString, CancellationToken ct)
    {
        await ScaffoldingTestHelpers.ApplySchemaAsync(context, ct);
        var model = ScaffoldingTestHelpers.CreateModel(context);

        var parents = Assert.Single(model.Tables, t => t.Name == "Parents" && t is not DatabaseView);
        var name = Assert.Single(parents.Columns, c => c.Name == "Name");
        Assert.False(name.IsNullable);
        Assert.Contains("anon", name.DefaultValueSql ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Equal("TEXT", name.StoreType, ignoreCase: true);
    }

    public static async Task Pk_and_autoincrement(ScaffoldProbeDbContext context, string connectionString, CancellationToken ct)
    {
        await ScaffoldingTestHelpers.ApplySchemaAsync(context, ct);
        var model = ScaffoldingTestHelpers.CreateModel(context);

        var parents = Assert.Single(model.Tables, t => t.Name == "Parents" && t is not DatabaseView);
        var id = Assert.Single(parents.Columns, c => c.Name == "Id");
        Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);
        Assert.NotNull(parents.PrimaryKey);
        Assert.Contains(parents.PrimaryKey!.Columns, c => c.Name == "Id");
    }

    public static async Task Collation(ScaffoldProbeDbContext context, string connectionString, CancellationToken ct)
    {
        await ScaffoldingTestHelpers.ApplySchemaAsync(context, ct);
        var model = ScaffoldingTestHelpers.CreateModel(context);

        var parents = Assert.Single(model.Tables, t => t.Name == "Parents" && t is not DatabaseView);
        Assert.Equal("NOCASE", parents.Columns.Single(c => c.Name == "Label").Collation);
        Assert.Null(parents.Columns.Single(c => c.Name == "Name").Collation);
    }

    public static async Task Unique_index(ScaffoldProbeDbContext context, string connectionString, CancellationToken ct)
    {
        await ScaffoldingTestHelpers.ApplySchemaAsync(context, ct);
        var model = ScaffoldingTestHelpers.CreateModel(context);

        var children = Assert.Single(model.Tables, t => t.Name == "Children" && t is not DatabaseView);
        var unique = Assert.Single(children.Indexes, i => i.Name == "IX_Children_Code");
        Assert.True(unique.IsUnique);
        Assert.Contains(unique.Columns, c => c.Name == "Code");
    }

    public static async Task Foreign_key(ScaffoldProbeDbContext context, string connectionString, CancellationToken ct)
    {
        await ScaffoldingTestHelpers.ApplySchemaAsync(context, ct);
        var model = ScaffoldingTestHelpers.CreateModel(context);

        var children = Assert.Single(model.Tables, t => t.Name == "Children" && t is not DatabaseView);
        var fk = Assert.Single(children.ForeignKeys);
        Assert.Equal("Parents", fk.PrincipalTable.Name);
        Assert.Contains(fk.Columns, c => c.Name == "ParentId");
        Assert.Contains(fk.PrincipalColumns, c => c.Name == "Id");
    }

    public static async Task View(ScaffoldProbeDbContext context, string connectionString, CancellationToken ct)
    {
        await ScaffoldingTestHelpers.ApplySchemaAsync(context, ct);
        var model = ScaffoldingTestHelpers.CreateModel(context);

        var view = Assert.IsType<DatabaseView>(Assert.Single(model.Tables, t => t.Name == "ParentNames"));
        Assert.Contains(view.Columns, c => c.Name == "Id");
        Assert.Contains(view.Columns, c => c.Name == "Name");
    }

    public static async Task History_table_excluded(ScaffoldProbeDbContext context, string connectionString, CancellationToken ct)
    {
        await ScaffoldingTestHelpers.ApplySchemaAsync(context, ct);
        var model = ScaffoldingTestHelpers.CreateModel(context);

        Assert.DoesNotContain(model.Tables, t => t.Name == "__EFMigrationsHistory");
        Assert.Contains(model.Tables, t => t.Name == "Parents");
    }

    public static void UseLibSql_codegen()
    {
        using var provider = ScaffoldingTestHelpers.CreateDesignTimeServices();
        var codegen = provider.GetRequiredService<IProviderConfigurationCodeGenerator>();
        var fragment = codegen.GenerateUseProvider("Data Source=test.db", providerOptions: null);

        Assert.Equal(nameof(LibSqlDbContextOptionsBuilderExtensions.UseLibSql), fragment.Method);
        Assert.Contains(fragment.Arguments, a => a is string s && s.Contains("Data Source=test.db", StringComparison.Ordinal));
    }

    public static void Design_time_di_resolves_services()
    {
        using var provider = ScaffoldingTestHelpers.CreateDesignTimeServices();
        Assert.NotNull(provider.GetService<IDatabaseModelFactory>());
        Assert.NotNull(provider.GetService<IProviderConfigurationCodeGenerator>());
        Assert.IsType<LibSqlDatabaseModelFactory>(
            provider.GetRequiredService<IDatabaseModelFactory>());
        Assert.IsType<LibSqlCodeGenerator>(
            provider.GetRequiredService<IProviderConfigurationCodeGenerator>());
    }
}
