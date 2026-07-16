using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

[Collection(RemoteLibSqlCollection.Name)]
public sealed class RemoteMigrationDeferredTests
{
    private readonly RemoteLibSqlFixture _fixture;

    public RemoteMigrationDeferredTests(RemoteLibSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public Task Concurrent_migrators()
        => RunRemoteAsync(cs => MigrationDeferredCases.Concurrent_migrators(cs, TestContext.Current.CancellationToken));

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

    private async Task RunAsync(Func<ExtendedMigrationDbContext, CancellationToken, Task> body)
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await using var context = new ExtendedMigrationDbContext(
            ExtendedMigrationTestHelpers.Configure(_fixture.ConnectionString).Options);
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

    private async Task RunSync(Action<ExtendedMigrationDbContext> body)
    {
        if (!_fixture.IsAvailable)
        {
            Assert.Skip(
                "Remote sqld unavailable: "
                + (_fixture.UnavailableReason ?? "unknown")
                + ". Docker + Testcontainers start sqld automatically; or set LIBSQL_TEST_URL.");
        }

        await using var context = new ExtendedMigrationDbContext(
            ExtendedMigrationTestHelpers.Configure(_fixture.ConnectionString).Options);
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
}
