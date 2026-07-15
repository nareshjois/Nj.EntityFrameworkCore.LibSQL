using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

public sealed class LocalQueryTranslationTests
{
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

    private static async Task RunAsync(Func<QueryDbContext, SqlCaptureLogger, CancellationToken, Task> body)
    {
        var connectionString = QueryTestHelpers.LocalConnectionString();
        var sql = new SqlCaptureLogger();
        try
        {
            await using var context = new QueryDbContext(QueryTestHelpers.Configure(connectionString, sql).Options);
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
        finally
        {
            await QueryTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }
}
