# Contract: Conexión y prueba Firebird

## Business surface

Presentation y futuros casos de uso consumen únicamente:

```csharp
public interface IConnectionTestService
{
    Task<ConnectionTestResult> TestAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
```

Business invierte la dependencia hacia Data mediante:

```csharp
public interface IFirebirdConnectionProbe
{
    Task<ConnectionTestStatus> ProbeAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
```

`ConnectionTestService` es responsable de validación, duración, mensaje seguro y evento final. `IFirebirdConnectionProbe` no retorna excepciones, cadenas ni códigos técnicos.

## Data-only factory surface

Los repositorios Data presentes y futuros comparten una fábrica que no cruza la frontera de capa:

```csharp
public interface IFirebirdConnectionFactory
{
    DbConnection Create(AppConfiguration configuration);
}
```

Rules:

- `Create` retorna una instancia nueva y cerrada en cada llamada.
- El tipo concreto es `FbConnection`, pero ningún contrato Business/Entities lo referencia.
- El llamador Data adquiere propiedad exclusiva y debe cerrar/desechar la instancia.
- La fábrica no abre, prueba, almacena ni registra la conexión.
- La fábrica no conserva `AppConfiguration` ni connection strings como estado.

## TestAsync behavioral contract

### Validation

- Rechaza `null` como error de programación mediante `ArgumentNullException` antes de logging de datos.
- Evalúa `IpEmpresa`, `RutaEmpresa`, `Usuario` y `Password` en orden estable.
- Retorna todos los campos ausentes en un único `InvalidConfiguration`.
- Una configuración inválida crea cero conexiones y no invoca el probe.
- Campos no relacionados (`Ambiente`, `FormatoPrecio`, `Columnas`, licencia, impresora o reporte) no bloquean esta prueba.

### Successful connection

- Acepta configuración proporcionada en memoria, esté o no guardada.
- Abre exactamente una conexión y la dispone antes de completar.
- Retorna `Success`, mensaje fijo, duración no negativa e issues vacíos.
- No ejecuta SQL, consulta metadata, fecha del servidor ni productos.

### Timeout and cancellation

- Usa un límite de 5 segundos tanto en la cadena como en el token interno.
- Propaga el token enlazado a `DbConnection.OpenAsync`.
- Cancelación explícita produce `Canceled`; deadline interno produce `Timeout`.
- Cancelación explícita gana si ambos tokens se señalan casi al mismo tiempo.
- El método completa con resultado controlado, no con excepción esperable.

### Failure classification

- `AuthenticationFailed`, `ServerUnavailable` y `DatabaseUnavailable` requieren tipo/código reconocido.
- Mensajes localizados no participan en la clasificación.
- Código o excepción ambigua produce `UnexpectedFailure`.
- Todo fallo cierra/desecha la conexión y deja intacta la configuración.

## Connection string contract

La fábrica configura el builder sin publicar su salida:

| App/source | FbConnectionStringBuilder property | Exact value |
|---|---|---|
| `AppConfiguration.IpEmpresa` | `DataSource` | Valor recibido |
| `AppConfiguration.RutaEmpresa` | `Database` | Valor recibido |
| `AppConfiguration.Usuario` | `UserID` | Valor recibido |
| `AppConfiguration.Password` | `Password` | Valor recibido solo en memoria |
| Constant | `Port` | `3050` |
| Constant | `Charset` | `ISO8859_1` |
| Constant | `Dialect` | `3` |
| Constant | `ConnectionTimeout` | `5` |
| Constant | `Pooling` | `true` |

No existe API pública para obtener el connection string. Las pruebas inspeccionan propiedades individuales con valores centinela y no imprimen la cadena completa.

## Error classification contract

La política inicial usa los códigos siguientes cuando aparecen en cualquier elemento de `FbException.Errors`:

| Category | Codes |
|---|---|
| Authentication | `335544472`, `335545106` |
| Server/network | `335544421`, `335544721`, `335544726`, `335544727` |
| Database/attach | `335544323`, `335544344`, `335544375`, `335544379` |

Precedence:

1. Token del consumidor cancelado → `Canceled`.
2. Deadline interno o `TimeoutException` → `Timeout`.
3. Código de autenticación → `AuthenticationFailed`.
4. Código de red → `ServerUnavailable`.
5. Código de base/attach → `DatabaseUnavailable`.
6. Tipo/código no reconocido → `UnexpectedFailure`.

La lista se amplía solo con evidencia oficial y una prueba que demuestre que el código no es ambiguo.

## Diagnostic contract

| Event | Level | Safe properties |
|---|---|---|
| `ConnectionTestStarted` | Information | Operation |
| `ConnectionValidationFailed` | Warning | Operation, Outcome, IssueFields, IssueCount, DurationMs |
| `ConnectionTestCompleted` | Information | Operation, Outcome, DurationMs |
| `FirebirdConnectionFailed` | Warning/Error | Operation, Outcome, ExceptionType, ErrorCodes |

Forbidden properties/content:

- Password o password protegido.
- Licencia.
- Connection string completa o parcial.
- `AppConfiguration` serializada/desestructurada.
- Objeto `DbConnection`, `FbConnection`, `Exception` o `FbException` desestructurado.
- Mensaje de excepción sin sanitizar.
- Host, ruta, usuario o payload JSON.

## Composition contract

`App.ConfigureServices` registra una fábrica Data, un probe Data, el validador Business y `IConnectionTestService`. Los registros pueden ser singleton porque no almacenan configuración ni conexión; las conexiones son objetos por llamada.

No View, ViewModel ni comando puede referenciar `FirebirdSql.Data.FirebirdClient`, `FbConnection`, `FirebirdConnectionFactory` o construir una cadena. No se agregan botones, comandos visuales ni inicialización automática de red.
