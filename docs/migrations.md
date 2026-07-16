# Migrations

This provider supports the EF Core migrations **runtime** workflow
(`Database.Migrate`, history table, migration locking) as validated by the
WP-08 FunctionalTests matrix, and the design-time / reverse-engineering
workflow validated by WP-09 (factory matrix, goldens, and
[`samples/MigrationsSample`](../samples/MigrationsSample)).

## Preview status

| Surface | Status |
|---------|--------|
| `EnsureCreated` (schema in addressable DB) | Working (local + remote) |
| `EnsureDeleted` local | Working (`Exists()` false; deletes file when unlocked, else wipe + tombstone on Windows `C-005`) |
| `EnsureDeleted` remote / replica | Throws `NotSupportedException` |
| `Database.Migrate` up / down | Working (FunctionalTests matrix) |
| Migration lock table acquire/release | Working (Nelknet-safe split commands) |
| Non-idempotent `dotnet ef migrations script` | Working |
| Idempotent SQL script generation (`--idempotent`) | Not supported (SQLite/libSQL limitation; throws) |
| Design-time DI + `IDatabaseModelFactory` | Working (WP-09) |
| `UseLibSql` scaffolding codegen | Working (`LibSqlCodeGenerator`) |
| `dotnet ef` migrations add / database update / scaffold | Working (`MigrationsSample` + `eng/verify-migrations-sample.sh`) |

See [wp-08-handoff.md](wp-08-handoff.md) and [wp-09-handoff.md](wp-09-handoff.md).

## Locked create / delete policy

These rules will not change without a documented compatibility impact:

| Mode | `EnsureCreated` | `EnsureDeleted` |
|------|-----------------|-----------------|
| Local file | May create the database file and apply model schema (EF SQLite–like) | May delete the database file |
| Remote (`sqld` / Turso) | Creates schema **inside** an already-addressable database only — never creates cloud databases or namespaces | Always throws `NotSupportedException` with a clear message |
| Embedded replica | Schema inside the replica database only | Always throws `NotSupportedException` |

## Migration history

History schema remains compatible with the EF SQLite provider
(`__EFMigrationsHistory`, `__EFMigrationsLock`) unless a Nelknet/libSQL
incompatibility forces a documented delta. Any divergence must be recorded in
[compatibility.md](compatibility.md).

## Design-time tooling

Design-time services ship in the same NuGet package
(`Nj.EntityFrameworkCore.LibSql`) via `LibSqlDesignTimeServices` (no separate
`.Design` package). Install `dotnet-ef` matching the EF Core patch in
[versions.md](versions.md).

Walkthrough: [samples/MigrationsSample/README.md](../samples/MigrationsSample/README.md).
Automated smoke: `./eng/verify-migrations-sample.sh`.

On remote/sqld, CLR type inference from row sampling may warn and skip
(see `C-003` in [compatibility.md](compatibility.md)); column store types,
AUTOINCREMENT, and collation from CREATE SQL still scaffold. Virtual tables
and vector types are not reverse-engineered (`C-004`).
