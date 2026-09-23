# Build y pruebas

Fecha: 2026-09-23

Entorno:

- SDK efectivo: .NET `10.0.401`.
- Target productivo: `net10.0`; WPF y pruebas: `net10.0-windows`.
- FirebirdSql.Data.FirebirdClient: `10.3.4`.

Comandos:

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln -c Debug --no-restore
dotnet test SysTools.sln -c Debug --no-build
dotnet build SysTools.sln -c Release --no-restore
dotnet test SysTools.sln -c Release --no-build
```

Resultados Debug y Release:

- Restore: PASS.
- Build: PASS; 0 warnings y 0 errores en ambas configuraciones.
- `SysTools.Repositories.Tests`: 43 passed, 0 failed, 3 skipped.
- `SysTools.Firebird.Tests`: 53 passed, 0 failed, 3 skipped.
- `SysTools.Presentation.Tests`: 26 passed, 0 failed.
- `SysTools.Configuration.Tests`: 33 passed, 0 failed.
- Total: 155 passed, 0 failed, 6 skipped.

Las seis omisiones son tres integraciones de conexion 003 y tres integraciones
de repositorios 004. Todas requieren un entorno Firebird no productivo
autorizado; no hay pruebas unitarias omitidas.
