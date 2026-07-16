using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

public sealed class LocalQueryDeferredTests
{
    [Fact]
    public Task Tph_inheritance_query()
        => QueryDeferredCases.Tph_inheritance_query(
            QueryTestHelpers.LocalConnectionString(),
            TestContext.Current.CancellationToken);

    [Fact]
    public Task Glob_hex_substr_sql_goldens()
        => RunSeededAsync(QueryDeferredCases.Glob_hex_substr_sql_goldens);

    [Fact]
    public Task Compiled_query_smoke()
        => RunSeededAsync((ctx, _, ct) => QueryDeferredCases.Compiled_query_smoke(ctx, ct));

    [Fact]
    public Task Command_and_query_interceptors()
        => QueryDeferredCases.Command_and_query_interceptors(
            QueryTestHelpers.LocalConnectionString(),
            TestContext.Current.CancellationToken);

    private static async Task RunSeededAsync(Func<QueryDbContext, SqlCaptureLogger, CancellationToken, Task> body)
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
