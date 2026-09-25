# Feature 008 baseline

Date: 2026-09-24

## Commands

```powershell
dotnet --version
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
dotnet test SysTools.sln --no-build --logger "console;verbosity=minimal"
```

## Results

- SDK: 10.0.401 (compatible with the .NET 10 target).
- Restore: PASS; all projects were already up to date.
- Build: PASS; 0 warnings and 0 errors.
- Deterministic tests: 487 passed, 0 failed.
- Controlled integrations: 6 skipped because no authorized Firebird environment was supplied.
- Projects: 4 production projects and 7 test projects; no project or package was added by Feature 008 setup.

