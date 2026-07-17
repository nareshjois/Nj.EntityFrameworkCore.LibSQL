# WP-14 handoff — Documentation and samples (G13)

## Summary

Consumer and contributor docs meet **G13**: local + self-hosted remote samples
run from public instructions (no Turso secrets).

## Delivered

- README quick starts (local / remote / replica) + DiPoolingSample +
  MigrationsSample pointer
- Docs: connection-strings, migrate-from-ef-sqlite, transactions, expanded
  versions/deployment/architecture
- CONTRIBUTING first-time path
- XML: `CS1591` as error on `Nj.LibSql.Data`; EF baseline keeps `NoWarn;CS1591`

## Acceptance

Contributor: restore → build → unit tests → LocalSample → start-sqld →
RemoteSample.
