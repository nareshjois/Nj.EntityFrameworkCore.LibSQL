using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Updates;

public sealed class LocalUpdateDeferredTests
{
    [Fact]
    public Task Savepoints_under_user_transaction()
        => RunAsync(UpdateDeferredCases.Savepoints_under_user_transaction);

    [Fact]
    public async Task Busy_locked_multi_connection_stress()
    {
        var connectionString = UpdateTestHelpers.LocalConnectionString();
        try
        {
            await UpdateDeferredCases.Busy_locked_multi_connection_stress(
                connectionString,
                TestContext.Current.CancellationToken);
        }
        finally
        {
            await UpdateTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    [Fact]
    public Task Cancellation_on_SaveChanges()
        => RunAsync(UpdateDeferredCases.Cancellation_on_SaveChanges);

    [Fact]
    public async Task Returning_disabled_with_after_trigger()
    {
        var connectionString = UpdateTestHelpers.LocalConnectionString();
        var sql = new SqlCaptureLogger();
        try
        {
            await UpdateDeferredCases.Returning_disabled_with_after_trigger(
                connectionString,
                sql,
                TestContext.Current.CancellationToken);
        }
        finally
        {
            await UpdateTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    [Fact]
    public async Task Pooled_context_stress()
    {
        var connectionString = UpdateTestHelpers.LocalConnectionString();
        try
        {
            await UpdateDeferredCases.Pooled_context_stress(
                connectionString,
                TestContext.Current.CancellationToken);
        }
        finally
        {
            await UpdateTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

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

    private static async Task RunAsync(Func<UpdateDbContext, CancellationToken, Task> body)
    {
        var connectionString = UpdateTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new UpdateDbContext(UpdateTestHelpers.Configure(connectionString).Options);
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
        finally
        {
            await UpdateTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }
}
