# Contract: Support workflow

## `ISupportWorkflow`

- `InitializeAsync(token) -> SupportInitializationResult`
- `PrepareLicenseRequestAsync(token) -> SupportActionResult`
- `PrepareErrorReport(summary, detail, token) -> SupportActionResult`
- `OpenDraftAsync(draft, channel, token) -> SupportActionResult`
- `GetLogsAsync(token) -> LogCatalogResult`
- `ReadLogAsync(fileId, token) -> LogPreviewResult`
- `OpenLogFolderAsync(token) -> SupportActionResult`
- `Invalidate()`

Workflow single-flight para aperturas externas, usa epoch para resultados tardíos y no expone configuración o rutas.

## Infrastructure ports

- `ISupportExternalLauncher.OpenMailAsync(draft, channel, token) -> ExternalLaunchResult`
- `ISupportExternalLauncher.OpenLogFolderAsync(token) -> ExternalLaunchResult`
- `ISupportLogStore.GetCatalogAsync(token) -> LogCatalogResult`
- `ISupportLogStore.ReadAsync(fileId, token) -> LogPreviewResult`

Data construye destinos fijos, resuelve el root y captura fallos recuperables. Business no usa filesystem ni procesos.

## Presentation contract

- `SupportViewModel` implementa lifecycle asíncrono, comandos de copia, preparación, apertura, refresco y selección, y estado textual.
- El portapapeles se usa solo por acción explícita; el ViewModel no registra su contenido.

## Logging

Permitidos: operación, tipo, canal, estado, duración y DiagnosticId opaco. Prohibidos: UUID, asunto, cuerpo, detalle, contenido, nombre o ruta de log, configuración, credenciales, licencia y excepción adjunta.

