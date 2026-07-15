# Provider development

Guidance for maintainers implementing and reviewing this provider.

## External guidance

Follow Microsoft’s current
[Writing an EF Core database provider](https://learn.microsoft.com/ef/core/providers/writing-a-provider)
documentation. Prefer **published** EF Core and specification-test packages over
forking the full `dotnet/efcore` repository.

## Architecture sketch

```text
Application
  → EF Core 10
    → Nj.EntityFrameworkCore.LibSql
      → Nelknet.LibSQL.Data (ADO.NET)
        → Local file | Remote sqld/Turso | Embedded replica
```

The default rule is a **connection-semantics substitution**, not a new SQL
dialect: preserve EF SQLite translation/SQL unless a test proves libSQL/Nelknet
requires a change. See the plan’s functional delta contract for the allowed
touch list (relational connection, database creator, model factory, unsupported
native features, exception mapping, connection-mode options).

## Implementation sequence

| Phase | Focus |
|-------|--------|
| WP-01 | Repository / build scaffold (complete) |
| WP-02 | Nelknet ADO.NET contract tests (no EF) (complete) |
| WP-03 | Import EF Core 10 SQLite baseline; mechanical rename; customization map (complete) |
| WP-04 | `UseLibSql` / DI / options / relational connection (complete — G4) |
| WP-05 | Type mapping / SQL generation (complete — G5) |
| WP-06+ | Queries, updates, transactions, migrations, design-time, compliance |

## Source-commit rules

1. Preserve Microsoft MIT copyright headers on imported files.
2. Keep **upstream import**, **mechanical rename**, and **functional LibSQL
   changes** in separate commits.
3. Update [NOTICE](../NOTICE) when the baseline import lands (tag, commit, paths,
   date).
4. Do not reference `Microsoft.EntityFrameworkCore.Sqlite`.
5. Pin `Nelknet.LibSQL.Data` exactly while it remains pre-1.0; bump only via
   tested dependency PRs.

## Documents produced in WP-03

- [`upstream-baseline.md`](upstream-baseline.md) — EF tag/commit and imported paths
- [`provider-service-map.md`](provider-service-map.md) — subsystem customization inventory
- [`capabilities.md`](capabilities.md) — local / remote / replica capability model

Compare the renamed tree to upstream with `./eng/compare-upstream-sqlite.sh`.

## Review checklist for LibSQL deltas

A PR that changes preserved SQLite subsystems (conventions, type mapping, SQL
generation, migrations algorithms, etc.) must include:

1. A minimal failing test vs unmodified EF SQLite behavior where applicable
2. Evidence the difference comes from libSQL/Nelknet connection semantics
3. A compatibility impact note in [compatibility.md](compatibility.md)
4. Maintainer approval for widening the provider delta
