# Transactions and consistency

## Transactions

- Standard EF / ADO.NET transactions and SQL savepoints are supported
  (local and remote).
- Distributed transactions are **not** supported.
- Nested / savepoint details follow libSQL and
  [connection-modes.md](connection-modes.md).

## No automatic write retries

The provider **never** auto-retries writes. A transport failure during
`Commit` / `SaveChanges` may leave an **ambiguous** outcome — the write may or
may not have applied on the server. Callers must reconcile (read-your-writes,
idempotent keys, or application recovery). Fault injection:
`Kill_sqld_mid_commit_is_ambiguous_without_auto_retry` in ConnectionModes tests.

## Cancellation

| Surface | Behavior |
|---------|----------|
| Remote `Execute*Async` | `CancellationToken` honored (HTTP/WS linked CTS); `Cancel()` is best-effort |
| Local execute | Pre/post cancel checks only — mid-flight native work is **not** aborted |
| `Sync` / `SyncAsync` | Token cancels waiting; `libsql_sync2` is not interruptible mid-call |

## Embedded replica Sync

- `LibSqlConnection.Sync` / `Database.Sync` run native `libsql_sync2`.
- Consistency matches libSQL (`Read Your Writes`, `Sync Interval` in seconds).
- Validated on self-hosted **sqld**. Turso Cloud Sync hang — **C-019**.

## Related

- [connection-modes.md](connection-modes.md)
- [observability.md](observability.md) — ActivitySource; no retry wrapping
- [limitations.md](limitations.md)
