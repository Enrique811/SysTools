# Data Model: Configuración local y secretos

## AppConfiguration

Entidad compartida en memoria. No contiene atributos de serialización, acceso a archivos ni criptografía.

| Field | Type | Default | Validation / meaning |
|---|---|---|---|
| IpEmpresa | string | `""` | Requerido para `IsConnectionReady`; se evalúa sin espacios exteriores. |
| RutaEmpresa | string | `""` | Requerido para `IsConnectionReady`; no se verifica existencia en esta feature. |
| Usuario | string | `"SYSDBA"` | Requerido para `IsConnectionReady`. |
| Password | string | `""` | Requerido para `IsConnectionReady`; nunca se incluye en `ToString`, errores o logs. |
| Ambiente | string | `"a"` | Solo `a` o `b`; cualquier otro valor bloquea persistencia. |
| Impresora | string | `""` | Opcional; no se enumera ni valida contra Windows. |
| FormatoPrecio | string | `"MX"` | Solo `CO` o `MX`; cualquier otro valor bloquea persistencia. |
| Reporte | string | `""` | Opcional; no se verifica archivo o extensión. |
| Columnas | int | `1` | Solo 1, 2 o 3; cualquier otro valor bloquea persistencia. |
| Informacion | string | `""` | Opcional; conserva Unicode y espacios internos. |
| Licencia | string | `""` | Dato opaco y opcional; se excluye de logs. |

### Invariants

- Ningún string es `null`; al construir o mapear se normaliza `null` a vacío.
- El objeto no implementa una representación textual que enumere valores.
- Solo `Ambiente`, `FormatoPrecio` y `Columnas` determinan `IsPersistable` en v1.
- `IpEmpresa`, `RutaEmpresa`, `Usuario` y `Password` determinan `IsConnectionReady` además de requerir `IsPersistable=true`.
- Validar no modifica los valores originales; los consumidores deciden cuándo mostrar o reemplazar texto.

## ConfigurationIssue

Problema seguro y localizable devuelto por validación o por la coordinación de una operación.

| Field | Type | Rules |
|---|---|---|
| Code | ConfigurationIssueCode | Código estable; no contiene valores. |
| Field | string? | Nombre canónico del campo o `null` para un problema general. |
| Message | string | Mensaje seguro para un consumidor; no contiene payload ni excepción. |
| Severity | ConfigurationIssueSeverity | `Warning` para aptitud futura; `Error` para estructura/persistencia. |

### Codes

- `RequiredForConnection`
- `UnsupportedEnvironment`
- `UnsupportedPriceFormat`
- `UnsupportedColumnCount`
- `ConfigurationNotFound`
- `MalformedConfiguration`
- `SecretUnavailable`
- `StorageUnavailable`
- `AccessDenied`
- `OperationCanceled`

## ConfigurationValidationResult

| Field | Type | Rules |
|---|---|---|
| IsPersistable | bool | Verdadero solo si no existen issues `Error`. |
| IsConnectionReady | bool | Verdadero solo si `IsPersistable` y no existen issues `RequiredForConnection`. |
| Issues | IReadOnlyList&lt;ConfigurationIssue&gt; | Contiene todos los problemas de una evaluación y nunca es `null`. |

### State matrix

| Catalog values | Required connection fields | IsPersistable | IsConnectionReady |
|---|---|---:|---:|
| Valid | Complete | true | true |
| Valid | One or more empty | true | false |
| One or more invalid | Complete | false | false |
| One or more invalid | One or more empty | false | false |

## ConfigurationLoadResult

Resultado de `IConfigurationService.LoadAsync`.

| Field | Type | Meaning |
|---|---|---|
| Status | ConfigurationLoadStatus | `Loaded`, `DefaultCreated`, `InvalidContent`, `SecretUnavailable`, `StorageFailure`, `AccessDenied`, `Canceled`. |
| Configuration | AppConfiguration? | Presente solo en `Loaded` o `DefaultCreated`. |
| Validation | ConfigurationValidationResult? | Presente cuando existe una configuración utilizable en memoria. |
| Issues | IReadOnlyList&lt;ConfigurationIssue&gt; | Resumen seguro; no contiene excepciones. |

## ConfigurationSaveResult

Resultado de `IConfigurationService.SaveAsync`.

| Field | Type | Meaning |
|---|---|---|
| Status | ConfigurationSaveStatus | `Saved`, `ValidationFailed`, `StorageFailure`, `AccessDenied`, `Canceled`. |
| Validation | ConfigurationValidationResult | Evaluación completa de la solicitud. |
| Issues | IReadOnlyList&lt;ConfigurationIssue&gt; | Problemas seguros de validación u operación. |

## StoredConfigurationDocument

Tipo interno de Data. Es el único modelo serializado.

| JSON property | Type | Rules |
|---|---|---|
| schemaVersion | int | `1`; versiones mayores se leen de forma tolerante mientras los campos conocidos sean compatibles. |
| ipEmpresa | string | Mapea a `IpEmpresa`. |
| rutaEmpresa | string | Mapea a `RutaEmpresa`. |
| usuario | string | Mapea a `Usuario`. |
| passwordProtegido | string | Ciphertext DPAPI codificado como Base64; nunca es el password en claro. |
| ambiente | string | Mapea a `Ambiente`. |
| impresora | string | Mapea a `Impresora`. |
| formatoPrecio | string | Mapea a `FormatoPrecio`. |
| reporte | string | Mapea a `Reporte`. |
| columnas | int | Mapea a `Columnas`. |
| informacion | string | Mapea a `Informacion`. |
| licencia | string | Mapea a `Licencia`; no se registra. |
| extensionData | Dictionary&lt;string, JsonElement&gt; | No aparece como nombre JSON; captura y reemite propiedades desconocidas. |

### Security boundaries

```text
AppConfiguration.Password (plaintext, memoria)
        |
        v
ISecretProtector.Protect
        |
        v
StoredConfigurationDocument.passwordProtegido (DPAPI + Base64, disco)
```

La transformación inversa solo ocurre dentro del repositorio al cargar. Un fallo de Base64 o DPAPI produce `SecretUnavailable` y no crea una entidad con password falso.

## Repository operation states

### Load

```text
Request
  ├─ file absent → NotFound → service builds defaults → atomic save → DefaultCreated
  ├─ valid JSON + secret opens → Loaded
  ├─ malformed/type mismatch → InvalidContent (source preserved)
  ├─ secret cannot open → SecretUnavailable (source preserved)
  ├─ access denied / I/O → controlled failure (source preserved)
  └─ cancellation → Canceled
```

### Save

```text
Request
  ├─ not persistable → ValidationFailed (no I/O)
  └─ persistable
      ├─ protect + serialize + replace succeeds → Saved
      ├─ protection fails → StorageFailure (destination preserved)
      ├─ write/replace fails → controlled failure (destination preserved)
      └─ cancellation before replace → Canceled (destination preserved)
```

## Concurrency and ownership

- Una instancia de repositorio serializa operaciones con `SemaphoreSlim`.
- La configuración entregada al servicio no se comparte como estado mutable global.
- El repositorio vuelve a leer propiedades desconocidas del destino antes de guardar y las mezcla sin permitir que sobrescriban nombres v1 conocidos.
- Coordinación multi-proceso, merge de cambios simultáneos y bloqueo distribuido quedan fuera de alcance.
