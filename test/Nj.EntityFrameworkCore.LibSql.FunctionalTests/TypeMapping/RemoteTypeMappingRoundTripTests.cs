using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.TypeMapping;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteTypeMappingRoundTripTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteTypeMappingRoundTripTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task Parameter_binding_round_trips_built_in_clr_types_on_remote()
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await using var context = new TypeMappingDbContext(
            new DbContextOptionsBuilder<TypeMappingDbContext>()
                .UseLibSql(_fixture.ConnectionString)
                .Options);

        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                """
                DELETE FROM "BuiltIns";
                DELETE FROM "ConverterKeys";
                DELETE FROM "JsonRows";
                DELETE FROM "Defaults";
                """,
                TestContext.Current.CancellationToken);

            var expected = TypeMappingSampleData.CreateBuiltIn();
            context.BuiltIns.Add(expected);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();

            var actual = await context.BuiltIns.SingleAsync(TestContext.Current.CancellationToken);
            TypeMappingSampleData.AssertBuiltInEqual(expected, actual);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
