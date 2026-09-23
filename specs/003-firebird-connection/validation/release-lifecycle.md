# Pruebas Release y ciclo de vida

Fecha: 2026-09-23

Comandos principales:

```powershell
dotnet build SysTools.sln -c Release --no-restore
dotnet test SysTools.sln -c Release --no-build
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Release --no-build --filter 'Category=Lifecycle'
```

Resultados:

- Build Release: PASS; 0 warnings, 0 errores.
- Suite completa: 110 passed, 0 failed, 3 integraciones skipped.
- Build Release final: 6.35 segundos, 0 warnings y 0 errores.
- Filtro Lifecycle: 6 passed, 0 failed, 0 skipped; 85 ms reportados por el runner.
- La prueba de 100 intentos verificó 100 conexiones creadas y 100 disposiciones
  únicas entre éxitos, fallos y cancelaciones.
- La prueba concurrente verificó una solicitud cancelada y otra exitosa sin
  compartir conexión, resultado ni ciclo de vida.
