// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;

/// <summary>
///     Static capability flags for this provider's bundled libSQL / SQLite feature level.
///     Seeded from Nelknet driver-contract findings (SQLite 3.45.1) so query/update
///     translation does not probe Microsoft.Data.Sqlite.
/// </summary>
public static class LibSqlDatabaseCapabilities
{
    /// <summary>
    ///     Reported bundled SQLite-compatible version used for feature gates.
    /// </summary>
    public static Version BundledSqliteVersion { get; } = new(3, 45, 1);

    /// <summary>
    ///     INSERT/UPDATE/DELETE … RETURNING (SQLite 3.35+).
    /// </summary>
    public static bool SupportsReturningClause
        => BundledSqliteVersion >= new Version(3, 35);

    /// <summary>
    ///     JSON1 functions used by query translation (SQLite 3.38+).
    /// </summary>
    public static bool SupportsJsonFunctions
        => BundledSqliteVersion >= new Version(3, 38);
}
