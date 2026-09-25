# Full regression

Fecha: 2026-09-25

- `dotnet restore SysTools.sln`: PASS.
- `dotnet build SysTools.sln --no-restore`: PASS, 0 warnings, 0 errors.
- `dotnet test SysTools.sln --no-build`: PASS.
- Resultados: 688 passed, 0 failed, 7 skipped.
- SKIPPED: 3 Firebird connection, 3 Firebird repository y 1 physical printer; todos dependen de integraciones externas controladas.

La omisión física mantiene `productionApproved=false` y no se presenta como aprobación operativa.
