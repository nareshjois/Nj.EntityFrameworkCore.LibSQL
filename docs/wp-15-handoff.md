# WP-15 handoff — Preview NuGet (stable track)

## Summary

**G13** docs/samples complete. Preview `10.0.0-preview.1` is on NuGet.org via
[Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing).
Stable `10.0.0` remains a destination — see [releasing.md](releasing.md).

## Compatibility / known gaps

Authoritative matrix: [compatibility.md](compatibility.md). Highlight for
Preview consumers:

- **C-019** — Turso Cloud embedded-replica Sync hang (#24); use sqld for Sync.
- Advertised RIDs only: `linux-x64`, `osx-arm64`, `win-x64`.
- Decimal / regex dialect differences (C-001).

## Publish (done)

1. Tag `v10.0.0-preview.1` (`d445ca7`).
2. Pack CI — SBOM + provenance on tag and publish runs.
3. GitHub Release —
   [v10.0.0-preview.1](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/releases/tag/v10.0.0-preview.1).
4. nuget.org push via Trusted Publishing (`NuGet/login@v1` + `NUGET_USER`) —
   packages: `Nj.EntityFrameworkCore.LibSql`, `Nj.LibSql.Data`, `Nj.LibSql.Bindings`
   at `10.0.0-preview.1` (+ symbols).

Republish / future tags: see [releasing.md](releasing.md).

## Next

Stable backlog (RIDs, skips, signing, API baseline) after Preview feedback.
