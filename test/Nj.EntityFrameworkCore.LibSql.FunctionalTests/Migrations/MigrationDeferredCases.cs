using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Nj.EntityFrameworkCore.LibSql.Internal;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

/// <summary>
/// WP-10 deferred WP-08 migration cases (concurrency, extended ops, failure recovery).
/// </summary>
internal static class MigrationDeferredCases
{
    public static async Task Concurrent_migrators(string connectionString, CancellationToken ct)
    {
        await ResetExtendedArtifactsAsync(connectionString, ct);

        await using var context1 = CreateExtendedContext(connectionString);
        await using var context2 = CreateExtendedContext(connectionString);

        await context1.Database.OpenConnectionAsync(ct);
        await context2.Database.OpenConnectionAsync(ct);
        try
        {
            var migrate1 = context1.Database.MigrateAsync(ct);
            var migrate2 = context2.Database.MigrateAsync(ct);
            await Task.WhenAll(migrate1, migrate2);
        }
        finally
        {
            await context1.Database.CloseConnectionAsync();
            await context2.Database.CloseConnectionAsync();
        }

        await using var verify = CreateExtendedContext(connectionString);
        await verify.Database.OpenConnectionAsync(ct);
        try
        {
            Assert.Equal(1, await ExtendedMigrationTestHelpers.TableCountAsync(verify, "Gizmos", ct));
            Assert.Equal(1, await ExtendedMigrationTestHelpers.TableCountAsync(verify, "Gadgets", ct));
            Assert.Equal(3, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(verify, ct));
            Assert.Equal(0, await ExtendedMigrationTestHelpers.LockRowCountAsync(verify, ct));
        }
        finally
        {
            await verify.Database.CloseConnectionAsync();
        }
    }

    public static async Task Lock_recovery_after_stale_lock(ExtendedMigrationDbContext context, CancellationToken ct)
    {
        await ExtendedMigrationTestHelpers.ResetExtendedArtifactsAsync(context, ct);
        await context.Database.MigrateAsync(ct);
        Assert.Equal(0, await ExtendedMigrationTestHelpers.LockRowCountAsync(context, ct));

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsLock"("Id", "Timestamp") VALUES(1, '2020-01-01T00:00:00+00:00');
            """,
            ct);
        Assert.Equal(1, await ExtendedMigrationTestHelpers.LockRowCountAsync(context, ct));

        await context.Database.ExecuteSqlRawAsync("""DELETE FROM "__EFMigrationsLock";""", ct);
        Assert.Equal(0, await ExtendedMigrationTestHelpers.LockRowCountAsync(context, ct));

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(ExtendedMigrationTestHelpers.AddGadgetPriorityMigrationId, ct);
        Assert.Equal(3, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(context, ct));
        Assert.Equal(0, await ExtendedMigrationTestHelpers.LockRowCountAsync(context, ct));
    }

    public static async Task Extended_migration_op_matrix(ExtendedMigrationDbContext context, CancellationToken ct)
    {
        await ExtendedMigrationTestHelpers.ResetExtendedArtifactsAsync(context, ct);
        await context.Database.MigrateAsync(ct);

        Assert.Equal(1, await ExtendedMigrationTestHelpers.TableCountAsync(context, "Gizmos", ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.TableCountAsync(context, "Gadgets", ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.IndexCountAsync(context, "IX_Gizmos_Code", ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.IndexCountAsync(context, "IX_Gadgets_GizmoId", ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.ForeignKeyCountAsync(context, "Gadgets", ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.ColumnCountAsync(context, "Gadgets", "Priority", ct));
        Assert.Equal(3, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(context, ct));
    }

    public static void Unsupported_sequence_operation_throws(ExtendedMigrationDbContext context)
    {
        var generator = context.GetService<IMigrationsSqlGenerator>();
        var operation = new CreateSequenceOperation
        {
            Name = "MySequence",
            ClrType = typeof(long)
        };

        var ex = Assert.Throws<NotSupportedException>(() => generator.Generate([operation], context.Model));
        Assert.Equal(LibSqlStrings.SequencesNotSupported, ex.Message);
    }

    public static void Unsupported_rename_index_throws(ExtendedMigrationDbContext context)
    {
        var generator = context.GetService<IMigrationsSqlGenerator>();
        var operation = new RenameIndexOperation
        {
            Table = "Gizmos",
            Name = "IX_Gizmos_Code",
            NewName = "IX_Gizmos_Code_Renamed"
        };

        var ex = Assert.Throws<NotSupportedException>(() => generator.Generate([operation], context.Model));
        Assert.Equal(LibSqlStrings.InvalidMigrationOperation("RenameIndexOperation"), ex.Message);
    }

    public static async Task Migrate_inside_user_transaction(ExtendedMigrationDbContext context, CancellationToken ct)
    {
        await ExtendedMigrationTestHelpers.ResetExtendedArtifactsAsync(context, ct);

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await context.Database.MigrateAsync(ct);
        await transaction.CommitAsync(ct);

        Assert.Equal(3, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(context, ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.TableCountAsync(context, "Gadgets", ct));
    }

    public static async Task Failure_rollback_and_resume(ExtendedMigrationDbContext context, CancellationToken ct)
    {
        await ExtendedMigrationTestHelpers.ResetExtendedArtifactsAsync(context, ct);

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(ExtendedMigrationTestHelpers.AddGizmosMigrationId, ct);
        Assert.Equal(1, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(context, ct));

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "Gadgets" (
                "Id" INTEGER NOT NULL,
                "GizmoId" INTEGER NOT NULL,
                "Name" TEXT NOT NULL,
                CONSTRAINT "PK_Gadgets" PRIMARY KEY ("Id")
            );
            """,
            ct);

        await Assert.ThrowsAnyAsync<Exception>(() => context.Database.MigrateAsync(ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(context, ct));

        await context.Database.ExecuteSqlRawAsync("""DROP TABLE "Gadgets";""", ct);
        await context.Database.MigrateAsync(ct);

        Assert.Equal(3, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(context, ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.ColumnCountAsync(context, "Gadgets", "Priority", ct));
    }

    public static async Task Multi_version_chain_three_migrations(ExtendedMigrationDbContext context, CancellationToken ct)
    {
        await ExtendedMigrationTestHelpers.ResetExtendedArtifactsAsync(context, ct);
        await context.Database.MigrateAsync(ct);

        var applied = await context.Database.GetAppliedMigrationsAsync(ct);
        Assert.Equal(
            [
                ExtendedMigrationTestHelpers.AddGizmosMigrationId,
                ExtendedMigrationTestHelpers.AddGadgetsMigrationId,
                ExtendedMigrationTestHelpers.AddGadgetPriorityMigrationId
            ],
            applied);
    }

    public static async Task Version_pin_N_to_N_plus_one(ExtendedMigrationDbContext context, CancellationToken ct)
    {
        await ExtendedMigrationTestHelpers.ResetExtendedArtifactsAsync(context, ct);

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(ExtendedMigrationTestHelpers.AddGizmosMigrationId, ct);

        Assert.Equal(1, await ExtendedMigrationTestHelpers.TableCountAsync(context, "Gizmos", ct));
        Assert.Equal(0, await ExtendedMigrationTestHelpers.TableCountAsync(context, "Gadgets", ct));
        Assert.Equal(1, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(context, ct));

        await migrator.MigrateAsync(ExtendedMigrationTestHelpers.AddGadgetsMigrationId, ct);

        Assert.Equal(1, await ExtendedMigrationTestHelpers.TableCountAsync(context, "Gadgets", ct));
        Assert.Equal(0, await ExtendedMigrationTestHelpers.ColumnCountAsync(context, "Gadgets", "Priority", ct));
        Assert.Equal(2, await ExtendedMigrationTestHelpers.HistoryRowCountAsync(context, ct));
    }

    private static ExtendedMigrationDbContext CreateExtendedContext(string connectionString)
        => new(ExtendedMigrationTestHelpers.Configure(connectionString).Options);

    private static async Task ResetExtendedArtifactsAsync(string connectionString, CancellationToken ct)
    {
        await using var context = CreateExtendedContext(connectionString);
        await context.Database.OpenConnectionAsync(ct);
        try
        {
            await ExtendedMigrationTestHelpers.ResetExtendedArtifactsAsync(context, ct);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
