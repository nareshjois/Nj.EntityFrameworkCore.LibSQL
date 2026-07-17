// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
///     Allows LibSql-specific configuration to be performed on <see cref="DbContextOptions" />.
/// </summary>
/// <remarks>
///     <para>
///         Instances of this class are returned from a call to
///         <see
///             cref="LibSqlDbContextOptionsBuilderExtensions.UseLibSql(DbContextOptionsBuilder, string, System.Action{LibSqlDbContextOptionsBuilder})" />
///         and it is not designed to be directly constructed in your application code.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see> for more information and examples.
///     </para>
/// </remarks>
public class LibSqlDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<LibSqlDbContextOptionsBuilder, LibSqlOptionsExtension>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LibSqlDbContextOptionsBuilder" /> class.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    public LibSqlDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
        : base(optionsBuilder)
    {
    }

    /// <summary>
    ///     SpatiaLite is not supported. Always throws <see cref="NotSupportedException" />.
    /// </summary>
    /// <remarks>
    ///     Nj.LibSql.Data does not expose loadable extensions. See docs/limitations.md.
    /// </remarks>
    /// <returns>Never returns.</returns>
    public virtual LibSqlDbContextOptionsBuilder UseSpatialite()
        => throw new NotSupportedException(
            "SpatiaLite / loadable SQLite extensions are not supported by Nj.EntityFrameworkCore.LibSql. "
            + "Nj.LibSql.Data does not expose sqlite3_load_extension; see docs/limitations.md.");
}
