using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteQueryTranslationTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteQueryTranslationTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public Task Filter_and_project()
        => RunAsync(QueryTranslationCases.Filter_and_project);

    [Fact]
    public Task Order_skip_take()
        => RunAsync(QueryTranslationCases.Order_skip_take);

    [Fact]
    public Task Join()
        => RunAsync(QueryTranslationCases.Join);

    [Fact]
    public Task Include_collection()
        => RunAsync(QueryTranslationCases.Include_collection);

    [Fact]
    public Task GroupBy_aggregate()
        => RunAsync(QueryTranslationCases.GroupBy_aggregate);

    [Fact]
    public Task Union_set_operation()
        => RunAsync(QueryTranslationCases.Union_set_operation);

    [Fact]
    public Task String_methods()
        => RunAsync(QueryTranslationCases.String_methods);

    [Fact]
    public Task Math_methods()
        => RunAsync(QueryTranslationCases.Math_methods);

    [Fact]
    public Task DateTime_member()
        => RunAsync(QueryTranslationCases.DateTime_member);

    [Fact]
    public Task Guid_and_bytes()
        => RunAsync(QueryTranslationCases.Guid_and_bytes);

    [Fact]
    public Task Json_and_primitive_collection()
        => RunAsync(QueryTranslationCases.Json_and_primitive_collection);

    [Fact]
    public Task FromSql_interpolated()
        => RunAsync(QueryTranslationCases.FromSql_interpolated);

    [Fact]
    public Task Sync_and_async_parity()
        => RunAsync((ctx, _, ct) => QueryTranslationCases.Sync_and_async_parity(ctx, ct));

    [Fact]
    public Task TagWith()
        => RunAsync(QueryTranslationCases.TagWith);

    private async Task RunAsync(Func<QueryDbContext, SqlCaptureLogger, CancellationToken, Task> body)
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        var sql = new SqlCaptureLogger();
        await using var context = new QueryDbContext(
            QueryTestHelpers.Configure(_fixture.ConnectionString, sql).Options);

        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await QueryTestHelpers.EnsureSchemaAsync(context, TestContext.Current.CancellationToken);
            await QueryTestHelpers.SeedAsync(context, TestContext.Current.CancellationToken);
            await body(context, sql, TestContext.Current.CancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
