using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Updates;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteUpdateTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteUpdateTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public Task Crud_SaveChanges()
        => RunAsync(UpdateCases.Crud_SaveChanges);

    [Fact]
    public Task Multi_entity_auto_transaction_rolls_back()
        => RunAsync(UpdateCases.Multi_entity_auto_transaction_rolls_back);

    [Fact]
    public Task Explicit_transaction_commit()
        => RunAsync(UpdateCases.Explicit_transaction_commit);

    [Fact]
    public Task Explicit_transaction_rollback()
        => RunAsync(UpdateCases.Explicit_transaction_rollback);

    [Fact]
    public Task Optimistic_concurrency_conflict()
        => RunAsync(UpdateCases.Optimistic_concurrency_conflict);

    [Fact]
    public Task ExecuteUpdate_and_ExecuteDelete()
        => RunAsync(UpdateCases.ExecuteUpdate_and_ExecuteDelete);

    [Fact]
    public Task Constraint_violation_after_prior_commit_isolated()
        => RunAsync(UpdateCases.Constraint_violation_after_prior_commit_isolated);

    private async Task RunAsync(Func<UpdateDbContext, SqlCaptureLogger, CancellationToken, Task> body)
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        var sql = new SqlCaptureLogger();
        await using var context = new UpdateDbContext(
            UpdateTestHelpers.Configure(_fixture.ConnectionString, sql).Options);

        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await UpdateTestHelpers.EnsureSchemaAsync(context, TestContext.Current.CancellationToken);
            await body(context, sql, TestContext.Current.CancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
