// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Nelknet.LibSQL.Data;
using Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;

namespace Nj.EntityFrameworkCore.LibSql.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class LibSqlDatabaseCreator : RelationalDatabaseCreator
{
    private readonly ILibSqlRelationalConnection _connection;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public LibSqlDatabaseCreator(
        RelationalDatabaseCreatorDependencies dependencies,
        ILibSqlRelationalConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder)
        : base(dependencies)
    {
        _connection = connection;
        _rawSqlCommandBuilder = rawSqlCommandBuilder;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Create()
    {
        Dependencies.Connection.Open();

        try
        {
            if (!LibSqlConnectionStringHelpers.IsRemote(Dependencies.Connection.ConnectionString))
            {
                // Prefer DELETE journal on Windows so EnsureDeleted is less likely to fight WAL sidecars.
                var journalMode = OperatingSystem.IsWindows() ? "delete" : "wal";
                _rawSqlCommandBuilder.Build($"PRAGMA journal_mode = '{journalMode}';")
                    .ExecuteNonQuery(
                        new RelationalCommandParameterObject(
                            Dependencies.Connection,
                            null,
                            null,
                            Dependencies.CurrentContext.Context,
                            Dependencies.CommandLogger, CommandSource.Migrations));
            }
        }
        finally
        {
            Dependencies.Connection.Close();
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool Exists()
    {
        var connectionString = _connection.ConnectionString;
        if (LibSqlConnectionStringHelpers.IsInMemory(connectionString)
            || LibSqlConnectionStringHelpers.IsRemote(connectionString))
        {
            // Memory always exists; remote databases are pre-provisioned endpoints.
            return true;
        }

        var path = LibSqlConnectionStringHelpers.TryGetLocalFilePath(connectionString);
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            return true;
        }

        using var probe = _connection.CreateReadOnlyConnection();
        try
        {
            probe.Open(errorsExpected: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool HasTables()
    {
        var opened = Dependencies.Connection.Open();
        try
        {
            var count = (long)_rawSqlCommandBuilder
                .Build("SELECT COUNT(*) FROM \"sqlite_master\" WHERE \"type\" = 'table' AND \"rootpage\" IS NOT NULL;")
                .ExecuteScalar(
                    new RelationalCommandParameterObject(
                        Dependencies.Connection,
                        null,
                        null,
                        Dependencies.CurrentContext.Context,
                        Dependencies.CommandLogger, CommandSource.Migrations))!;

            return count != 0;
        }
        finally
        {
            if (opened)
            {
                Dependencies.Connection.Close();
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Delete()
    {
        if (LibSqlConnectionStringHelpers.IsRemote(Dependencies.Connection.ConnectionString))
        {
            throw new NotSupportedException(
                "EnsureDeleted / database delete is not supported for remote libSQL endpoints. "
                + "Provision and tear down remote databases with Turso / sqld tooling.");
        }

        var connectionString = Dependencies.Connection.ConnectionString
            ?? throw new InvalidOperationException("A connection string is required to delete a local libSQL database.");
        var path = LibSqlConnectionStringHelpers.TryGetLocalFilePath(connectionString);
        var dbConnection = Dependencies.Connection.DbConnection;

        if (dbConnection.State != ConnectionState.Closed)
        {
            Dependencies.Connection.Close();
        }

        if (string.IsNullOrEmpty(path))
        {
            try
            {
                path = dbConnection.DataSource;
            }
            catch
            {
                // Ignore DataSource resolution failures.
            }
        }

        // MDS-shaped: close process-wide natives before File.Delete (C-005).
        if (dbConnection is LibSQLConnection libSqlConnection)
        {
            LibSQLConnection.ClearPool(libSqlConnection);
        }
        else
        {
            LibSQLConnection.ClearAllPools();
        }

        if (!string.IsNullOrEmpty(path)
            && !path.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            && File.Exists(path))
        {
            DeleteLocalDatabaseFiles(path);
        }
        else if (dbConnection.State == ConnectionState.Open)
        {
            dbConnection.Close();
            dbConnection.Open();
        }
    }

    private static void DeleteLocalDatabaseFiles(string path)
    {
        foreach (var candidate in new[] { path + "-shm", path + "-wal", path })
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            const int maxAttempts = 10;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    File.Delete(candidate);
                    break;
                }
                catch (IOException) when (attempt < maxAttempts - 1)
                {
                    LibSQLConnection.ClearAllPools();
                    Thread.Sleep(50 * (attempt + 1));
                }
            }
        }
    }
}
