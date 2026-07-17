# WP-13 handoff — Observability, security, resilience (G12)

## Summary

Closed acceptance gate **G12**: token redaction + leak tests, HTTP-first
cancellation, thin `ActivitySource`, supply-chain CI, and failure/cancel docs.

## Security / redaction

- Public `LibSqlConnectionStringBuilder.Redact`
- EF `LogFragment` scrubs tokens
- Tests: unit redact aliases; `Security/TokenLeakTests`; DriverContract open leak

## Cancellation

- `LibSqlCommand` async overrides forward CT to remote `LibSqlHttpCommand`
- `LibSqlHttpCommand.Cancel` cancels in-flight linked CTS
- `OpenAsync` remote path uses caller CT + 15s timeout (no `.Wait`)
- Local: pre/post cancel only (documented)
- Tests: `LocalCancellationTests`; remote pre-cancel on sqld suite

## Observability

- `LibSqlActivitySource` (`Nj.LibSql.Data`) on remote execute + Sync
- Docs: [observability.md](observability.md)
- Unit smoke: `LibSqlActivitySourceTests`

## Supply chain

| Gate | Workflow / script |
|------|-------------------|
| CodeQL | `codeql.yml` (existing) |
| dependency-review | `dependency-review.yml` |
| Secret scan | `secret-scan.yml` (gitleaks; enable GitHub push protection in repo settings) |
| SBOM | `eng/generate-sbom.sh` + Syft in `package.yml` |
| Provenance | `actions/attest-build-provenance` on nupkgs in `package.yml` |

## Policy

- No write auto-retries; ambiguous commit documented
- Vulnerability response: [SECURITY.md](../SECURITY.md)

## Explicitly deferred

- Opt-in read retries
- Native mid-execute abort
- Full EF→OTel bridge package

## Next

WP-14 docs polish → WP-15 preview NuGet.
