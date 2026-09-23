# Build y pruebas Debug

Fecha: 2026-09-23

Entorno:

- SDK efectivo: .NET `10.0.401` (compatible con el roll-forward de `global.json`).
- Target productivo: `net10.0` y WPF `net10.0-windows`.
- FirebirdSql.Data.FirebirdClient: `10.3.4`.

Comandos:

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln -c Debug --no-restore
dotnet test SysTools.sln -c Debug --no-build
```

Resultados:

- Restore: PASS; todos los proyectos actualizados.
- Build Debug: PASS; 0 warnings, 0 errores.
- `SysTools.Firebird.Tests`: 53 passed, 0 failed, 3 skipped.
- `SysTools.Presentation.Tests`: 24 passed, 0 failed, 0 skipped.
- `SysTools.Configuration.Tests`: 33 passed, 0 failed, 0 skipped.
- Total: 110 passed, 0 failed, 3 skipped.

Las tres omisiones son integraciones Firebird que requieren variables y un
servidor no productivo autorizado; no representan pruebas unitarias omitidas.
