// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Nj.EntityFrameworkCore.LibSql.Diagnostics.Internal;
using Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;
using Nj.EntityFrameworkCore.LibSql.Metadata.Internal;
using Nj.EntityFrameworkCore.LibSql.Migrations.Internal;
using Nj.EntityFrameworkCore.LibSql.Query.Internal;
using Nj.EntityFrameworkCore.LibSql.Storage.Internal;
using Nj.EntityFrameworkCore.LibSql.Update.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     SQLite specific extension methods for <see cref="IServiceCollection" />.
/// </summary>
public static class LibSqlServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the given Entity Framework <see cref="DbContext" /> as a service in the <see cref="IServiceCollection" />
    ///     and configures it to connect to a SQLite database.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is a shortcut for configuring a <see cref="DbContext" /> to use SQLite. It does not support all options.
    ///         Use <see cref="O:EntityFrameworkServiceCollectionExtensions.AddDbContext" /> and related methods for full control of
    ///         this process.
    ///     </para>
    ///     <para>
    ///         Use this method when using dependency injection in your application, such as with ASP.NET Core.
    ///         For applications that don't use dependency injection, consider creating <see cref="DbContext" />
    ///         instances directly with its constructor. The <see cref="DbContext.OnConfiguring" /> method can then be
    ///         overridden to configure the SQLite provider and connection string.
    ///     </para>
    ///     <para>
    ///         To configure the <see cref="DbContextOptions{TContext}" /> for the context, either override the
    ///         <see cref="DbContext.OnConfiguring" /> method in your derived context, or supply
    ///         an optional action to configure the <see cref="DbContextOptions" /> for the context.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-di">Using DbContext with dependency injection</see> for more information and examples.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///         <see href="https://aka.ms/efcore-docs-sqlite">Accessing SQLite databases with EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be registered.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="sqliteOptionsAction">An optional action to allow additional SQLite specific configuration.</param>
    /// <param name="optionsAction">An optional action to configure the <see cref="DbContextOptions" /> for the context.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddLibSql<TContext>(
        this IServiceCollection serviceCollection,
        string? connectionString,
        Action<LibSqlDbContextOptionsBuilder>? sqliteOptionsAction = null,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
        => serviceCollection.AddDbContext<TContext>((_, options) =>
        {
            optionsAction?.Invoke(options);
            options.UseLibSql(connectionString, sqliteOptionsAction);
        });

    /// <summary>
    ///     <para>
    ///         Adds the services required by the SQLite database provider for Entity Framework
    ///         to an <see cref="IServiceCollection" />.
    ///     </para>
    ///     <para>
    ///         Warning: Do not call this method accidentally. It is much more likely you need
    ///         to call <see cref="AddLibSql{TContext}" />.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Calling this method is no longer necessary when building most applications, including those that
    ///     use dependency injection in ASP.NET or elsewhere.
    ///     It is only needed when building the internal service provider for use with
    ///     the <see cref="DbContextOptionsBuilder.UseInternalServiceProvider" /> method.
    ///     This is not recommend other than for some advanced scenarios.
    /// </remarks>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>
    ///     The same service collection so that multiple calls can be chained.
    /// </returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddEntityFrameworkLibSql(this IServiceCollection serviceCollection)
    {
        var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
            .TryAdd<LoggingDefinitions, LibSqlLoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<LibSqlOptionsExtension>>()
            .TryAdd<IRelationalTypeMappingSource, LibSqlTypeMappingSource>()
            .TryAdd<ISqlGenerationHelper, LibSqlSqlGenerationHelper>()
            .TryAdd<IRelationalAnnotationProvider, LibSqlAnnotationProvider>()
            .TryAdd<IModelValidator, LibSqlModelValidator>()
            .TryAdd<IProviderConventionSetBuilder, LibSqlConventionSetBuilder>()
            .TryAdd<IModificationCommandBatchFactory, LibSqlModificationCommandBatchFactory>()
            .TryAdd<IModificationCommandFactory, LibSqlModificationCommandFactory>()
            .TryAdd<IRelationalConnection>(p => p.GetRequiredService<ILibSqlRelationalConnection>())
            .TryAdd<IMigrationsSqlGenerator, LibSqlMigrationsSqlGenerator>()
            .TryAdd<IRelationalDatabaseCreator, LibSqlDatabaseCreator>()
            .TryAdd<IHistoryRepository, LibSqlHistoryRepository>()
            .TryAdd<IRelationalQueryStringFactory, LibSqlQueryStringFactory>()
            .TryAdd<IQueryCompilationContextFactory, LibSqlQueryCompilationContextFactory>()
            .TryAdd<IMethodCallTranslatorProvider, LibSqlMethodCallTranslatorProvider>()
            .TryAdd<IAggregateMethodCallTranslatorProvider, LibSqlAggregateMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, LibSqlMemberTranslatorProvider>()
            .TryAdd<IQuerySqlGeneratorFactory, LibSqlQuerySqlGeneratorFactory>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, LibSqlQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, LibSqlSqlTranslatingExpressionVisitorFactory>()
            .TryAdd<IQueryTranslationPostprocessorFactory, LibSqlQueryTranslationPostprocessorFactory>()
            .TryAdd<IUpdateSqlGenerator, LibSqlUpdateSqlGenerator>()
            .TryAdd<ISqlExpressionFactory, LibSqlSqlExpressionFactory>()
            .TryAdd<IRelationalParameterBasedSqlProcessorFactory, LibSqlParameterBasedSqlProcessorFactory>()
            .TryAddProviderSpecificServices(b => b.TryAddScoped<ILibSqlRelationalConnection, LibSqlRelationalConnection>())
            .TryAddCoreServices();

        return serviceCollection;
    }
}
