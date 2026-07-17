// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Nj.EntityFrameworkCore.LibSql.Internal;

namespace Nj.EntityFrameworkCore.LibSql.Migrations.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class LibSqlHistoryRepository : HistoryRepository
{
    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public LibSqlHistoryRepository(HistoryRepositoryDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string ExistsSql
        => CreateExistsSql(TableName);

    /// <summary>
    ///     The name of the table that will serve as a database-wide lock for migrations.
    /// </summary>
    protected virtual string LockTableName { get; } = "__EFMigrationsLock";

    private string CreateExistsSql(string tableName)
    {
        var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));

        return $"""
SELECT COUNT(*) FROM "sqlite_master" WHERE "name" = {stringTypeMapping.GenerateSqlLiteral(tableName)} AND "type" = 'table';
""";
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool InterpretExistsResult(object? value)
        => (long)value! != 0L;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override string GetCreateIfNotExistsScript()
    {
        var script = GetCreateScript();
        return script.Insert(script.IndexOf("CREATE TABLE", StringComparison.Ordinal) + 12, " IF NOT EXISTS");
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override string GetBeginIfNotExistsScript(string migrationId)
        => throw new NotSupportedException(LibSqlStrings.MigrationScriptGenerationNotSupported);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override string GetBeginIfExistsScript(string migrationId)
        => throw new NotSupportedException(LibSqlStrings.MigrationScriptGenerationNotSupported);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override string GetEndIfScript()
        => throw new NotSupportedException(LibSqlStrings.MigrationScriptGenerationNotSupported);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override LockReleaseBehavior LockReleaseBehavior
        => LockReleaseBehavior.Explicit;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override IMigrationsDatabaseLock AcquireDatabaseLock()
    {
        Dependencies.MigrationsLogger.AcquiringMigrationLock();

        if (!InterpretExistsResult(
                Dependencies.RawSqlCommandBuilder.Build(CreateExistsSql(LockTableName))
                    .ExecuteScalar(CreateRelationalCommandParameters())))
        {
            CreateLockTableCommand().ExecuteNonQuery(CreateRelationalCommandParameters());
        }

        var retryDelay = _retryDelay;
        while (true)
        {
            var dbLock = CreateMigrationDatabaseLock();
            var timestamp = DateTimeOffset.UtcNow;
            // ExecuteScalar may not return the last result of a multi-statement
            // batch (INSERT …; SELECT changes()). Split the acquire.
            CreateInsertLockCommand(timestamp).ExecuteNonQuery(CreateRelationalCommandParameters());
            var insertCount = CreateLockOwnerCountCommand(timestamp)
                .ExecuteScalar(CreateRelationalCommandParameters());
            if (Convert.ToInt64(insertCount) == 1L)
            {
                return dbLock;
            }

            Thread.Sleep(retryDelay);
            if (retryDelay < TimeSpan.FromMinutes(1))
            {
                retryDelay = retryDelay.Add(retryDelay);
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override async Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(
        CancellationToken cancellationToken = default)
    {
        Dependencies.MigrationsLogger.AcquiringMigrationLock();

        if (!InterpretExistsResult(
                await Dependencies.RawSqlCommandBuilder.Build(CreateExistsSql(LockTableName))
                    .ExecuteScalarAsync(CreateRelationalCommandParameters(), cancellationToken).ConfigureAwait(false)))
        {
            await CreateLockTableCommand().ExecuteNonQueryAsync(CreateRelationalCommandParameters(), cancellationToken)
                .ConfigureAwait(false);
        }

        var retryDelay = _retryDelay;
        while (true)
        {
            var dbLock = CreateMigrationDatabaseLock();
            var timestamp = DateTimeOffset.UtcNow;
            await CreateInsertLockCommand(timestamp)
                .ExecuteNonQueryAsync(CreateRelationalCommandParameters(), cancellationToken)
                .ConfigureAwait(false);
            var insertCount = await CreateLockOwnerCountCommand(timestamp)
                .ExecuteScalarAsync(CreateRelationalCommandParameters(), cancellationToken)
                .ConfigureAwait(false);
            if (Convert.ToInt64(insertCount) == 1L)
            {
                return dbLock;
            }

            await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(true);
            if (retryDelay < TimeSpan.FromMinutes(1))
            {
                retryDelay = retryDelay.Add(retryDelay);
            }
        }
    }

    private IRelationalCommand CreateLockTableCommand()
        => Dependencies.RawSqlCommandBuilder.Build(
            $"""
CREATE TABLE IF NOT EXISTS "{LockTableName}" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_{LockTableName}" PRIMARY KEY,
    "Timestamp" TEXT NOT NULL
);
""");

    private IRelationalCommand CreateInsertLockCommand(DateTimeOffset timestamp)
    {
        var timestampLiteral = Dependencies.TypeMappingSource.GetMapping(typeof(DateTimeOffset)).GenerateSqlLiteral(timestamp);

        return Dependencies.RawSqlCommandBuilder.Build(
            $"""
INSERT OR IGNORE INTO "{LockTableName}"("Id", "Timestamp") VALUES(1, {timestampLiteral});
""");
    }

    private IRelationalCommand CreateLockOwnerCountCommand(DateTimeOffset timestamp)
    {
        var timestampLiteral = Dependencies.TypeMappingSource.GetMapping(typeof(DateTimeOffset)).GenerateSqlLiteral(timestamp);

        return Dependencies.RawSqlCommandBuilder.Build(
            $"""
SELECT COUNT(*) FROM "{LockTableName}" WHERE "Id" = 1 AND "Timestamp" = {timestampLiteral};
""");
    }

    private IRelationalCommand CreateDeleteLockCommand(int? id = null)
    {
        var sql = $"""
DELETE FROM "{LockTableName}"
""";
        if (id != null)
        {
            sql += $""" WHERE "Id" = {id}""";
        }

        sql += ";";
        return Dependencies.RawSqlCommandBuilder.Build(sql);
    }

    private LibSqlMigrationDatabaseLock CreateMigrationDatabaseLock()
        => new(CreateDeleteLockCommand(), CreateRelationalCommandParameters(), this);

    private RelationalCommandParameterObject CreateRelationalCommandParameters()
        => new(
            Dependencies.Connection,
            null,
            null,
            Dependencies.CurrentContext.Context,
            Dependencies.CommandLogger, CommandSource.Migrations);
}
