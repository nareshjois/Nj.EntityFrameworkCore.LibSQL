using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Updates;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteUpdateDeferredTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteUpdateDeferredTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public Task Savepoints_under_user_transaction()
        => RunAsync(UpdateDeferredCases.Savepoints_under_user_transaction);

    [Fact]
    public Task Busy_locked_multi_connection_stress()
        => RunRemoteAsync(cs => UpdateDeferredCases.Busy_locked_multi_connection_stress(cs, TestContext.Current.CancellationToken));

    [Fact]
    public Task Cancellation_on_SaveChanges()
        => RunAsync(UpdateDeferredCases.Cancellation_on_SaveChanges);

    [Fact]
    public async Task Returning_disabled_with_after_trigger()
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        var sql = new SqlCaptureLogger();
        await UpdateDeferredCases.Returning_disabled_with_after_trigger(
            _fixture.ConnectionString,
            sql,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public Task Pooled_context_stress()
        => RunRemoteAsync(cs => UpdateDeferredCases.Pooled_context_stress(cs, TestContext.Current.CancellationToken));

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

    private async Task RunAsync(Func<UpdateDbContext, CancellationToken, Task> body)
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await using var context = new UpdateDbContext(
            UpdateTestHelpers.Configure(_fixture.ConnectionString).Options);

        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        try
        {
            await UpdateTestHelpers.EnsureSchemaAsync(context, TestContext.Current.CancellationToken);
            await body(context, TestContext.Current.CancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
