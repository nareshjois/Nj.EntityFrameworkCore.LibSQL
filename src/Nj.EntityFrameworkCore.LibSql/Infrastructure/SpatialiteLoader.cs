// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;

namespace Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
///     SpatiaLite / loadable SQLite extensions are not supported by this provider.
/// </summary>
/// <remarks>
///     Nj.LibSql.Data does not expose <c>sqlite3_load_extension</c>. Remote <c>sqld</c>
///     endpoints also typically disallow arbitrary extension loading. See docs/limitations.md.
/// </remarks>
public static class SpatialiteLoader
{
    /// <summary>
    ///     Always returns <see langword="false" />. Loadable extensions are not supported.
    /// </summary>
    public static bool TryLoad(DbConnection connection)
    {
        Check.NotNull(connection);
        return false;
    }

    /// <summary>
    ///     Always throws. Loadable extensions (including SpatiaLite) are not supported.
    /// </summary>
    public static void Load(DbConnection connection)
    {
        Check.NotNull(connection);
        throw new NotSupportedException(
            "Loading SpatiaLite or other SQLite extensions is not supported by Nj.EntityFrameworkCore.LibSql. "
            + "Nj.LibSql.Data does not expose sqlite3_load_extension; see docs/limitations.md.");
    }
}
