# Full regression

Date: 2026-09-23

Commands:

```powershell
dotnet build SysTools.sln --no-restore
dotnet test SysTools.sln --no-build --no-restore
```

Results:

- Build: PASS, 0 warnings, 0 errors.
- Licensing: 91 passed, 0 failed.
- Presentation: 30 passed, 0 failed.
- Business rules: 72 passed, 0 failed.
- Configuration: 33 passed, 0 failed.
- Firebird: 53 passed, 3 skipped integration tests, 0 failed.
- Repositories: 43 passed, 3 skipped integration tests, 0 failed.
- Total: 322 passed, 6 environment-gated skips, 0 failed.
