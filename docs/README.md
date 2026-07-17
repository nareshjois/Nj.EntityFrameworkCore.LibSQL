# Docs

Current-state documentation for `Nj.EntityFrameworkCore.LibSql` and the in-repo
ADO.NET driver (`Nj.LibSql.Data` / `Nj.LibSql.Bindings`).

## Using the provider

| Document | Purpose |
|----------|---------|
| [architecture.md](architecture.md) | Packages, naming, provider vs baseline, natives, CI |
| [connection-modes.md](connection-modes.md) | Local, remote, and embedded-replica behavior |
| [connection-strings.md](connection-strings.md) | Connection-string keys (Nj.LibSql.Data SoT) |
| [migrate-from-ef-sqlite.md](migrate-from-ef-sqlite.md) | Moving from Microsoft EF SQLite |
| [transactions.md](transactions.md) | Transactions, ambiguous commit, cancel, Sync |
| [observability.md](observability.md) | ActivitySource spans and redaction |
| [deployment.md](deployment.md) | Advertised RIDs, publish modes, troubleshooting |
| [performance.md](performance.md) | Benchmark baselines and soft thresholds |
| [limitations.md](limitations.md) | Explicit non-goals and unsupported APIs |
| [compatibility.md](compatibility.md) | Capability matrix and waiver log |
| [migrations.md](migrations.md) | Migrations and EnsureCreated/Deleted policy |

## Developing / releasing

| Document | Purpose |
|----------|---------|
| [testing.md](testing.md) | How to run tests, Testcontainers, Turso secrets |
| [versions.md](versions.md) | Compatibility matrix and pins |
| [upstream-baseline.md](upstream-baseline.md) | EF Core SQLite import provenance |
| [release-policy.md](release-policy.md) | Versioning and approval |
| [releasing.md](releasing.md) | Tag → pack → NuGet push runbook |
| [wp-14-handoff.md](wp-14-handoff.md) | G13 docs handoff |
| [wp-15-handoff.md](wp-15-handoff.md) | Preview NuGet handoff |

Root: [README](../README.md), [CONTRIBUTING](../CONTRIBUTING.md),
[SECURITY](../SECURITY.md), [CODE_OF_CONDUCT](../CODE_OF_CONDUCT.md),
[NOTICE](../NOTICE), [CHANGELOG](../CHANGELOG.md).
