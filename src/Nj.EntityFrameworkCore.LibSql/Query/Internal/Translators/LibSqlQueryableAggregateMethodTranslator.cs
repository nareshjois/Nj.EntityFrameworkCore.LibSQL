using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Nj.EntityFrameworkCore.LibSql.Internal;

// ReSharper disable once CheckNamespace
namespace Nj.EntityFrameworkCore.LibSql.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class LibSqlQueryableAggregateMethodTranslator : IAggregateMethodCallTranslator
{
    private readonly ISqlExpressionFactory _sqlExpressionFactory;
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public LibSqlQueryableAggregateMethodTranslator(
        ISqlExpressionFactory sqlExpressionFactory,
        IRelationalTypeMappingSource typeMappingSource)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _typeMappingSource = typeMappingSource;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual SqlExpression? Translate(
        MethodInfo method,
        EnumerableExpression source,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType == typeof(Queryable))
        {
            var methodInfo = method.IsGenericMethod
                ? method.GetGenericMethodDefinition()
                : method;
            switch (methodInfo.Name)
            {
                case nameof(Queryable.Average)
                    when (QueryableMethods.IsAverageWithoutSelector(methodInfo)
                        || QueryableMethods.IsAverageWithSelector(methodInfo))
                    && source.Selector is SqlExpression averageSqlExpression:
                    var averageArgumentType = GetProviderType(averageSqlExpression);
                    if (averageArgumentType == typeof(decimal))
                    {
                        // Microsoft SQLite uses ef_avg; rewrite via avg(CAST AS REAL).
                        return AggregateAsRealThenDecimal(source, averageSqlExpression, "avg");
                    }

                    break;

                case nameof(Queryable.Max)
                    when (methodInfo == QueryableMethods.MaxWithoutSelector
                        || methodInfo == QueryableMethods.MaxWithSelector)
                    && source.Selector is SqlExpression maxSqlExpression:
                    var maxArgumentType = GetProviderType(maxSqlExpression);
                    if (maxArgumentType == typeof(DateTimeOffset)
                        || maxArgumentType == typeof(TimeSpan)
                        || maxArgumentType == typeof(ulong))
                    {
                        throw new NotSupportedException(
                            LibSqlStrings.AggregateOperationNotSupported(nameof(Queryable.Max), maxArgumentType.ShortDisplayName()));
                    }

                    if (maxArgumentType == typeof(decimal))
                    {
                        return AggregateAsRealThenDecimal(source, maxSqlExpression, "max");
                    }

                    break;

                case nameof(Queryable.Min)
                    when (methodInfo == QueryableMethods.MinWithoutSelector
                        || methodInfo == QueryableMethods.MinWithSelector)
                    && source.Selector is SqlExpression minSqlExpression:
                    var minArgumentType = GetProviderType(minSqlExpression);
                    if (minArgumentType == typeof(DateTimeOffset)
                        || minArgumentType == typeof(TimeSpan)
                        || minArgumentType == typeof(ulong))
                    {
                        throw new NotSupportedException(
                            LibSqlStrings.AggregateOperationNotSupported(nameof(Queryable.Min), minArgumentType.ShortDisplayName()));
                    }

                    if (minArgumentType == typeof(decimal))
                    {
                        return AggregateAsRealThenDecimal(source, minSqlExpression, "min");
                    }

                    break;

                case nameof(Queryable.Sum)
                    when (QueryableMethods.IsSumWithoutSelector(methodInfo)
                        || QueryableMethods.IsSumWithSelector(methodInfo))
                    && source.Selector is SqlExpression sumSqlExpression:
                    var sumArgumentType = GetProviderType(sumSqlExpression);
                    if (sumArgumentType == typeof(decimal))
                    {
                        return AggregateAsRealThenDecimal(source, sumSqlExpression, "sum");
                    }

                    break;
            }
        }

        return null;
    }

    private SqlExpression AggregateAsRealThenDecimal(
        EnumerableExpression source,
        SqlExpression sqlExpression,
        string aggregateFunction)
    {
        sqlExpression = CombineTerms(source, sqlExpression);
        var asReal = _sqlExpressionFactory.Convert(
            sqlExpression,
            typeof(double),
            _typeMappingSource.FindMapping(typeof(double)));
        var aggregated = _sqlExpressionFactory.Function(
            aggregateFunction,
            [asReal],
            nullable: true,
            argumentsPropagateNullability: Statics.FalseArrays[1],
            typeof(double),
            _typeMappingSource.FindMapping(typeof(double)));

        return _sqlExpressionFactory.Convert(aggregated, typeof(decimal), sqlExpression.TypeMapping);
    }

    private static Type? GetProviderType(SqlExpression expression)
        => expression.TypeMapping?.Converter?.ProviderClrType
            ?? expression.TypeMapping?.ClrType
            ?? expression.Type;

    private SqlExpression CombineTerms(EnumerableExpression enumerableExpression, SqlExpression sqlExpression)
    {
        if (enumerableExpression.Predicate != null)
        {
            sqlExpression = _sqlExpressionFactory.Case(
                new List<CaseWhenClause> { new(enumerableExpression.Predicate, sqlExpression) },
                elseResult: null);
        }

        if (enumerableExpression.IsDistinct)
        {
            sqlExpression = new DistinctExpression(sqlExpression);
        }

        return sqlExpression;
    }
}
