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

Estado final: PASS. Se ejecutó con la configuración heredada autorizada,
decodificada solamente en memoria: 10 conexiones consecutivas exitosas, cada
una dentro de 6 segundos. No se registraron valores de conexión.
