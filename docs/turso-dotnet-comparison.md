# Nj vs Turso .NET comparison

Living capability matrix for `Nj.EntityFrameworkCore.LibSql` / `Nj.LibSql.Data`
versus Turso’s official [bindings/dotnet](https://github.com/tursodatabase/turso/tree/main/bindings/dotnet)
(`Turso.Data` / `turso_sdk_kit`).

**Nj stance:** differentiate on **remote EF** + embedded replica Sync (sqld) + deep EF
compliance; close high-value ADO gaps (`DbBatch`, keywords). Desktop RID set stays
the three smoked RIDs (`linux-x64`, `osx-arm64`, `win-x64`) for now. Opt-in Turso
engine for local EF is planned via
`UseLibSql(cs, o => o.NativeEngine(LibSqlNativeEngine.Turso))` — not a full cutover.

## Engines

| | Nj | Turso official |
|--|--|--|
| Engine | Classic libSQL C API (`libsql-server-v0.24.32` pin) | Turso (`turso_sdk_kit`) |
| ADO | `Nj.LibSql.Data` | `Turso.Data` (+ `Turso.Data.Sqlite` facade) |
| EF | Full EF Core 10 `UseLibSql` | Thin `UseTurso` (local only) |
| NuGet | `10.0.0-preview.1` | `Turso.Data.Sqlite` prereleases |

## Connection modes

| Capability | Nj | Turso |
|------------|----|-------|
| Local / `:memory:` | Yes | Yes |
| Remote HTTPS / `libsql://` | Yes (managed Hrana) | Yes (Hrana `/v2/pipeline`) |
| Real WebSocket Hrana | Yes vs self-hosted `sqld` | Remaps `ws`/`wss` → HTTP |
| Embedded replica Sync | Yes vs **sqld**; Turso Cloud **C-019** | Reserved / fail-early until sync kit |
| Remote EF | **Yes** | **No** (documented) |

## ADO parity notes (Nj)

| Item | Status |
|------|--------|
| Public `DbBatch` / `CreateBatch` | Yes — remote one round-trip; local `ExecuteNonQuery` sequential; local multi-command `ExecuteReader` fail-clear |
| `Tls` | Yes — `libsql://` → HTTPS when true (default), HTTP when false |
| `Filename` | Alias of `Data Source` |
| MDS-compat keywords (`Mode`, `Cache`, `Foreign Keys`, …) | Accepted and ignored |
| `$name` / `@name` / `:name` | Interchangeable prefixes |
| `IsAotCompatible` + PublishAot smoke | Yes — `./eng/smoke-aot.sh` / `samples/AotLocalSample` (dynamic natives) |
| Extra desktop RIDs (`win-arm64`, …) | **Deferred** — not advertised |

## Bake-off checklist (manual)

Shared scenarios against one Turso DB / local sqld. Not a hard CI gate against
Turso’s package — record pass/fail here when run.

| # | Scenario | Nj | Turso.Data |
|---|----------|----|------------|
| 1 | Local open + SELECT 1 | Yes | Yes |
| 2 | Remote open + parameterized SELECT | Yes | Yes |
| 3 | Txn + SAVEPOINT | Yes | Yes |
| 4 | EF local EnsureCreated + SaveChanges | Yes (`UseLibSql`) | Yes (`UseTurso`) |
| 5 | EF remote query | Yes | N/A (local EF only) |
| 6 | Replica Sync | Yes vs sqld; Turso Cloud C-019 | Fail-early until sync kit |
| 7 | Remote `DbBatch` | Yes (`CreateBatch`) | Yes |
| 8 | EF local + `NativeEngine(Turso)` | Planned | N/A |
| 9 | PublishAot local ADO SELECT 1 | Yes (`smoke-aot.sh`) | NativeAot packages exist |

## Keyword parity (Nj `LibSqlConnectionStringBuilder`)

| Keyword | Nj | Notes vs Turso |
|---------|-----|----------------|
| `Data Source` (+ `Filename`, `Uri`, …) | Yes | Turso also uses `Filename` |
| `Auth Token` | Yes | Same |
| `Tls` | Yes | Maps `libsql://` scheme |
| `Sync URL` / `Sync Auth Token` / `Sync Interval` / `Offline` | Yes (replica) | Turso uses reserved `Replica Path` |
| `Read Your Writes` | Yes | Dual meaning (remote baton vs replica) — see [connection-modes.md](connection-modes.md) |
| `Mode` / `Cache` / `Foreign Keys` / `Pooling` / … | Ignored | Soft MDS migration |

## Non-goals

MDS Sqlite facade package, mobile RIDs, static NativeAOT RID product line,
UDFs / backup / SpatiaLite, replacing remote Hrana with Turso-only remote EF.

## Related

- [connection-strings.md](connection-strings.md)
- [compatibility.md](compatibility.md) (C-019)
- [deployment.md](deployment.md)
