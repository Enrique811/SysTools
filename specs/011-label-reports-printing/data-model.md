# Data Model: Reportes, etiquetas e impresión

## LabelRowContext

- `Environment`: Preview o Production.
- `Capacity`: 1..3.
- `TemplateId`: ID administrado sin ruta.
- `PrinterId`: nombre exacto; requerido solo en Production.
- `Information`: snapshot adicional.
- Inmutable desde primera captura hasta completar/cancelar.

## PreparedLabel

- `Position`: 1..Capacity, contigua.
- `Data`: `LabelData` existente.
- `Barcode`: valor normalizado, tipo y PNG defensivo exitoso.

## PreparedLabelRow

- `OperationId`: GUID opaco.
- `Context`: snapshot de fila.
- `ServerDate`: fecha Firebird única.
- `Labels`: read-only, exactamente Capacity.
- Estado: `Ready -> Producing -> AwaitingPreview | RetryableFailure | Completed | OutcomeUnknown | Discarded`.

## LabelTemplateDescriptor

- `Id`, `Version`, `Capacity`, `CellWidthMm`, `PageHeightMm`, `Dpi`.
- `Sha256` y `ProductionApproved`.
- Nombre simple `.label.json`, medidas positivas, capacity 1..3 y DPI administrado.

## LabelPreviewDocument

- `OperationId`, dimensiones px/mm y `PngBytes` defensivo.
- Bytes 1..10 MiB y dimensiones positivas; sin ruta ni tipo WPF/impresora.

## LabelOutputResult

- Status: `Pending`, `PreviewReady`, `Printed`, `Busy`, `InvalidProduct`, `ConfigurationUnavailable`, `TemplateMissing`, `TemplateIncompatible`, `TemplateNotApproved`, `BarcodeFailed`, `RenderFailed`, `PrinterUnavailable`, `PrintRejected`, `OutcomeUnknown`, `Canceled`, `Stale`.
- Mensaje seguro, progreso, `CanRetry` y documento solo para PreviewReady.

## State transitions

```text
Empty -> Collecting -> Ready -> Producing
Producing -> AwaitingPreview -> Completed
Producing -> Printed/Completed
Producing -> RetryableFailure -> Producing | Discarded
Producing -> OutcomeUnknown -> Discarded
Collecting -> Canceled/Empty
Any nonterminal -> Stale on shutdown
```
