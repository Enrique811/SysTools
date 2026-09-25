# Data Model: Integracion operativa del verificador de precios

## PriceVerifierPreparationStatus

Estado excluyente de la preparacion de sesion.

| Value | Meaning |
|---|---|
| `Ready` | Configuracion valida, conexion exitosa y licencia valida; existe sesion privada utilizable. |
| `ConfigurationUnavailable` | No pudo cargarse o no esta lista para conexion. |
| `ConnectionUnavailable` | La comprobacion de conexion no fue exitosa. |
| `LicenseUnavailable` | La conexion funciona, pero la licencia no habilita operacion. |

La cancelacion solicitada no es un estado: se propaga y la capa visual ignora continuaciones del lifecycle cancelado.

## PriceVerifierPreparationResult

Resultado inmutable y seguro para Presentation.

| Field | Type | Rules |
|---|---|---|
| `Status` | `PriceVerifierPreparationStatus` | Obligatorio y definido. |
| `Message` | `string` | No vacio, estable, amigable y sin configuracion/detalle tecnico. |
| `ConnectionStatus` | `ConnectionTestStatus?` | `Success` en Ready/LicenseUnavailable; null si no se intento conexion. |
| `LicenseStatus` | `LicenseValidationStatus?` | `Valid` solo en Ready; null si no se intento licencia. |
| `AdditionalInformation` | `string` | Solo desde configuracion cargada; nunca incluye secretos. |
| `IsReady` | computed `bool` | True solo cuando `Status == Ready`. |

### Invariants

- `Ready` exige conexion `Success` y licencia `Valid`.
- `ConfigurationUnavailable` no contiene estados posteriores.
- `ConnectionUnavailable` contiene conexion distinta de `Success` y licencia null.
- `LicenseUnavailable` contiene conexion `Success` y licencia distinta de `Valid`.
- Nunca contiene `AppConfiguration`, password, host, ruta, usuario ni texto de licencia.

## PriceVerifierLookupStatus

| Value | Meaning | Blocks session |
|---|---|---|
| `Success` | Producto exacto encontrado y precio preparado cuando existe. | No |
| `MissingInput` | Codigo vacio tras normalizar extremos. | No |
| `InputTooLong` | Codigo normalizado mayor de 50 caracteres. | No |
| `NotFound` | Consulta valida sin producto. | No |
| `OperationalFailure` | Sesion no lista o fallo recuperable de acceso al catalogo. | Yes |

Una confirmacion recibida durante busy no crea resultado ni transicion: el comando la ignora antes de llamar al workflow.

## PriceVerifierLookupResult

| Field | Type | Rules |
|---|---|---|
| `Status` | `PriceVerifierLookupStatus` | Obligatorio y definido. |
| `Message` | `string` | Mensaje seguro y no vacio. |
| `Product` | `Product?` | Presente solo en Success. |
| `FormattedPrice` | `string?` | Presente en Success solo cuando `Product.PriceWithTax` tiene valor. |
| `IsSuccess` | computed `bool` | True solo para Success. |

### Invariants

- Success exige Product y conserva exactamente su barcode textual.
- Estados no exitosos no contienen Product ni precio.
- El workflow invoca el formatter solo cuando existe precio.
- MissingInput/InputTooLong/NotFound conservan la sesion Ready.
- OperationalFailure invalida la configuracion privada y exige Prepare/Retry.

## Private workflow session

Estado interno de Business; no es Entity publica ni se persiste.

| Field | Purpose |
|---|---|
| `AppConfiguration? configuration` | Configuracion completa validada, confinada al workflow. |
| `long epoch` | Identifica activacion/preparacion vigente. |
| `bool isReady` | Habilita lookup solo despues de las tres comprobaciones. |

### State transitions

```text
NoSession
  -> Preparing
  -> Ready                         (config + connection + license valid)
  -> BlockedConfiguration          (load/validation failure)
  -> BlockedConnection             (connection != Success)
  -> BlockedLicense                (license != Valid)

Ready -> Querying
Querying -> Ready                  (Success, MissingInput, InputTooLong, NotFound)
Querying -> BlockedCatalog         (safe catalog failure)

Blocked* -> Preparing             (Retry)
Any -> NoSession                  (Deactivate/Invalidate; cancel + epoch increment)
```

Un resultado cuyo epoch ya no es vigente se descarta y no puede conservar configuracion, invalidar una sesion nueva ni actualizar Presentation.

## Presentation state

Estado transitorio del `PriceVerifierViewModel`; no se persiste.

| Field | Rules |
|---|---|
| `Barcode` | Two-way; texto actual visible. |
| `IsBusy` | True durante Prepare o Lookup. |
| `IsAvailable` | True solo con preparacion Ready y no busy. |
| `ConnectionStatus` / `LicenseStatus` | Valores visibles coherentes derivados de PreparationResult. |
| `OperationalMessage` | Texto + severidad; nunca contiene detalle tecnico. |
| `ProductDescription`, `ProductPresentation`, `StockDisplay`, `FinalPriceDisplay` | Valores del ultimo Success o `—` neutral. |
| `AdditionalInformation` | Informacion permitida de configuracion, de solo lectura. |
| `FocusRequestVersion` | Contador creciente que solicita foco/select-all despues de Ready y de intentos no bloqueantes. |

### Publication rules

- Al iniciar Prepare o Lookup se limpia cualquier resultado comercial que ya no sea vigente.
- Success publica todos los campos en una sola continuacion vigente.
- NotFound e invalid input dejan campos neutrales y solicitan foco.
- OperationalFailure limpia campos, bloquea captura, actualiza estados y habilita Retry.
- Deactivate cancela, invalida epoch y evita cualquier publicacion posterior.

## Existing entities reused

- `AppConfiguration`: solo dentro de Business/Data.
- `ConnectionTestResult` y `ConnectionTestStatus`: evidencia segura de conexion.
- `LicenseValidationResult` y `LicenseValidationStatus`: decision segura de licencia.
- `Product`: entidad inmutable mostrada mediante propiedades del ViewModel.
- `OperationalMessage`, `AvailabilityStatus`: modelos visuales existentes para shell/modulo.
