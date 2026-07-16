using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

public sealed class LocalMigrationDeferredTests
{
    [Fact]
    public async Task Concurrent_migrators()
    {
        var connectionString = MigrationTestHelpers.LocalConnectionString();
        try
        {
            await MigrationDeferredCases.Concurrent_migrators(
                connectionString,
                TestContext.Current.CancellationToken);
        }
        finally
        {
            await ExtendedMigrationTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    [Fact]
    public Task Lock_recovery_after_stale_lock()
        => RunAsync(MigrationDeferredCases.Lock_recovery_after_stale_lock);

    [Fact]
    public Task Extended_migration_op_matrix()
        => RunAsync(MigrationDeferredCases.Extended_migration_op_matrix);

    [Fact]
    public Task Unsupported_sequence_operation_throws()
        => RunSync(MigrationDeferredCases.Unsupported_sequence_operation_throws);

    [Fact]
    public Task Unsupported_rename_index_throws()
        => RunSync(MigrationDeferredCases.Unsupported_rename_index_throws);

    [Fact]
    public Task Migrate_inside_user_transaction()
        => RunAsync(MigrationDeferredCases.Migrate_inside_user_transaction);

    [Fact]
    public Task Failure_rollback_and_resume()
        => RunAsync(MigrationDeferredCases.Failure_rollback_and_resume);

    [Fact]
    public Task Multi_version_chain_three_migrations()
        => RunAsync(MigrationDeferredCases.Multi_version_chain_three_migrations);

    [Fact]
    public Task Version_pin_N_to_N_plus_one()
        => RunAsync(MigrationDeferredCases.Version_pin_N_to_N_plus_one);

    private static async Task RunAsync(Func<ExtendedMigrationDbContext, CancellationToken, Task> body)
    {
        var connectionString = MigrationTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new ExtendedMigrationDbContext(
                ExtendedMigrationTestHelpers.Configure(connectionString).Options);
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            try
            {
                await body(context, TestContext.Current.CancellationToken);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            await ExtendedMigrationTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }

    private static async Task RunSync(Action<ExtendedMigrationDbContext> body)
    {
        var connectionString = MigrationTestHelpers.LocalConnectionString();
        try
        {
            await using var context = new ExtendedMigrationDbContext(
                ExtendedMigrationTestHelpers.Configure(connectionString).Options);
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            try
            {
                body(context);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
        finally
        {
            await ExtendedMigrationTestHelpers.TryDeleteLocalFileAsync(connectionString);
        }
    }
}
