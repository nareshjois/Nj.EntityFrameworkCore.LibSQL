# ADR 0001: Soft-fork Nelknet via git submodule

## Status

Accepted — 2026-07-15

## Context

Stock `Nelknet.LibSQL.Data` historically blocked EF `SaveChanges` with
database-generated keys (`INSERT…RETURNING`): the driver could return an id while
the write did not reliably commit (`C-002`). Separately, remote HTTP connections
did not round-trip Hrana stream batons and mis-reported pipeline-level SQL errors,
so EF remote `SaveChanges` / `EnsureCreated` failed. Upstream [0.2.11](https://github.com/nelknet/Nelknet.LibSQL/releases/tag/v0.2.11)
absorbed the RETURNING drain and HTTP stream fixes; the soft-fork rebases onto
that tag and keeps EF-compliance patches that are not yet upstream.

Extensions / `CreateFunction` / SpatiaLite remain **out of scope** for this
initiative (`C-001` stays fail-fast translation).

## Decision

1. Soft-fork lives at [nareshjois/Nelknet.LibSQL](https://github.com/nareshjois/Nelknet.LibSQL)
   (fork of [nelknet/Nelknet.LibSQL](https://github.com/nelknet/Nelknet.LibSQL)).
2. Consume it as a **git submodule** at `external/Nelknet.LibSQL`.
3. The EF provider and driver-contract tests use a **ProjectReference** to
   `external/Nelknet.LibSQL/src/Nelknet.LibSQL.Data/Nelknet.LibSQL.Data.csproj`
   instead of the NuGet package pin.
4. Keep public types (`LibSQLConnection`, etc.) so `UseLibSql` stays stable.
5. Prefer upstreaming patches to Nelknet; rebase the submodule periodically.
   Soft-fork tip consumed here is currently `@8b5a289` (branch `soft/main-0.2.11`
   on top of upstream `v0.2.11`).

## Patch set on soft-fork tip

### Upstream 0.2.11 (now base)

1. Local `INSERT`/`UPDATE`/`DELETE`/`REPLACE` with `RETURNING`: keep statement
   handles alive and drain/complete readers so commits persist (`C-002`).
2. Preserve Hrana HTTP stream state, honor server pipeline URLs, surface
   top-level protocol errors so remote transactions commit/roll back correctly.

### Soft-fork-only (rebased onto 0.2.11)

#### Parameters — unprefixed names

Normalize names lacking `@`/`:`/`$`/`?` by prefixing `@` so EF
`FromSqlInterpolated` (`p0` in the collection, `@p0` in SQL) binds like
Microsoft.Data.Sqlite.

#### C-005 — Close + EnsureDeleted on Windows

1. Track `LibSQLCommand` instances on the connection; dispose them (finalize
   prepared statements) before `libsql_disconnect` / `libsql_close`.
2. Expose `LibSQLConnection.ClearPool` / `ClearAllPools` so
   `LibSqlDatabaseCreator.Delete` can release the connection before
   `File.Delete`.
3. When Windows still cannot delete/rename the file, wipe user schema and
   tombstone the path so `Exists()` is false until `Create()` (file may linger).

#### EF compliance (C-011 and related)

1. Prefetch first `libsql_next_row` in `ExecuteReader` so UNIQUE/FK failures
   surface even when EF never calls `Read()` (NoResults inserts).
2. Map constraint error text to `LibSQLConstraintException`.
3. Split SQL batches for `ExecuteNonQuery` (EF `HasData` + `SELECT changes()`),
   without breaking `CREATE TRIGGER … BEGIN … END` bodies.
4. Empty BLOB / temporal / `DbParameter.Size` parity fixes for EF type tests.

## Consequences

- Clones and CI must initialize submodules.
- Driver fixes land in the submodule (push to `nareshjois/Nelknet.LibSQL`), then
  the parent repo advances the submodule pointer.
- Preview does not require a private NuGet feed for the fork.
- Native extension APIs are **not** in scope; limitations.md unchanged for that
  list.
- Open a separate upstream Nelknet PR(s) so stock NuGet can eventually replace the
  soft-fork pin.
