using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteQueryDeferredTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteQueryDeferredTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public Task Tph_inheritance_query()
        => RunRemoteAsync(cs => QueryDeferredCases.Tph_inheritance_query(cs, TestContext.Current.CancellationToken));

    [Fact]
    public Task Glob_hex_substr_sql_goldens()
        => RunSeededAsync(QueryDeferredCases.Glob_hex_substr_sql_goldens);

    [Fact]
    public Task Compiled_query_smoke()
        => RunSeededAsync((ctx, _, ct) => QueryDeferredCases.Compiled_query_smoke(ctx, ct));

    [Fact]
    public Task Command_and_query_interceptors()
        => RunRemoteAsync(cs => QueryDeferredCases.Command_and_query_interceptors(cs, TestContext.Current.CancellationToken));

    private async Task RunRemoteAsync(Func<string, Task> body)
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await body(_fixture.ConnectionString);
    }

    private async Task RunSeededAsync(Func<QueryDbContext, SqlCaptureLogger, CancellationToken, Task> body)
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
