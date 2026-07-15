using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
            // EnsureCreated only creates when the database has zero tables. A shared sqld
            // endpoint often already has unrelated tables, so create our schema explicitly
            // when BuiltIns is missing.
            await EnsureTypeMappingSchemaAsync(context, TestContext.Current.CancellationToken);

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

    private static async Task EnsureTypeMappingSchemaAsync(DbContext context, CancellationToken cancellationToken)
    {
        var creator = context.GetService<IRelationalDatabaseCreator>();
        await creator.EnsureCreatedAsync(cancellationToken);

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = 'BuiltIns';""";
        var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        if (count == 0)
        {
            await creator.CreateTablesAsync(cancellationToken);
        }
    }
}
