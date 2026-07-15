using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Updates;

public sealed class LocalUpdateTests
{
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

    private static async Task RunAsync(Func<UpdateDbContext, SqlCaptureLogger, CancellationToken, Task> body)
    {
        var connectionString = UpdateTestHelpers.LocalConnectionString();
        var sql = new SqlCaptureLogger();
        try
        {
            await using var context = new UpdateDbContext(UpdateTestHelpers.Configure(connectionString, sql).Options);
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
        finally
        {
            await UpdateTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }
}
