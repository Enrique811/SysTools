# Validación de build y pruebas

Fecha: 2026-09-20. SDK seleccionado: 10.0.401 mediante `global.json` y `.dotnet` local.

| Comando | Duración | Resultado |
|---|---:|---|
| `dotnet restore SysTools.sln` | 1.87 s | correcto |
| `dotnet build SysTools.sln --no-restore` | 3.11 s | correcto, 0 warnings, 0 errores |
| `dotnet test SysTools.sln --no-build --no-restore` | 2.72 s | 18/18 correctas, 0 omitidas |

También se ejecutó build Release: 0 warnings y 0 errores.
