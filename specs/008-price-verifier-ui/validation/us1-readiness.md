# US1 readiness validation

**Result**: PASS — 2026-09-24

- La matriz automatizada cubre todos los estados de carga, conexión y licencia, con orden `Configuration -> Connection -> License` y short-circuit.
- Solo `Loaded` con configuración lista, conexión `Success` y licencia `Valid` conserva una sesión privada Ready.
- El ViewModel publica Pending/Ready/Blocked, Retry repite la preparación y la shell refleja la misma instancia de estado.
- DI valida con `ValidateOnBuild` y `ValidateScopes`; workflow y ViewModel son singleton.

Comandos verificados:

```powershell
dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --no-build --filter "FullyQualifiedName~PriceVerifier"
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --no-build --filter "FullyQualifiedName~PriceVerifier|FullyQualifiedName~ShellStatus|FullyQualifiedName~Mvvm|FullyQualifiedName~DependencyInjection"
```

Resultados finales de los filtros: Business 45 PASS; Presentation 27 PASS.
