using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Scaffolding;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteScaffoldingTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteScaffoldingTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public Task Tables_and_columns()
        => RunAsync(ScaffoldingCases.Tables_and_columns);

    [Fact]
    public Task Pk_and_autoincrement()
        => RunAsync(ScaffoldingCases.Pk_and_autoincrement);

    [Fact]
    public Task Collation()
        => RunAsync(ScaffoldingCases.Collation);

    [Fact]
    public Task Unique_index()
        => RunAsync(ScaffoldingCases.Unique_index);

    [Fact]
    public Task Foreign_key()
        => RunAsync(ScaffoldingCases.Foreign_key);

    [Fact]
    public Task View()
        => RunAsync(ScaffoldingCases.View);

    [Fact]
    public Task History_table_excluded()
        => RunAsync(ScaffoldingCases.History_table_excluded);

    private async Task RunAsync(Func<ScaffoldProbeDbContext, string, CancellationToken, Task> body)
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await using var context = new ScaffoldProbeDbContext(
            ScaffoldingTestHelpers.Configure(_fixture.ConnectionString).Options);
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await body(context, _fixture.ConnectionString, TestContext.Current.CancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
