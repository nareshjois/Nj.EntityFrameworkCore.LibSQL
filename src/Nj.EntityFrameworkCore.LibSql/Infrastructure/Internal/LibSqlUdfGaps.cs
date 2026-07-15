// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;

/// <summary>
///     Helpers for SQL UDFs that Microsoft EF SQLite registers via
///     <c>SqliteConnection.CreateFunction</c> / <c>CreateAggregate</c> / <c>CreateCollation</c>.
///     Nelknet does not expose those APIs; translations must fail in-query rather than
///     emit SQL that dies at execution. See docs/udf-gap.md.
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
            + "CreateFunction/CreateAggregate/CreateCollation. Nelknet.LibSQL.Data does not "
            + $"expose those APIs. See {DocumentationPath}. "
            + "Until Nelknet adds UDF support or this provider rewrites the translation "
            + "(WP-05), avoid this LINQ shape or evaluate it on the client deliberately.");

    /// <summary>
    ///     Throws <see cref="NotSupportedException" /> for a missing UDF / collation.
    /// </summary>
    [DoesNotReturn]
    public static void Throw(string feature)
        => throw CreateException(feature);
}
