// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Nj.LibSql.Data;

namespace Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;

/// <summary>
///     Connection-string helpers for local vs remote libSQL detection.
/// </summary>
public static class LibSqlConnectionStringHelpers
{
    /// <summary>
    ///     Parses a LibSql connection string; returns null if empty/invalid.
    /// </summary>
    public static LibSqlConnectionStringBuilder? TryParse(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        try
        {
            return new LibSqlConnectionStringBuilder(connectionString);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     True when the data source is an in-memory database.
    /// </summary>
    public static bool IsInMemory(string? connectionString)
    {
        var builder = TryParse(connectionString);
        if (builder is null)
        {
            return false;
        }

        var dataSource = builder.DataSource ?? string.Empty;
        return dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
               || dataSource.StartsWith("file:memory:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     True when the connection string targets a remote HTTP(S)/libsql endpoint.
    /// </summary>
    public static bool IsRemote(string? connectionString)
    {
        var builder = TryParse(connectionString);
        if (builder is null)
        {
            return false;
        }

        if (builder.Mode == LibSqlConnectionMode.Remote)
        {
            return true;
        }

        var dataSource = builder.DataSource ?? string.Empty;
        return dataSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || dataSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || dataSource.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase)
               || dataSource.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
               || dataSource.StartsWith("wss://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Best-effort local file path from a connection string (null for memory/remote).
    /// </summary>
    public static string? TryGetLocalFilePath(string? connectionString)
    {
        if (IsInMemory(connectionString) || IsRemote(connectionString))
        {
            return null;
        }

        var builder = TryParse(connectionString);
        var dataSource = builder?.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return dataSource;
    }
}
