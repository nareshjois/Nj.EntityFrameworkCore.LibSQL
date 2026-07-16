// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
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
    // When Windows cannot File.Delete a local db (native lock after Close — C-005),
    // we wipe schema and tombstone the path so Exists() reports false until Create().
    private static readonly ConcurrentDictionary<string, byte> DeletedLocalPaths =
        new(StringComparer.OrdinalIgnoreCase);

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
        var path = LibSqlConnectionStringHelpers.TryGetLocalFilePath(Dependencies.Connection.ConnectionString);
        if (!string.IsNullOrEmpty(path))
        {
            DeletedLocalPaths.TryRemove(NormalizePath(path), out _);
        }

        Dependencies.Connection.Open();

        try
        {
            if (!LibSqlConnectionStringHelpers.IsRemote(Dependencies.Connection.ConnectionString))
            {
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
            return true;
        }

        var path = LibSqlConnectionStringHelpers.TryGetLocalFilePath(connectionString);
        if (!string.IsNullOrEmpty(path))
        {
            if (DeletedLocalPaths.ContainsKey(NormalizePath(path)))
            {
                return false;
            }

            return File.Exists(path);
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

        if (dbConnection is LibSQLConnection libSqlConnection)
        {
            LibSQLConnection.ClearPool(libSqlConnection);
        }

        if (string.IsNullOrEmpty(path)
            || path.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            if (dbConnection.State == ConnectionState.Open)
            {
                dbConnection.Close();
                dbConnection.Open();
            }

            return;
        }

        if (!File.Exists(path))
        {
            DeletedLocalPaths.TryAdd(NormalizePath(path), 0);
            return;
        }

        if (TryDeleteFile(path + "-shm"))
        {
            // ignore failure
        }

        if (TryDeleteFile(path + "-wal"))
        {
            // ignore failure
        }

        if (TryDeleteFile(path))
        {
            DeletedLocalPaths.TryRemove(NormalizePath(path), out _);
            return;
        }

        // C-005: native lock still held — wipe user schema and tombstone the path.
        WipeLocalDatabase(connectionString);
        DeletedLocalPaths[NormalizePath(path)] = 0;
    }

    private static void WipeLocalDatabase(string connectionString)
    {
        try
        {
            using var wipe = new LibSQLConnection(connectionString);
            wipe.Open();
            try
            {
                var names = new List<string>();
                using (var list = wipe.CreateCommand())
                {
                    list.CommandText =
                        """
                        SELECT "name" FROM "sqlite_master"
                        WHERE "type" = 'table' AND "name" NOT LIKE 'sqlite_%'
                        """;
                    using var reader = list.ExecuteReader();
                    while (reader.Read())
                    {
                        names.Add(reader.GetString(0));
                    }
                }

                foreach (var name in names)
                {
                    using var drop = wipe.CreateCommand();
                    drop.CommandText = $"""DROP TABLE IF EXISTS "{name.Replace("\"", "\"\"")}" """;
                    drop.ExecuteNonQuery();
                }

                using (var views = wipe.CreateCommand())
                {
                    views.CommandText =
                        """
                        SELECT "name" FROM "sqlite_master"
                        WHERE "type" = 'view'
                        """;
                    using var reader = views.ExecuteReader();
                    var viewNames = new List<string>();
                    while (reader.Read())
                    {
                        viewNames.Add(reader.GetString(0));
                    }

                    foreach (var name in viewNames)
                    {
                        using var drop = wipe.CreateCommand();
                        drop.CommandText = $"""DROP VIEW IF EXISTS "{name.Replace("\"", "\"\"")}" """;
                        drop.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                wipe.Close();
                LibSQLConnection.ClearPool(wipe);
            }
        }
        catch
        {
            // Best-effort wipe; tombstone still makes Exists() false.
        }
    }

    private static bool TryDeleteFile(string path)
    {
        if (!File.Exists(path))
        {
            return true;
        }

        const int maxAttempts = 8;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (IOException) when (attempt < maxAttempts - 1)
            {
                Thread.Sleep(25 * (attempt + 1));
            }
            catch (IOException)
            {
                return false;
            }
        }

        return false;
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path);
}
