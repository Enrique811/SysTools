# Validación US1: conexión exitosa

Fecha: 2026-09-23

## Pruebas con dobles

Comando:

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj --configuration Debug --no-restore
```

Resultado: PASS. 16 pruebas superadas, 0 fallidas y 1 integración omitida.

Se verificaron los ocho estados e invariantes del resultado, el mapeo exacto del
builder Firebird, la creación de conexiones independientes, `OpenAsync`, el token
enlazado, disposición única, coordinación Business y eventos seguros de inicio/fin.

## Composición

Comando:

```powershell
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~FirebirdDependencyInjectionTests"
```

Resultado: PASS. 1 prueba superada. La fábrica, el probe, el servicio y la shell se
resolvieron sin intentar abrir una conexión durante el arranque.

## Integración real

Estado: SKIPPED. `SYSTOOLS_FIREBIRD_RUN_INTEGRATION` no está habilitada en este
entorno. No se leyeron ni registraron valores de conexión. La prueba opt-in queda
preparada para ejecutar diez conexiones consecutivas y exigir menos de seis
segundos por intento cuando exista un entorno autorizado.
