# Capabilities

Mode-aware capability model for Preview 1 (local + remote) and Preview 2+
(embedded replica). Aligns with [connection-modes.md](connection-modes.md) and
[limitations.md](limitations.md).

| Capability | Local file | Remote `sqld` / Turso | Embedded replica |
|------------|------------|------------------------|------------------|
| Connect via connection string | Yes | Yes | Deferred (P2+) |
| Connect via existing `LibSQLConnection` | Yes | Yes | Deferred |
| Model building / metadata | Yes | Yes | Deferred |
| `SELECT` / LINQ queries | Yes | Yes | Deferred |
| CRUD / save changes | Yes | Yes | Deferred |
| Transactions (ADO commit/rollback) | Yes | Yes | Deferred |
| Migrations `Up`/`Down` SQL | Yes | Yes (schema inside existing DB) | Deferred |
| `EnsureCreated` / `Database.Migrate` | Yes (may create file) | Schema only | Same as remote |
| `EnsureDeleted` | Yes (may delete file) | **No** — `NotSupportedException` | **No** |
| SpatiaLite / loadable extensions | **No** | **No** | **No** |
| Turso admin (create DB/token) | N/A | **Out of scope** | **Out of scope** |
| DatabaseFacade sync API | N/A | N/A | Deferred (delegates to Nelknet) |

## SQLite vs libSQL notes

Do not assume identical compatibility. Document intentional differences in
[compatibility.md](compatibility.md) when differential tests fail. Known
driver notes live in [driver-contract.md](driver-contract.md).

Server-version gated EF SQLite features (e.g. `RETURNING`, certain JSON
functions) are gated via `LibSqlDatabaseCapabilities` — not via
`Microsoft.Data.Sqlite.SqliteConnection.ServerVersion`.
