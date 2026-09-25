# Full regression

**Result**: PASS — 2026-09-24

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
dotnet test SysTools.sln --no-build
```

- Restore: PASS.
- Build: PASS, 0 warnings, 0 errors.
- Tests: 516 passed, 0 failed, 6 skipped, 522 total.
- Skips: exclusivamente seis integraciones Firebird/repositorio opt-in sin entorno autorizado.
- Runtime smoke: el ejecutable WPF permaneció activo durante 6 segundos sin configuración local y cerró de forma controlada.
