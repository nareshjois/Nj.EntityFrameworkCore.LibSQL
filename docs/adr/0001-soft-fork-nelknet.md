# ADR 0001: Soft-fork Nelknet via git submodule

## Status

Accepted — 2026-07-15

## Context

Stock `Nelknet.LibSQL.Data` 0.2.10 blocks EF `SaveChanges` with database-generated
keys (`INSERT…RETURNING`): the driver can return an id while the write does not
reliably commit (`C-002`). Waiting solely on upstream stalls Preview.

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

## C-002 fix (fork branch `fix/returning-statement-lifetime`)

Stock Nelknet closed `LibSQLDataReader` without stepping remaining rows to
`SQLITE_DONE`, and disposed parameterized statements before the reader finished.
EF `SaveChanges` reads one `INSERT…RETURNING` row and closes the reader, leaving
the write in a rollback journal (`cannot commit` / phantom generated keys).

Fork changes in `LibSQLDataReader` / `LibSQLCommand`:

1. Drain remaining rows in `Close()` before releasing handles.
2. Transfer prepared-statement ownership to the reader for parameterized queries.
3. Keep scalar-statement lifetime until after rows are drained; avoid double-free
   of row/rows handles in `ExecuteScalar`.

## Consequences

- Clones and CI must initialize submodules.
- Driver fixes land in the submodule (push to `nareshjois/Nelknet.LibSQL`), then
  the parent repo advances the submodule pointer.
- Preview does not require a private NuGet feed for the fork.
- Native extension APIs are **not** in scope; limitations.md unchanged for that
  list.
