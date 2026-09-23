# Implementation Plan: Conexión y prueba Firebird

**Branch**: `003-firebird-connection` | **Date**: 2026-09-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-firebird-connection/spec.md`

## Summary

Agregar una capacidad interna para construir conexiones Firebird desde `AppConfiguration` y comprobarlas sin guardar primero la configuración ni exponer el proveedor fuera de Data. Entities incorporará el resultado seguro y sus estados; Business validará los cuatro campos requeridos, coordinará la prueba y producirá mensajes controlados; Data construirá cada `FbConnection` mediante `FbConnectionStringBuilder`, abrirá y desechará una instancia por intento, aplicará timeout/cancelación y traducirá fallos técnicos con códigos estables. Presentation solo registrará los servicios en el composition root.

La conexión fijará `DataSource`, `Database`, `UserID`, `Password`, puerto `3050`, charset `ISO8859_1`, dialecto 3 y timeout de 5 segundos. La prueba combinará ese timeout del proveedor con un `CancellationTokenSource` enlazado de 5 segundos, dará prioridad a una cancelación explícita del consumidor cuando coincida con el límite y mantendrá los logs libres de configuración, cadenas de conexión, mensajes crudos del proveedor y secretos.

## Technical Context

**Language/Version**: C# 14 sobre .NET 10 (`global.json` fija 10.0.100 con roll-forward a la feature instalada)

**Primary Dependencies**: `FirebirdSql.Data.FirebirdClient` 10.3.4 en Data; `System.Data.Common` para el contrato de fábrica; `Microsoft.Extensions.Logging.Abstractions` 10.0.12 ya presente en Business/Data; DI y Serilog ya compuestos por Presentation

**Storage**: Firebird remoto existente; esta feature no crea tablas, ejecuta migraciones ni persiste datos nuevos

**Testing**: xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, conexiones dobles basadas en `DbConnection` y pruebas de integración opt-in contra una base Firebird no productiva

**Target Platform**: Windows 11 x64; aplicación WPF `net10.0-windows` y capas no visuales `net10.0`

**Project Type**: Aplicación Windows Desktop WPF con proyectos separados Presentation, Business, Data y Entities

**Performance Goals**: Toda prueba termina en menos de 6 segundos; el timeout funcional es 5 segundos; 10 de 10 conexiones reales consecutivas válidas tienen éxito; 100 intentos automatizados disponen exactamente una conexión cada uno

**Constraints**: Charset `ISO8859_1`, puerto fijo 3050, dialecto 3, sin ORM, sin conexión global, sin reintentos, sin consultas funcionales, cancelable, sin secretos ni cadena completa en mensajes/logs, y sin cambiar el JSON de configuración

**Scale/Scope**: Una conexión independiente por solicitud; dos o más pruebas pueden coexistir sin compartir estado; ocho estados de salida; cuatro campos de readiness; una suite unitaria determinista y una suite de integración condicionada por variables de entorno

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-design gate

| Principle / restriction | Evaluation | Evidence in this plan |
|---|---|---|
| 3 capas + Entities | PASS | Entities contiene resultados; Business contratos, validación y coordinación; Data fábrica, proveedor y clasificación técnica; Presentation solo compone. |
| WPF + MVVM sin lógica en vistas | PASS | No se agrega UI y ninguna View/ViewModel crea cadenas, abre conexiones o interpreta errores. |
| Migración incremental Spec Kit | PASS | Es la tercera base del primer sprint, posterior a shell y configuración, y excluye repositorios/consultas. |
| Compatibilidad funcional y mejora del legado | PASS | Conserva host, ruta, usuario, password, charset y timeout; elimina la conexión global y los mensajes técnicos al usuario. |
| Seguridad, logging y configuración controlada | PASS | Consume el password en memoria, no persiste otra copia y registra solo estados, duración, tipos y códigos numéricos seguros. |
| Stack aprobado | PASS | Usa C#, .NET 10, FirebirdSql.Data.FirebirdClient, DI y Serilog, sin EF Core. |
| Validación proporcional al riesgo | PASS | Incluye pruebas puras, recursos dobles, concurrencia, auditoría de secretos y conexión real opt-in documentada. |
| Compatibilidad de encoding Firebird | PASS | Esta etapa fija y verifica `ISO8859_1`; los valores de producto, nulos, stock, textos largos y decimales se validarán en la feature de repositorios que los consulta. |
| Orden del primer sprint | PASS | Completa la conexión Firebird antes de repositorios, licencia, UI completa y reportes. |

No existen violaciones que requieran Complexity Tracking.

### Post-design gate

El modelo y los contratos conservan las fronteras: `IConnectionTestService` e `IFirebirdConnectionProbe` pertenecen a Business y solo exponen tipos de Entities; `IFirebirdConnectionFactory`, `FirebirdConnectionFactory`, el adaptador del proveedor y el clasificador técnico pertenecen a Data. La fábrica devuelve `DbConnection`, por lo que Business, Entities, Views y ViewModels no referencian `FbConnection` ni el paquete Firebird. El quickstart exige validación real no productiva, auditoría de secretos y comprobación estática de dependencias. Todos los gates continúan en PASS.

## Project Structure

### Documentation (this feature)

```text
specs/003-firebird-connection/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── connection-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # generado por speckit-tasks
```

### Source Code (repository root)

```text
src/
├── Entities/
│   └── Connection/
│       ├── ConnectionConfigurationIssue.cs
│       ├── ConnectionTestResult.cs
│       └── ConnectionTestStatus.cs
├── Business/
│   └── Connection/
│       ├── ConnectionConfigurationValidator.cs
│       ├── ConnectionTestService.cs
│       ├── IConnectionTestService.cs
│       └── IFirebirdConnectionProbe.cs
├── Data/
│   └── Connection/
│       ├── FirebirdConnectionDefaults.cs
│       ├── FirebirdConnectionFactory.cs
│       ├── FirebirdConnectionProbe.cs
│       ├── FirebirdErrorClassifier.cs
│       └── IFirebirdConnectionFactory.cs
└── Presentation/
    └── App.xaml.cs              # solo registros DI

tests/
├── SysTools.Firebird.Tests/
│   ├── Entities/
│   ├── Business/
│   ├── Data/
│   ├── Integration/
│   ├── Security/
│   └── TestDoubles/
└── SysTools.Presentation.Tests/
    ├── Architecture/
    └── Composition/
```

**Structure Decision**: Se conservan los cuatro proyectos productivos. La nueva suite `SysTools.Firebird.Tests` referencia Entities, Business y Data para probar reglas, fábrica, ciclo de vida y clasificación sin WPF; las pruebas existentes de Presentation validan el registro DI y las fronteras arquitectónicas. El paquete Firebird se agrega únicamente a `src/Data/SysTools.Data.csproj`.

## Design Decisions

### Superficie Business y frontera Data

- `IConnectionTestService` es la única superficie destinada a Presentation y consumidores de casos de uso.
- `ConnectionTestService` valida toda la entrada antes de invocar Data y retorna un `ConnectionTestResult` seguro para cualquier salida esperada.
- `IFirebirdConnectionProbe`, definido en Business, abstrae la apertura técnica y devuelve solo un `ConnectionTestStatus`; Data lo implementa.
- `IFirebirdConnectionFactory`, definido y consumido dentro de Data, retorna `DbConnection`. Los futuros repositorios podrán reutilizarlo sin que el cliente Firebird cruce hacia Business o Entities.
- Los servicios son stateless y pueden registrarse como singleton; cada llamada crea y desecha su propia conexión.

### Construcción de conexión

- `FirebirdConnectionFactory` usa `FbConnectionStringBuilder`; no concatena una cadena manualmente.
- El mapeo es `IpEmpresa → DataSource`, `RutaEmpresa → Database`, `Usuario → UserID` y `Password → Password`.
- Se fijan `Port=3050`, `Charset="ISO8859_1"`, `Dialect=3`, `ConnectionTimeout=5` y `Pooling=true`.
- Los valores recibidos no se mutan ni persisten; la validación usa `IsNullOrWhiteSpace`, pero no recorta el password.
- Cada llamada retorna un objeto nuevo. Cerrar/desechar el objeto es obligatorio aunque el pool interno del proveedor permanezca habilitado.

### Timeout, cancelación y propiedad del recurso

- `FirebirdConnectionProbe` crea un token enlazado al consumidor y programa un límite de 5 segundos además del timeout de la cadena.
- `OpenAsync` recibe el token enlazado y la conexión vive dentro de `await using` para asegurar `DisposeAsync` en éxito, fallo o cancelación.
- Si el token del consumidor está cancelado, el estado final es `Canceled`; si solo venció el token interno o aparece `TimeoutException`, es `Timeout`.
- Cuando ambos ocurren casi simultáneamente, la cancelación explícita del consumidor tiene precedencia.
- No se captura ni reutiliza una conexión abierta como estado del servicio.

### Clasificación conservadora de fallos

- La clasificación usa tipos de excepción y números de la colección `FbException.Errors`; nunca compara texto localizado.
- Autenticación, red y base se reconocen mediante listas explícitas y cubiertas por pruebas. Los códigos de I/O durante el attach se consideran base no disponible; los códigos de red se consideran servidor no disponible.
- Excepciones o códigos no reconocidos producen `UnexpectedFailure` en vez de afirmar una causa.
- El clasificador no devuelve la excepción, su mensaje, host, ruta, usuario ni cadena de conexión.

### Diagnóstico seguro

- Business registra `ConnectionTestStarted` con operación; `ConnectionValidationFailed` con operación, outcome, duración, campos y cantidad de issues; y `ConnectionTestCompleted` con operación, outcome y duración.
- Data registra `FirebirdConnectionFailed` con estado, tipo de excepción y colección numérica de códigos cuando existen.
- No se pasa la excepción como parámetro de logging en este flujo porque su mensaje puede contener host, ruta o fragmentos de conexión.
- No se permite destructurar `AppConfiguration`, `DbConnection`, `FbException` ni el connection string.

### Estrategia de pruebas

- Business usa un probe doble para verificar validación completa, mensajes, estados, duración, cancelación y que configuraciones inválidas no llegan a Data.
- Data usa una fábrica doble de `DbConnection` para demostrar apertura/disposición exactamente una vez en 100 combinaciones y aislamiento entre solicitudes concurrentes.
- La fábrica real se inspecciona mediante `FbConnectionStringBuilder` para verificar mapeo, charset, puerto, dialecto y timeout sin imprimir el connection string.
- El clasificador se prueba por familias de códigos numéricos Firebird —incluidos los códigos de red— y fallback desconocido; el probe se prueba por separado para timeout, cancelación y excepciones conocidas o desconocidas.
- La integración real es opt-in mediante variables de entorno; se omite sin credenciales, pero no cuenta como validación operativa hasta registrar al menos 10 éxitos consecutivos y los fallos autorizados disponibles.

## Complexity Tracking

No aplica: el diseño no introduce excepciones a la constitución.
