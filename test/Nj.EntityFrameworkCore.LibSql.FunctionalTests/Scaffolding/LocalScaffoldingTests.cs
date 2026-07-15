using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Scaffolding;

public sealed class LocalScaffoldingTests
{
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

    [Fact]
    public void UseLibSql_codegen()
        => ScaffoldingCases.UseLibSql_codegen();

    [Fact]
    public void Design_time_di_resolves_services()
        => ScaffoldingCases.Design_time_di_resolves_services();

    private static async Task RunAsync(Func<ScaffoldProbeDbContext, string, CancellationToken, Task> body)
    {
        var connectionString = ScaffoldingTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new ScaffoldProbeDbContext(
                ScaffoldingTestHelpers.Configure(connectionString).Options);
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            try
            {
                await body(context, connectionString, TestContext.Current.CancellationToken);
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
}
