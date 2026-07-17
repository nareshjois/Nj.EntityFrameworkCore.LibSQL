# Connection modes

Mode is selected **only** through the `Nj.LibSql.Data` connection string (or an
existing `LibSqlConnection`). There are **no** `UseLibSqlLocal` /
`UseLibSqlRemote` / `UseLibSqlReplica` helpers.

```csharp
options.UseLibSql("Data Source=app.db");
// or
options.UseLibSql(existingLibSqlConnection);
```

Property names and aliases: `Data Source` / `DataSource` / `Filename`;
`Auth Token` / `AuthToken` / `Token`; `Sync URL` / `SyncUrl`;
`Sync Auth Token` / `SyncAuthToken`; `Sync Interval` (seconds);
`Read Your Writes`; `Offline` (appends `?offline` to the sync URL).

## Local

```text
Data Source=/path/to/app.db
Data Source=:memory:
```

## Remote — self-hosted `sqld` or Turso

`http(s)://` and `libsql://` use HTTP Hrana. Explicit `ws://` / `wss://` use
WebSocket. Turso Cloud rejects WebSocket upgrades — use HTTPS / `libsql://`.

```text
Data Source=http://127.0.0.1:8080
Data Source=https://<db>-<org>.turso.io;Auth Token=<token>
Data Source=libsql://<db>-<org>.turso.io;Auth Token=<token>
```

Large SELECT result sets over HTTP are buffered in process memory (**C-018**).
Prefer WebSocket (`ws://`) against self-hosted `sqld` for very large cursors;
Turso Cloud must stay on HTTP.

Tests start a pinned `libsql-server` image via Testcontainers. Override with
`LIBSQL_TEST_URL`, or use [`eng/sqld/docker-compose.yml`](../eng/sqld/docker-compose.yml).
See [testing.md](testing.md).

Creating remote databases and tokens is **out of scope**
([limitations.md](limitations.md)).

## Embedded replica

```text
Data Source=local-replica.db;Sync URL=http://127.0.0.1:8080
Data Source=local-replica.db;Sync URL=https://<db>-<org>.turso.io;Auth Token=<token>
```

`Data Source` must be a **local file path** (not `:memory:` or a remote URL).
`Sync URL` is the primary. Auth uses `Sync Auth Token` if set, otherwise
`Auth Token`.

Driver:

```csharp
connection.Sync();
await connection.SyncAsync();
```

EF (`DatabaseFacade`):

```csharp
context.Database.Sync();
await context.Database.SyncAsync();
```

Consistency matches native libSQL (`Read Your Writes`, `Sync Interval` in
**seconds**, `libsql_sync2`). These APIs do not claim stronger guarantees.

`EnsureDeleted` throws for embedded replicas (same policy as remote).

### Primary compatibility (C-019)

Embedded-replica **Sync is validated against self-hosted `sqld`**. Against Turso
Cloud, the pinned native client (`libsql-server-v0.24.32`) opens a replica but
`Sync` currently hangs; treat Turso as an HTTP primary only until a newer
native pin is verified. See [compatibility.md](compatibility.md).

## Redaction

Auth tokens must never appear in default logs or exception messages. Use
`LibSqlConnectionStringBuilder.Redact` for diagnostics. EF `LogFragment` and
open-failure exceptions scrub tokens; SQL parameter values follow EF
`EnableSensitiveDataLogging` (off by default).

## Cancellation

| Surface | Behavior |
|---------|----------|
| Remote command `Execute*Async` | `CancellationToken` forwarded to HTTP/WebSocket; `Cancel()` cancels the in-flight linked CTS (bytes already sent may still complete server-side). |
| Local command execute | Pre/post `ThrowIfCancellationRequested` only — **mid-flight native execute is not aborted**. |
| `Sync` / `SyncAsync` | Token cancels waiting on the thread-pool wrap; native `libsql_sync2` is not interruptible mid-call. |
| Remote `OpenAsync` | Caller token + 15s connect timeout linked CTS. |

## Ambiguous commits and retries

There is **no automatic write retry**. If the transport fails during
`Commit` / `SaveChanges` (e.g. `sqld` killed mid-commit), the outcome on the
server is **ambiguous** — the write may or may not have landed. Callers must
reconcile (read-your-writes, idempotent keys, or application-level recovery).
See the ConnectionModes fault-injection test and [limitations.md](limitations.md).

Observability spans: [observability.md](observability.md).

