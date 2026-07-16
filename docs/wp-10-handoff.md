# WP-10 handoff

**Status:** **closed on `main`** — EF specification/compliance harness,
hosted G6–G8 suites (local + remote slice), functional deferred-gap closure,
CI/reporting. Soft-fork pin `@8b5a289` (upstream `0.2.11` + HTTP ISO DateTime
binds). Merged via PR [#19](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/pull/19);
CI follow-up PR [#20](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/pull/20).

## Gates

| Gate | Status | Evidence |
|------|--------|----------|
| G6 Query & types | **Working (hosted)** | `BuiltInDataTypesLibSqlTest`, `OperatorsQueryLibSqlTest`, TPH/TPT/TPC inheritance, `StringTranslationsLibSqlTest`, `ModelBuilding101LibSqlTest` |
| G7 Updates & txn | **Working (hosted)** | `UpdatesLibSqlTest` (36/36), `TransactionLibSqlTest`, `OptimisticConcurrencyLibSqlTest`, `StoreGeneratedLibSqlTest` |
| G8 Migrations | **Working (hosted)** | `LibSqlMigrationsSqlGeneratorTest` (Sqlite-parity overrides), functional migration deferred matrix |
| Compliance harness | **Working** | `LibSqlComplianceTest` + `docs/provider-capabilities.json` + `ComplianceGuardTests` |
| Remote compliance | **Working (slice)** | `BuiltInDataTypesRemoteLibSqlTest` + `integration.yml` remote filter |
| CI reporting | **Working** | `eng/generate-compliance-report.sh`, nightly artifact upload |

## Verify

```bash
# Local compliance gate (CI parity; excludes remote BuiltInDataTypes)
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release \
  --filter "FullyQualifiedName!~BuiltInDataTypesRemoteLibSqlTest"

# Full local compliance (~2.9k tests; see waiver log for known deltas)
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release

# Functional deferred gaps (WP-06/07/08)
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release

# Compliance report artifact
./eng/generate-compliance-report.sh

# Remote compliance (Docker / LIBSQL_TEST_URL)
LIBSQL_TEST_URL=http://127.0.0.1:8080 ./eng/generate-compliance-report.sh
# or integration workflow remote slice:
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release --filter "FullyQualifiedName~Remote"

# Regenerate unhosted-suite waivers after adding/removing spec hosts
./eng/generate-provider-capabilities.sh
```

## Infrastructure delivered

| Asset | Path |
|-------|------|
| Test store | `test/.../ComplianceTests/Infrastructure/LibSqlTestStore.cs` (`EnsureClean`, stable file stores) |
| Remote fixture | `Infrastructure/RemoteLibSqlComplianceFixture.cs` |
| Capability manifest | `docs/provider-capabilities.json` |
| Skip guard | `test/.../UnitTests/ComplianceGuardTests.cs` (bans raw `[Fact(Skip=…)]`) |
| Report script | `eng/generate-compliance-report.sh` |

## Hosted specification suites (local)

`BuiltInDataTypes`, `Transaction`, `Updates`, `StoreGenerated`, `OptimisticConcurrency`,
`ValueConverters`, `WithConstructors`, `ModelBuilding101`, `ConcurrencyDetector` (enabled/disabled),
`OperatorsQuery`, TPH/TPT/TPC inheritance, `StringTranslations`, `GraphUpdates` (ChangedNotifications),
`LibSqlMigrationsSqlGeneratorTest`.

Unhosted EF relational bases remain waived via `C-AUTO` rows in `provider-capabilities.json`
(regenerate with `./eng/generate-provider-capabilities.sh`).

## Functional deferred closure (WP-06–08)

| Area | Tests |
|------|-------|
| WP-06 | `Query/QueryDeferredCases.cs`, `LocalQueryDeferredTests`, `RemoteQueryDeferredTests` (TPH, glob/hex/substr, compiled query, interceptors) |
| WP-07 | `Updates/UpdateDeferredCases.cs`, `LocalUpdateDeferredTests`, `RemoteUpdateDeferredTests` (savepoints, busy/locked, cancel, RETURNING+trigger, pool stress) |
| WP-08 | `Migrations/MigrationDeferredCases.cs`, `LocalMigrationDeferredTests`, `RemoteMigrationDeferredTests` (concurrent migrators, lock recovery, op matrix, unsupported ops, remote txn migrate, failure/resume, multi-version, N→N+1 pin) |

## Known compliance waivers (hosted suite deltas)

Documented in [compatibility.md](compatibility.md) `C-008`–`C-016`. Nightly `compliance-report`
artifact tracks pass/fail/skip counts by suite. PR CI runs the full **local** compliance
gate (excludes `BuiltInDataTypesRemoteLibSqlTest`; see C-016). Remote compliance is
tracked in `integration.yml` with `continue-on-error`.

## Out of scope (Preview 2+ roadmap — not “deferred”)

Embedded replica, Turso Cloud matrix, network fault injection, full remote compliance parity,
WP-11/12 platform/perf matrix.

## Next

- WP-11 connection-mode matrix (local/remote depth; embedded replica Preview 2+).
- Preview 2: expand hosted suites / burn down remaining `C-008`–`C-016` deltas where feasible.
- Publish `10.0.0-preview.2` after Nelknet upstream alignment.
