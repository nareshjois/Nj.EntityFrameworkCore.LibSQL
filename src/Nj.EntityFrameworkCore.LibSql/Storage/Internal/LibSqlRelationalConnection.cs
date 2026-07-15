// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Nelknet.LibSQL.Data;
using Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;
using Nj.EntityFrameworkCore.LibSql.Internal;

namespace Nj.EntityFrameworkCore.LibSql.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class LibSqlRelationalConnection : RelationalConnection, ILibSqlRelationalConnection
{
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Infrastructure> _logger;
    private readonly bool _loadSpatialite;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public LibSqlRelationalConnection(
        RelationalConnectionDependencies dependencies,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IDiagnosticsLogger<DbLoggerCategory.Infrastructure> logger)
        : base(dependencies)
    {
        _rawSqlCommandBuilder = rawSqlCommandBuilder;
        _logger = logger;

        var optionsExtension = dependencies.ContextOptions.Extensions.OfType<LibSqlOptionsExtension>().FirstOrDefault();
        if (optionsExtension != null)
        {
            _loadSpatialite = optionsExtension.LoadSpatialite;

            var relationalOptions = RelationalOptionsExtension.Extract(dependencies.ContextOptions);

            if (relationalOptions.Connection != null)
            {
                InitializeDbConnection(relationalOptions.Connection);
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override DbConnection CreateDbConnection()
    {
        var connection = new LibSQLConnection(GetValidatedConnectionString());
        InitializeDbConnection(connection);

        return connection;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ILibSqlRelationalConnection CreateReadOnlyConnection()
    {
        // Nelknet has no dedicated read-only open mode; reuse the same connection string
        // for Exists probes (open may fail if the local file is missing).
        var connectionString = GetValidatedConnectionString();
        var contextOptions = new DbContextOptionsBuilder().UseLibSql(connectionString).Options;

        return new LibSqlRelationalConnection(Dependencies with { ContextOptions = contextOptions }, _rawSqlCommandBuilder, _logger);
    }

    private void InitializeDbConnection(DbConnection connection)
    {
        if (_loadSpatialite)
        {
            SpatialiteLoader.Load(connection);
        }

        if (connection is not LibSQLConnection)
        {
            throw new InvalidOperationException(
                $"The connection of type '{connection.GetType().FullName}' is not supported. "
                + $"Expected '{typeof(LibSQLConnection).FullName}'.");
        }

        // Nelknet does not support sqlite3_create_function / aggregates.
        // Decimal LINQ → REAL/CAST; Regex.IsMatch → native REGEXP (docs/udf-gap.md).
    }
}
