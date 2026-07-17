namespace Nj.LibSql.Data.Http;

/// <summary>Shared Hrana session used by HTTP and WebSocket transports.</summary>
internal interface ILibSqlHranaSession : IDisposable
{
    Task<HranaBatchResponse> ExecuteBatchAsync(
        HranaBatchRequest batch,
        CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
