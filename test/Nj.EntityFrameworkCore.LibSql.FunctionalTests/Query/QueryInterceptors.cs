using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

internal sealed class RecordingCommandInterceptor : DbCommandInterceptor
{
    public List<string> CommandTexts { get; } = [];

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        CommandTexts.Add(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        CommandTexts.Add(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}

internal sealed class RecordingQueryExpressionInterceptor : IQueryExpressionInterceptor
{
    public int QueryCompilationCount { get; private set; }

    public Expression QueryCompilationStarting(
        Expression queryExpression,
        QueryExpressionEventData eventData)
    {
        QueryCompilationCount++;
        return queryExpression;
    }
}
