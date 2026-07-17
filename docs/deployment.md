# Deployment and native assets

Preview packaging notes for `Nj.LibSql.Bindings` / `Nj.EntityFrameworkCore.LibSql`.

## Advertised RIDs

| RID | CI / validation |
|-----|-----------------|
| `linux-x64` | `ci.yml` (`ubuntu-latest`), container smoke |
| `win-x64` | `ci.yml` (`windows-latest`) |
| `osx-arm64` | Committed natives; maintainer-validated (Apple Silicon) |

**Not advertised yet:** `win-arm64`, `linux-arm64`, `osx-x64`, musl/Alpine, mobile.

## Publish modes (Preview)

| Mode | Status | How to smoke |
|------|--------|----------------|
| Framework-dependent | Supported | `./eng/smoke-publish.sh` |
| Self-contained (`-r <rid>`) | Supported for advertised RIDs | same script |
| Single-file (`PublishSingleFile=true`) | Supported on advertised RIDs when natives extract with the host (validated on `osx-arm64` / CI RIDs) | same script |
| Container (`linux-x64`) | Supported on CI (`ubuntu-latest`); requires Docker + pull from MCR | `./eng/smoke-container.sh` (forces `linux/amd64`) |

```bash
./eng/smoke-publish.sh            # uses current machine RID
./eng/smoke-publish.sh linux-x64  # cross-publish when SDK supports it
./eng/smoke-container.sh          # requires Docker
```

## Troubleshooting

- **Missing native library:** ensure the package includes `runtimes/<rid>/native/libsql.*`
  and the app RID matches. Rebuild natives via [eng/native/README.md](../eng/native/README.md).
- **Wrong RID advertised:** Preview metadata must not claim RIDs without smoke — see
  [architecture.md](architecture.md) and [versions.md](versions.md).
- **AOT / heavy trimming:** not a Preview product line; do not rely on PublishAot yet.

## Related

- Pack verify: `./eng/verify-package.sh`
- Connection modes: [connection-modes.md](connection-modes.md)
