# Observability (WP-13)

`Nj.LibSql.Data` emits a thin `System.Diagnostics.ActivitySource` named
**`Nj.LibSql.Data`** (`LibSqlActivitySource.Name`).

## Spans

| Operation | When |
|-----------|------|
| `ExecuteNonQuery` / `ExecuteReader` | Remote HTTP/WebSocket command execute |
| `libsql.sync` | Embedded-replica `Sync` / `SyncAsync` |

Tags include `db.system=libsql`, connection mode, and a truncated `db.statement`.
**Auth tokens and connection strings are never tagged.**

There is **no** automatic retry wrapping these spans. Transport failures after a
write may leave an ambiguous commit — see [connection-modes.md](connection-modes.md).

## Subscribing

```csharp
using var listener = new ActivityListener
{
    ShouldListenTo = s => s.Name == LibSqlActivitySource.Name,
    Sample = (ref ActivityCreationOptions<ActivityContext> _)
        => ActivitySamplingResult.AllDataAndRecorded,
    ActivityStarted = a => Console.WriteLine(a.OperationName),
};
ActivitySource.AddActivityListener(listener);
```

Or wire OpenTelemetry to the same source name. EF Core DiagnosticSource /
`ILogger` events remain the primary SQL logging surface; this ActivitySource is
ADO-level only (not a full EF→OTel bridge).
