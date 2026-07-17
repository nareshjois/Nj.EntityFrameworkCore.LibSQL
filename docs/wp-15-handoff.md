# WP-15 handoff — Preview NuGet (stable track)

## Summary

**G13** docs/samples complete. Preview package stream is
`10.0.0-preview.1`. Stable `10.0.0` remains a destination — see stable backlog
in [releasing.md](releasing.md).

## Compatibility / known gaps

Authoritative matrix: [compatibility.md](compatibility.md). Highlight for
Preview consumers:

- **C-019** — Turso Cloud embedded-replica Sync hang (#24); use sqld for Sync.
- Advertised RIDs only: `linux-x64`, `osx-arm64`, `win-x64`.
- Decimal / regex dialect differences (C-001).

## Publish

Follow [releasing.md](releasing.md): tag `v10.0.0-preview.1` → `package.yml` →
manual `dotnet nuget push`.

## Next

Stable backlog (RIDs, skips, signing, API baseline) after Preview feedback.
