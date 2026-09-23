# Contract: Configuración local

## Business surface

El consumidor usa exclusivamente `IConfigurationService`:

```csharp
public interface IConfigurationService
{
    Task<ConfigurationLoadResult> LoadAsync(
        CancellationToken cancellationToken = default);

    ConfigurationValidationResult Validate(
        AppConfiguration configuration);

    Task<ConfigurationSaveResult> SaveAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
```

Business consume Data mediante esta abstracción:

```csharp
public interface IConfigurationRepository
{
    Task<ConfigurationRepositoryReadResult> ReadAsync(
        CancellationToken cancellationToken = default);

    Task<ConfigurationRepositoryWriteResult> WriteAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
```

Los resultados de repositorio son internos al flujo Business/Data y nunca contienen una excepción destinada a UI. El servicio traduce sus estados a los resultados compartidos descritos en `data-model.md`.

## Behavioral contract

### LoadAsync

- Si el archivo existe y es válido, retorna `Loaded`, la configuración y su validación completa.
- Si no existe, construye defaults, los persiste y retorna `DefaultCreated` solo si el guardado finaliza.
- Si crear defaults falla, retorna el estado de fallo; no afirma que la configuración existe.
- Si el archivo existe pero está malformado, no lo sobrescribe y retorna `InvalidContent`.
- Si el secreto no puede abrirse, no retorna una configuración parcialmente confiable y usa `SecretUnavailable`.
- La cancelación se observa antes de iniciar y durante I/O; después de consumarse el reemplazo, el resultado puede ser `Saved/Loaded` aunque el token cambie.

### Validate

- Evalúa todos los campos en una sola pasada lógica.
- No cambia el objeto recibido ni realiza I/O.
- Issues de campos requeridos para conexión son `Warning`; catálogos fuera de rango son `Error`.
- El orden de issues es estable: `ipEmpresa`, `rutaEmpresa`, `usuario`, `password`, `ambiente`, `formatoPrecio`, `columnas`, seguido de issues generales.

### SaveAsync

- Ejecuta validación antes de Data.
- Si `IsPersistable=false`, retorna `ValidationFailed` y no invoca escritura.
- Si `IsPersistable=true`, permite guardar aunque `IsConnectionReady=false`.
- Una escritura exitosa preserva exactamente los once valores funcionales al recargar.
- Cualquier fallo antes del reemplazo deja el destino anterior sin cambios.

## JSON contract v1

El archivo es UTF-8 y tiene esta forma. Los valores son ilustrativos y no constituyen credenciales reales.

```json
{
  "schemaVersion": 1,
  "ipEmpresa": "192.0.2.10",
  "rutaEmpresa": "C:\\Datos\\empresa.fdb",
  "usuario": "SYSDBA",
  "passwordProtegido": "<base64-de-ciphertext-dpapi>",
  "ambiente": "a",
  "impresora": "",
  "formatoPrecio": "MX",
  "reporte": "",
  "columnas": 1,
  "informacion": "",
  "licencia": ""
}
```

Rules:

- `password` en claro nunca es una propiedad JSON.
- `passwordProtegido` es Base64 de bytes producidos por DPAPI, no Base64 del texto original.
- Nombres conocidos son case-sensitive y se escriben exactamente como arriba.
- Propiedades desconocidas se aceptan y conservan al reescribir.
- Una propiedad conocida con tipo incompatible hace que el documento sea `InvalidContent`.
- Un campo conocido ausente se completa con el default v1 y aparece como issue de readiness cuando corresponda; no se inventa una credencial.
- `schemaVersion` ausente se interpreta como v1 para tolerar archivos tempranos; una versión mayor se acepta solo si los campos v1 siguen siendo compatibles.
- El repositorio rechaza documentos mayores de 1 MB antes de deserializarlos.

## Validation catalogue

| Field | Persistable values | Connection-ready rule |
|---|---|---|
| ipEmpresa | Any non-null string | Non-whitespace |
| rutaEmpresa | Any non-null string | Non-whitespace |
| usuario | Any non-null string | Non-whitespace |
| password | Any non-null string | Non-whitespace |
| ambiente | `a`, `b` | Same as persistable |
| impresora | Any non-null string | Not applicable |
| formatoPrecio | `CO`, `MX` | Same as persistable |
| reporte | Any non-null string | Not applicable |
| columnas | `1`, `2`, `3` | Same as persistable |
| informacion | Any non-null string | Not applicable |
| licencia | Any non-null string | Not applicable |

## Diagnostic contract

Events use stable names and scalar metadata only:

| Event | Level | Safe properties |
|---|---|---|
| `ConfigurationLoadStarted` | Debug | Operation, SchemaVersionExpected |
| `ConfigurationLoadCompleted` | Information | Operation, Outcome, IsConnectionReady, IssueCount |
| `ConfigurationDefaultCreated` | Information | Operation, Outcome |
| `ConfigurationValidationFailed` | Warning | Operation, IssueCodes, IssueFields |
| `ConfigurationSaveCompleted` | Information | Operation, Outcome, IsConnectionReady |
| `ConfigurationOperationFailed` | Error | Operation, Outcome, exception object |

Forbidden properties and patterns:

- Password or protected password.
- License value.
- Full configuration via destructuring (`{@Configuration}`).
- Database host/path or report/printer/information values.
- JSON payload or temporary file contents.

## Composition contract

`App.ConfigureServices` must resolve one `IConfigurationService` backed by `ConfigurationService`, `ConfigurationValidator` and `JsonConfigurationRepository`. Data dependencies use a production AppData path provider, DPAPI protector and atomic writer. Tests may replace those three seams without changing Business or Presentation.

No View, ViewModel or command may reference `JsonConfigurationRepository`, `DpapiSecretProtector`, file APIs or `ProtectedData`.
