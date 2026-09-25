# Data Model: Búsqueda de productos por descripción

## PriceVerifierSearchStatus

Estado excluyente de una búsqueda.

| Value | Meaning | Blocks session |
|---|---|---|
| `Success` | Existe al menos una coincidencia segura. | No |
| `NoMatches` | El prefijo es válido y no produjo resultados. | No |
| `MissingInput` | El criterio queda vacío tras retirar espacios exteriores. | No |
| `InputTooLong` | El criterio normalizado excede 255 caracteres. | No |
| `OperationalFailure` | No existe sesión Ready o falló el catálogo/formato. | Sí |

La cancelación solicitada se propaga y no se modela como resultado.

## PriceVerifierSearchItem

Proyección inmutable permitida para Presentation.

| Field | Type | Rules |
|---|---|---|
| `Barcode` | `string` | Obligatorio, no vacío, máximo 50; conserva ceros. |
| `Description` | `string` | Puede ser vacío; máximo 255. |
| `FormattedPrice` | `string?` | Solo existe si el producto tiene precio. |
| `Stock` | `string` | Valor del producto o neutral existente. |

No contiene id interno, configuración, credenciales, licencia ni entidad completa.

## PriceVerifierSearchResult

Resultado inmutable y seguro.

| Field | Type | Rules |
|---|---|---|
| `Status` | `PriceVerifierSearchStatus` | Obligatorio y definido. |
| `Message` | `string` | No vacío, estable y seguro. |
| `Items` | `IReadOnlyList<PriceVerifierSearchItem>` | Snapshot de solo lectura. |
| `IsSuccess` | computed `bool` | True solo para Success. |

### Invariants

- Success exige al menos un item.
- Cualquier estado distinto de Success exige lista vacía.
- El orden recibido del repositorio se conserva.
- Nunca contiene el criterio, `Product`, `AppConfiguration` ni detalles técnicos.

## ProductSearchDialogResult

Resultado de Presentation que cruza del diálogo al módulo padre.

| Outcome | Selected barcode | Meaning |
|---|---|---|
| `Selected` | Obligatorio | Cargar mediante consulta exacta. |
| `Canceled` | Null | Conservar producto visible y recuperar foco. |
| `OperationalFailure` | Null | Limpiar/bloquear y ofrecer Retry. |

## ProductSearchViewModel state

| Field | Rules |
|---|---|
| `SearchText` | Two-way; criterio visible sin normalización destructiva. |
| `Items` | Colección sustituida/publicada en una continuación vigente. |
| `SelectedItem` | Null o referencia a un item de Items. |
| `IsBusy` | True únicamente durante SearchAsync. |
| `StatusMessage` | Mensaje textual y accesible, sin criterio ni payload. |
| `ResultsFocusRequestVersion` | Contador para seleccionar/enfocar primera fila tras Success. |
| `SearchFocusRequestVersion` | Contador para foco en apertura, input inválido y NoMatches. |

## State transitions

```text
Closed -> OpenIdle
OpenIdle -> Searching                  (criterio confirmado)
Searching -> Results                  (Success)
Searching -> OpenIdle                 (MissingInput/InputTooLong/NoMatches)
Searching -> ClosedOperationalFailure (OperationalFailure)
Searching -> ClosedCanceled           (close/deactivate)
Results -> Searching                  (nuevo criterio)
Results -> ClosedSelected             (Enter/doble clic válido)
Results -> ClosedCanceled             (Escape/cierre)
```

Una continuación publica únicamente si su generación continúa vigente y la ventana no fue cerrada.

## Existing state reused

- Sesión privada de `PriceVerifierWorkflow`: configuración, readiness y epoch.
- `Product`: fuente interna de Business; no cruza como resultado de búsqueda.
- Estado/publicación de `PriceVerifierViewModel`: consulta exacta, neutralidad, Retry y foco del código.
