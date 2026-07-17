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

- **`Failed to load libsql native library`:** the process RID must match an
  advertised runtime pack (`linux-x64`, `win-x64`, `osx-arm64`). On Apple Silicon
  containers, force `linux/amd64` so `linux-x64` natives load
  (`./eng/smoke-container.sh`).
- **Missing `runtimes/<rid>/native/libsql.*` in the nupkg:** run
  `./eng/verify-package.sh`; rebuild natives via
  [eng/native/README.md](../eng/native/README.md) / `libsql-native.yml`.
- **Wrong RID in publish:** self-contained / single-file must use `-r` equal to
  an advertised RID. Cross-RID publish without matching natives will fail at
  open.
- **Single-file:** natives must extract beside the host; if load fails on a RID,
  treat single-file as unsupported for that RID (do not invent extract hacks).
- **AOT / heavy trimming:** not a Preview product line; do not rely on
  `PublishAot` yet.
- **Auth tokens in logs:** use `LibSqlConnectionStringBuilder.Redact`; see
  [observability.md](observability.md).

## Related

- Pack verify: `./eng/verify-package.sh`
- Connection modes: [connection-modes.md](connection-modes.md)
- Connection strings: [connection-strings.md](connection-strings.md)
