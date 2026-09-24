# Data Model: Validacion de licencias heredadas

## LicenseIssuer

Identidad autenticada por la firma.

| Value | Meaning |
|-------|---------|
| `None` | No se autentico ningun emisor. |
| `DistributorColombia` | Firma valida con la llave del distribuidor Colombia. |
| `Developer` | Firma valida con la llave del desarrollador. |

## LicenseValidationStatus

Estado unico y final; no se combinan booleanos que puedan contradecirse.

| Value | Meaning |
|-------|---------|
| `Valid` | Fuente, estructura, firma, UUID y vigencia validos. |
| `MissingInput` | Entrada nula, vacia o solo espacios. |
| `SourceUnavailable` | Archivo ausente, inaccesible, demasiado grande o ilegible. |
| `InvalidJson` | No es un objeto JSON valido. |
| `InvalidFields` | Falta un campo, el tipo/formato es incorrecto, existe duplicidad o el rango es imposible. |
| `InvalidSignature` | Base64/firma invalida o no corresponde a ninguna llave. |
| `HardwareIdUnavailable` | No fue posible obtener un UUID local canonico. |
| `HardwareMismatch` | UUID local y firmado son distintos. |
| `NotYetValid` | Fecha del servidor anterior al inicio inclusivo. |
| `Expired` | Fecha del servidor posterior al fin inclusivo. |
| `ServerTimeUnavailable` | El repositorio no pudo entregar fecha del servidor. |

## LicenseValidationResult

Valor inmutable compartido entre Business y consumidores.

| Field | Type | Rules |
|-------|------|-------|
| `Status` | `LicenseValidationStatus` | Obligatorio; `IsValid` solo si es `Valid`. |
| `Message` | `string` | Texto controlado no vacio asociado al estado. |
| `Issuer` | `LicenseIssuer` | Distinto de `None` desde firma valida; `Valid` siempre tiene emisor. |
| `ValidFrom` | `DateTime?` | Limite canonico `00:00:00`; ausente antes de parsear. |
| `ValidUntil` | `DateTime?` | Limite canonico `23:59:59`; ausente antes de parsear. |
| `ServerTime` | `DateTime?` | Solo cuando se obtuvo; no usa reloj local. |

Invariants:

- No contiene UUID, firma, contenido, ruta, configuracion ni excepcion.
- `ValidFrom <= ValidUntil` cuando ambos existen.
- `Valid` exige emisor, fechas y tiempo de servidor presentes, y tiempo dentro del rango inclusivo.
- Estados anteriores a firma usan `Issuer.None`; un fallo posterior puede conservar el emisor autenticado.
- Colecciones no aplican; el valor no expone mutabilidad.

## ParsedLicenseDocument (interno de Business)

Representacion transitoria que nunca sale del servicio.

| Field | Type | Rules |
|-------|------|-------|
| `HardwareId` | `string` | UUID canonico en mayusculas y con guiones. |
| `ValidFrom` | `DateTime` | Fecha real canonizada al inicio del dia. |
| `ValidUntil` | `DateTime` | Fecha real canonizada al final del dia. |
| `Signature` | `byte[]` | Base64 valido; copia privada. |
| `SignedPayload` | `byte[]` | UTF-8 exacto; uso exclusivo durante verificacion. |

Lifecycle: entrada -> fuente leida -> documento parseado -> emisor autenticado -> UUID coincidente -> tiempo servidor -> resultado final. Cada fallo termina el flujo; cancelacion no crea resultado.

## LicenseSourceReadResult (contrato Business/Data)

Resultado interno de cargar la entrada sin exponer la fuente al consumidor final.

| Field | Type | Rules |
|-------|------|-------|
| `Succeeded` | `bool` | Verdadero solo con contenido disponible. |
| `Content` | `string?` | Solo para Business; nunca se registra. |
| `FailureKind` | enum interno | `None`, `NotFound`, `Unreadable`, `TooLarge`. |

Un JSON directo no usa Data. Una ruta se lee una vez, en UTF-8 estricto y con limite de tamano documentado en el contrato.
