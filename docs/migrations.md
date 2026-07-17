# Migrations

EF Core migrations runtime (`Database.Migrate`, history table, locking) and
design-time / reverse-engineering are supported for Preview 1 local + remote.
Walkthrough: [samples/MigrationsSample/README.md](../samples/MigrationsSample/README.md).
Smoke: `./eng/verify-migrations-sample.sh`.

## Status

| Surface | Status |
|---------|--------|
| `EnsureCreated` (schema in addressable DB) | Working (local + remote) |
| `EnsureDeleted` local | Working (`Exists()` false; file delete or wipe + tombstone on Windows `C-005`) |
| `EnsureDeleted` remote / replica | Throws `NotSupportedException` |
| `Database.Migrate` up / down | Working |
| Migration lock acquire/release | Working (split commands for reliable `ExecuteScalar`) |
| Non-idempotent `dotnet ef migrations script` | Working |
| Idempotent SQL script (`--idempotent`) | Not supported (SQLite/libSQL; throws) |
| Design-time DI + `IDatabaseModelFactory` | Working |
| `UseLibSql` scaffolding codegen | Working |
| `dotnet ef` add / update / scaffold | Working |

## Create / delete policy

| Mode | `EnsureCreated` | `EnsureDeleted` |
|------|-----------------|-----------------|
| Local file | May create file + schema | May delete file |
| Remote (`sqld` / Turso) | Schema inside existing DB only | Always throws |
| Embedded replica | Schema inside replica only | Always throws |

## History schema

Compatible with EF SQLite (`__EFMigrationsHistory`, `__EFMigrationsLock`) unless
a documented delta appears in [compatibility.md](compatibility.md).

## Design-time

Design-time services ship in `Nj.EntityFrameworkCore.LibSql` (no separate
`.Design` package). Install `dotnet-ef` matching [versions.md](versions.md).

On remote/sqld, CLR type sampling may warn and skip (`C-003`). Virtual tables and
vector types are not reverse-engineered (`C-004`).
