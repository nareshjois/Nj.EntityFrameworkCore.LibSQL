// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Nj.EntityFrameworkCore.LibSql.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Extension methods for <see cref="IProperty" /> for SQLite metadata.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see>, and
///     <see href="https://aka.ms/efcore-docs-sqlite">Accessing SQLite databases with EF Core</see> for more information and examples.
/// </remarks>
public static class LibSqlPropertyExtensions
{
    /// <summary>
    ///     Returns the <see cref="LibSqlValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The strategy to use for the property.</returns>
    public static LibSqlValueGenerationStrategy GetValueGenerationStrategy(this IReadOnlyProperty property)
        => property[LibSqlAnnotationNames.ValueGenerationStrategy] is LibSqlValueGenerationStrategy strategy
            ? strategy
            : property.GetDefaultValueGenerationStrategy();

    /// <summary>
    ///     Returns the <see cref="LibSqlValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <remarks>
    ///     If no strategy is set for the property, then the strategy to use will be taken from the <see cref="IModel" />.
    /// </remarks>
    /// <param name="overrides">The property overrides.</param>
    /// <returns>The strategy, or <see cref="LibSqlValueGenerationStrategy.None" /> if none was set.</returns>
    public static LibSqlValueGenerationStrategy? GetValueGenerationStrategy(
        this IReadOnlyRelationalPropertyOverrides overrides)
        => (LibSqlValueGenerationStrategy?)overrides.FindAnnotation(LibSqlAnnotationNames.ValueGenerationStrategy)
            ?.Value;

    /// <summary>
    ///     Returns the <see cref="LibSqlValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="storeObject">The identifier of the store object.</param>
    /// <returns>The strategy to use for the property.</returns>
    public static LibSqlValueGenerationStrategy GetValueGenerationStrategy(
        this IReadOnlyProperty property,
        in StoreObjectIdentifier storeObject)
        => GetValueGenerationStrategy(property, storeObject, null);

    /// <summary>
    ///     Returns the default <see cref="LibSqlValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The default strategy for the property.</returns>
    public static LibSqlValueGenerationStrategy GetDefaultValueGenerationStrategy(this IReadOnlyProperty property)
        => GetDefaultValueGenerationStrategyInternal(property, property.FindRelationalTypeMapping());

    internal static LibSqlValueGenerationStrategy GetValueGenerationStrategy(
        this IReadOnlyProperty property,
        in StoreObjectIdentifier storeObject,
        ITypeMappingSource? typeMappingSource)
    {
        var @override = property.FindOverrides(storeObject)?.FindAnnotation(LibSqlAnnotationNames.ValueGenerationStrategy);
        if (@override != null)
        {
            return (LibSqlValueGenerationStrategy?)@override.Value ?? LibSqlValueGenerationStrategy.None;
        }

        var annotation = property.FindAnnotation(LibSqlAnnotationNames.ValueGenerationStrategy);
        if (annotation?.Value != null
            && StoreObjectIdentifier.Create(property.DeclaringType, storeObject.StoreObjectType) == storeObject)
        {
            return (LibSqlValueGenerationStrategy)annotation.Value;
        }

        var table = storeObject;
        var sharedProperty = property.FindSharedStoreObjectRootProperty(storeObject);
        return sharedProperty != null
            ? sharedProperty.GetValueGenerationStrategy(storeObject, typeMappingSource) == LibSqlValueGenerationStrategy.Autoincrement
            && storeObject.StoreObjectType == StoreObjectType.Table
                && !property.GetContainingForeignKeys().Any(fk =>
                    !fk.IsBaseLinking()
                    || (StoreObjectIdentifier.Create(fk.PrincipalEntityType, StoreObjectType.Table)
                            is { } principal
                        && fk.GetConstraintName(table, principal) != null))
                    ? LibSqlValueGenerationStrategy.Autoincrement
                    : LibSqlValueGenerationStrategy.None
            : GetDefaultValueGenerationStrategy(property, storeObject, typeMappingSource);
    }

    private static LibSqlValueGenerationStrategy GetDefaultValueGenerationStrategy(
        IReadOnlyProperty property,
        in StoreObjectIdentifier storeObject,
        ITypeMappingSource? typeMappingSource)
    {
        if (storeObject.StoreObjectType != StoreObjectType.Table
            || property.IsForeignKey()
            || property.ValueGenerated == ValueGenerated.Never
            || property.DeclaringType.GetMappingStrategy() == RelationalAnnotationNames.TpcMappingStrategy)
        {
            return LibSqlValueGenerationStrategy.None;
        }

        return GetDefaultValueGenerationStrategyInternal(property, property.FindRelationalTypeMapping(storeObject));
    }

    private static LibSqlValueGenerationStrategy GetDefaultValueGenerationStrategyInternal(
        IReadOnlyProperty property,
        RelationalTypeMapping? typeMapping)
    {
        if (property.TryGetDefaultValue(out _)
            || property.GetDefaultValueSql() != null
            || property.GetComputedColumnSql() != null
            || property.IsForeignKey()
            || property.ValueGenerated == ValueGenerated.Never
            || property.DeclaringType.GetMappingStrategy() == RelationalAnnotationNames.TpcMappingStrategy)
        {
            return LibSqlValueGenerationStrategy.None;
        }

        var primaryKey = property.DeclaringType.ContainingEntityType.FindPrimaryKey();
        if (primaryKey is not { Properties.Count: 1 }
            || primaryKey.Properties[0] != property
            || !property.ClrType.UnwrapNullableType().IsInteger()
            || (typeMapping?.Converter?.ProviderClrType
                ?? typeMapping?.ClrType)?.IsInteger() != true)
        {
            return LibSqlValueGenerationStrategy.None;
        }

        return LibSqlValueGenerationStrategy.Autoincrement;
    }

    /// <summary>
    ///     Sets the <see cref="LibSqlValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The strategy to use.</param>
    public static void SetValueGenerationStrategy(
        this IMutableProperty property,
        LibSqlValueGenerationStrategy? value)
        => property.SetOrRemoveAnnotation(LibSqlAnnotationNames.ValueGenerationStrategy, value);

    /// <summary>
    ///     Sets the <see cref="LibSqlValueGenerationStrategy" /> to use for the property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The strategy to use.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>The configured value.</returns>
    public static LibSqlValueGenerationStrategy? SetValueGenerationStrategy(
        this IConventionProperty property,
        LibSqlValueGenerationStrategy? value,
        bool fromDataAnnotation = false)
        => (LibSqlValueGenerationStrategy?)property.SetOrRemoveAnnotation(
            LibSqlAnnotationNames.ValueGenerationStrategy, value, fromDataAnnotation)?.Value;

    /// <summary>
    ///     Sets the <see cref="LibSqlValueGenerationStrategy" /> to use for the property for a particular table.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The strategy to use.</param>
    /// <param name="storeObject">The identifier of the table containing the column.</param>
    public static void SetValueGenerationStrategy(
        this IMutableProperty property,
        LibSqlValueGenerationStrategy? value,
        in StoreObjectIdentifier storeObject)
        => property.GetOrCreateOverrides(storeObject)
            .SetValueGenerationStrategy(value);

    /// <summary>
    ///     Sets the <see cref="LibSqlValueGenerationStrategy" /> to use for the property for a particular table.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The strategy to use.</param>
    /// <param name="storeObject">The identifier of the table containing the column.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>The configured value.</returns>
    public static LibSqlValueGenerationStrategy? SetValueGenerationStrategy(
        this IConventionProperty property,
        LibSqlValueGenerationStrategy? value,
        in StoreObjectIdentifier storeObject,
        bool fromDataAnnotation = false)
        => property.GetOrCreateOverrides(storeObject, fromDataAnnotation)
            .SetValueGenerationStrategy(value, fromDataAnnotation);

    /// <summary>
    ///     Sets the <see cref="LibSqlValueGenerationStrategy" /> to use for the property for a particular table.
    /// </summary>
    /// <param name="overrides">The property overrides.</param>
    /// <param name="value">The strategy to use.</param>
    public static void SetValueGenerationStrategy(
        this IMutableRelationalPropertyOverrides overrides,
        LibSqlValueGenerationStrategy? value)
        => overrides.SetOrRemoveAnnotation(LibSqlAnnotationNames.ValueGenerationStrategy, value);

    /// <summary>
    ///     Sets the <see cref="LibSqlValueGenerationStrategy" /> to use for the property for a particular table.
    /// </summary>
    /// <param name="overrides">The property overrides.</param>
    /// <param name="value">The strategy to use.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>The configured value.</returns>
    public static LibSqlValueGenerationStrategy? SetValueGenerationStrategy(
        this IConventionRelationalPropertyOverrides overrides,
        LibSqlValueGenerationStrategy? value,
        bool fromDataAnnotation = false)
        => (LibSqlValueGenerationStrategy?)overrides.SetOrRemoveAnnotation(
            LibSqlAnnotationNames.ValueGenerationStrategy, value, fromDataAnnotation)?.Value;

    /// <summary>
    ///     Gets the <see cref="ConfigurationSource" /> for the value generation strategy.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for the value generation strategy.</returns>
    public static ConfigurationSource? GetValueGenerationStrategyConfigurationSource(this IConventionProperty property)
        => property.FindAnnotation(LibSqlAnnotationNames.ValueGenerationStrategy)?.GetConfigurationSource();

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for the <see cref="LibSqlValueGenerationStrategy" /> for a particular table.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="storeObject">The identifier of the table containing the column.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for the <see cref="LibSqlValueGenerationStrategy" />.</returns>
    public static ConfigurationSource? GetValueGenerationStrategyConfigurationSource(
        this IConventionProperty property,
        in StoreObjectIdentifier storeObject)
        => property.FindOverrides(storeObject)?.GetValueGenerationStrategyConfigurationSource();

    /// <summary>
    ///     Returns the <see cref="ConfigurationSource" /> for the <see cref="LibSqlValueGenerationStrategy" /> for a particular table.
    /// </summary>
    /// <param name="overrides">The property overrides.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for the <see cref="LibSqlValueGenerationStrategy" />.</returns>
    public static ConfigurationSource? GetValueGenerationStrategyConfigurationSource(
        this IConventionRelationalPropertyOverrides overrides)
        => overrides.FindAnnotation(LibSqlAnnotationNames.ValueGenerationStrategy)?.GetConfigurationSource();

    /// <summary>
    ///     Returns the SRID to use when creating a column for this property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The SRID to use when creating a column for this property.</returns>
    public static int? GetSrid(this IReadOnlyProperty property)
        => (int?)property[LibSqlAnnotationNames.Srid];

    /// <summary>
    ///     Returns the SRID to use when creating a column for this property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="storeObject">The identifier of the store object.</param>
    /// <returns>The SRID to use when creating a column for this property.</returns>
    public static int? GetSrid(
        this IReadOnlyProperty property,
        in StoreObjectIdentifier storeObject)
    {
        var annotation = property.FindAnnotation(LibSqlAnnotationNames.Srid);
        if (annotation != null)
        {
            return (int?)annotation.Value;
        }

        return property.FindSharedStoreObjectRootProperty(storeObject)?.GetSrid(storeObject);
    }

    /// <summary>
    ///     Sets the SRID to use when creating a column for this property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The SRID.</param>
    public static void SetSrid(this IMutableProperty property, int? value)
        => property.SetOrRemoveAnnotation(LibSqlAnnotationNames.Srid, value);

    /// <summary>
    ///     Sets the SRID to use when creating a column for this property.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The SRID.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    public static int? SetSrid(this IConventionProperty property, int? value, bool fromDataAnnotation = false)
        => (int?)property.SetOrRemoveAnnotation(LibSqlAnnotationNames.Srid, value, fromDataAnnotation)?.Value;

    /// <summary>
    ///     Gets the <see cref="ConfigurationSource" /> for the column SRID.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The <see cref="ConfigurationSource" /> for the column SRID.</returns>
    public static ConfigurationSource? GetSridConfigurationSource(this IConventionProperty property)
        => property.FindAnnotation(LibSqlAnnotationNames.Srid)?.GetConfigurationSource();
}
