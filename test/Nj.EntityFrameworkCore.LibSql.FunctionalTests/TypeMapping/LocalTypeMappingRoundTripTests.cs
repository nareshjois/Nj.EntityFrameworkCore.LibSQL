using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.TypeMapping;

public sealed class LocalTypeMappingRoundTripTests
{
    [Fact]
    public async Task Parameter_binding_round_trips_built_in_clr_types()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var expected = TypeMappingSampleData.CreateBuiltIn();
        context.BuiltIns.Add(expected);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.ChangeTracker.Clear();
        var actual = await context.BuiltIns.SingleAsync(TestContext.Current.CancellationToken);
        TypeMappingSampleData.AssertBuiltInEqual(expected, actual);
    }

    [Fact]
    public async Task Literal_select_matches_type_mapping_formats()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var expected = TypeMappingSampleData.CreateBuiltIn();
        context.BuiltIns.Add(expected);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        // Guid format is fixed test data — not user input.
#pragma warning disable EF1002
        var rows = await context.BuiltIns
            .FromSqlRaw(
                """
                SELECT * FROM "BuiltIns"
                WHERE "GuidValue" = '11111111-2222-3333-4444-555555555555'
                  AND "Flag" = 1
                  AND "IntValue" = 1000001
                  AND "DecimalValue" = '1234567890.12345'
                  AND "TextValue" = 'hello-世界'
                """)
#pragma warning restore EF1002
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(rows);
        TypeMappingSampleData.AssertBuiltInEqual(expected, rows[0]);
    }

    [Fact]
    public async Task Converter_key_round_trips()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        context.ConverterKeys.Add(new ConverterKeyRow { Id = id, Name = "key" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var loaded = await context.ConverterKeys.SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        Assert.Equal(id, loaded.Id);
        Assert.Equal("key", loaded.Name);
    }

    [Fact]
    public async Task Json_owned_and_primitive_collection_round_trip()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        context.JsonRows.Add(
            new JsonRow
            {
                Id = 1,
                Payload = new JsonPayload { Label = "alpha", Count = 3 },
                Numbers = [1, 2, 3]
            });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var loaded = await context.JsonRows.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("alpha", loaded.Payload.Label);
        Assert.Equal(3, loaded.Payload.Count);
        Assert.Equal([1, 2, 3], loaded.Numbers);
    }

    [Fact]
    public async Task Store_defaults_are_in_create_script()
    {
        await using var context = CreateContext();
        var script = context.Database.GenerateCreateScript();
        Assert.Contains("DEFAULT 42", script, StringComparison.Ordinal);
        Assert.Contains("DEFAULT 'anon'", script, StringComparison.Ordinal);
    }

    private static TypeMappingDbContext CreateContext()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-tm-" + Guid.NewGuid().ToString("N") + ".db");
        return new TypeMappingDbContext(
            new DbContextOptionsBuilder<TypeMappingDbContext>()
                .UseLibSql($"Data Source={path}")
                .Options);
    }
}
