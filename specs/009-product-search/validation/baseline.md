# Línea base

- Fecha: 2026-09-24
- Solución: `SysTools.sln`; no se agregaron proyectos ni paquetes.
- `dotnet build SysTools.sln --no-restore`: PASS, 0 advertencias, 0 errores.
- `dotnet test SysTools.sln --no-build`: PASS, 516 aprobadas, 0 fallidas, 6 omitidas.
- Omisiones base: 3 pruebas de repositorio Firebird y 3 de integración, todas opt-in por entorno externo.
