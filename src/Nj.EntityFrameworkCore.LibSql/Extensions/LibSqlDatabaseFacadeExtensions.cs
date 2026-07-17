// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;
using Nj.LibSql.Data;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     LibSQL-specific extension methods for <see cref="DbContext.Database" />.
/// </summary>
public static class LibSqlDatabaseFacadeExtensions
{
    /// <summary>
    ///     Returns <see langword="true" /> if the database provider currently in use is the LibSQL provider.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method can only be used after the <see cref="DbContext" /> has been configured because
    ///         it is only then that the provider is known. This means that this method cannot be used
    ///         in <see cref="DbContext.OnConfiguring" /> because this is where application code sets the
    ///         provider to use as part of configuring the context.
    ///     </para>
    /// </remarks>
    /// <param name="database">The facade from <see cref="DbContext.Database" />.</param>
    /// <returns><see langword="true" /> if LibSQL is being used; <see langword="false" /> otherwise.</returns>
    public static bool IsLibSql(this DatabaseFacade database)
        => database.ProviderName == typeof(LibSqlOptionsExtension).Assembly.GetName().Name;

    /// <summary>
    ///     Synchronizes an embedded-replica database with its remote primary
    ///     (<c>libsql_sync2</c> via <see cref="LibSqlConnection.Sync"/>).
    /// </summary>
    /// <remarks>
    ///     Consistency matches the driver/native settings
    ///     (<c>Read Your Writes</c>, <c>Sync Interval</c>). This API does not claim
    ///     stronger guarantees than the underlying libSQL client.
    /// </remarks>
    /// <param name="database">The facade from <see cref="DbContext.Database" />.</param>
    /// <returns>Native sync frame statistics.</returns>
    public static LibSqlSyncResult Sync(this DatabaseFacade database)
    {
        ArgumentNullException.ThrowIfNull(database);

        if (!database.IsLibSql())
        {
            throw new InvalidOperationException(
                "Database.Sync requires the Nj.EntityFrameworkCore.LibSql provider.");
        }

        var connection = GetLibSqlConnection(database);
        var openedHere = false;
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
            openedHere = true;
        }

        try
        {
            return connection.Sync();
        }
        finally
        {
            if (openedHere)
            {
                connection.Close();
            }
        }
    }

    /// <summary>
    ///     Asynchronously synchronizes an embedded-replica database with its remote primary.
    /// </summary>
    /// <param name="database">The facade from <see cref="DbContext.Database" />.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Native sync frame statistics.</returns>
    public static async Task<LibSqlSyncResult> SyncAsync(
        this DatabaseFacade database,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);

        if (!database.IsLibSql())
        {
            throw new InvalidOperationException(
                "Database.SyncAsync requires the Nj.EntityFrameworkCore.LibSql provider.");
        }

        var connection = GetLibSqlConnection(database);
        var openedHere = false;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            openedHere = true;
        }

        try
        {
            return await connection.SyncAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private static LibSqlConnection GetLibSqlConnection(DatabaseFacade database)
    {
        var connection = database.GetDbConnection();
        if (connection is LibSqlConnection libSqlConnection)
        {
            return libSqlConnection;
        }

        throw new InvalidOperationException(
            "Database.Sync requires an underlying Nj.LibSql.Data.LibSqlConnection "
            + $"(got {connection.GetType().FullName}).");
    }
}
