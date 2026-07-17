// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;

/// <summary>
///     Reserved helper for SQL features that Microsoft EF SQLite implements via
///     <c>SqliteConnection.CreateFunction</c> / <c>CreateAggregate</c> / <c>CreateCollation</c>
///     when Nj.LibSql.Data cannot register equivalents. Decimal and <c>Regex.IsMatch</c> no longer
///     use this path (see docs/udf-gap.md). Keep for future gap features.
/// </summary>
public static class LibSqlUdfGaps
{
    /// <summary>
    ///     Canonical doc path (repo-relative) for the capability matrix.
    /// </summary>
    public const string DocumentationPath = "docs/udf-gap.md";

    /// <summary>
    ///     Builds a <see cref="NotSupportedException" /> for a missing UDF / collation.
    /// </summary>
    public static NotSupportedException CreateException(string feature)
        => new(
            $"Translation requires the SQL helper '{feature}', which depends on ADO.NET "
            + "CreateFunction/CreateAggregate/CreateCollation. Nj.LibSql.Data does not "
            + $"expose those APIs. See {DocumentationPath}.");

    /// <summary>
    ///     Throws <see cref="NotSupportedException" /> for a missing UDF / collation.
    /// </summary>
    [DoesNotReturn]
    public static void Throw(string feature)
        => throw CreateException(feature);
}
