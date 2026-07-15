## Summary

<!-- What and why -->

## Test plan

- [ ] `dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release`
- [ ] `dotnet test test/Nj.EntityFrameworkCore.LibSql.UnitTests -c Release`
- [ ] `./eng/verify-package.sh` (when packaging surface changes)
- [ ] Compatibility / waiver notes updated in `docs/compatibility.md` if tests skipped

## Notes

<!-- API surface, connection-mode impact, upstream delta -->
