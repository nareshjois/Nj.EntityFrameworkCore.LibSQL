# ADR 0001: Soft-fork Nelknet via git submodule

## Status

**Superseded** — 2026-07-17 by [ADR-0002](0002-nj-libsql-data.md) Phase 3 cutover.

The soft-fork submodule `external/Nelknet.LibSQL` has been removed. The EF
provider now ProjectReferences `Nj.LibSql.Data`.

## Context

Stock `Nelknet.LibSQL.Data` historically blocked EF `SaveChanges` with
database-generated keys (`INSERT…RETURNING`): the driver could return an id while
the write did not reliably commit (`C-002`). Separately, remote HTTP connections
did not round-trip Hrana stream batons and mis-reported pipeline-level SQL errors,
so EF remote `SaveChanges` / `EnsureCreated` failed. Upstream [0.2.11](https://github.com/nelknet/Nelknet.LibSQL/releases/tag/v0.2.11)
absorbed the RETURNING drain and HTTP stream fixes; the soft-fork rebased onto
that tag and kept EF-compliance patches that were not yet upstream.

## Decision (historical)

1. Soft-fork lived at [nareshjois/Nelknet.LibSQL](https://github.com/nareshjois/Nelknet.LibSQL).
2. Consumed as a **git submodule** at `external/Nelknet.LibSQL`.
3. EF provider used a **ProjectReference** to soft-fork Data instead of NuGet.
4. Prefer upstreaming patches; rebase periodically.

## Consequences (historical)

- Soft-fork was the EF default through Preview phases 0–2 of ADR-0002.
- At Phase 3, this ADR is superseded; see ADR-0002 for the current driver.
