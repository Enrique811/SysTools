# Contract: Label output workflow

## `ILabelOutputWorkflow`

- `CaptureAsync(Product, formattedPrice, CancellationToken) -> LabelOutputResult`
- `RetryAsync(CancellationToken) -> LabelOutputResult`
- `Cancel() -> LabelCancelResult`
- `GetState() -> LabelWorkflowSnapshot`
- `Invalidate()`

Workflow singleton, single-flight y dueño del snapshot/retry; nunca expone configuración completa.

## Infrastructure ports

- `ILabelTemplateProvider.ResolveAsync(templateId, capacity, token) -> LabelTemplateResult`
- `ILabelDocumentRenderer.RenderAsync(preparedRow, descriptor, token) -> LabelRenderResult`
- `ILabelPrinter.PrintAsync(document, exactPrinterId, operationId, token) -> LabelPrintResult`

Business no conoce rutas, streams, WPF, `System.Drawing`, spooler ni motor. Data nunca hace fallback.

## Presentation contract

- `ILabelPreviewDialogService.ShowAsync(LabelPreviewDocument, token) -> LabelPreviewDialogResult`
- `CloseActive()`

Presentation recibe solo documento neutral; cerrar preview no imprime ni exporta.

## Retry/idempotency

- Solo fallos pre-submit son reintentables con mismo ID/fila.
- Printed, preview confirmada u OutcomeUnknown consumen/bloquean la fila.
- Ningún retry automático; epoch anterior es Stale y no publica UI.

## Logging

Permitidos: Stage, Status, DurationMs, OperationId opaco, Capacity y enums. Prohibidos: producto/barcode/precio/info, imágenes/documentos, configuración, printer, rutas, UUID, password/licencia y excepción adjunta.
