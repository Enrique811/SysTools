# Contract: Configuration editor

## Business boundary

`IConfigurationEditorWorkflow` transitorio/stateful:

- `OpenAsync(CancellationToken) -> ConfigurationEditorSnapshot`
- `Validate(ConfigurationDraft, bool hasNewPassword) -> ConfigurationEditorValidation`
- `TestConnectionAsync(ConfigurationDraft, string newPassword, long revision, CancellationToken) -> ConnectionEditorResult`
- `ValidateLicenseAsync(ConfigurationDraft, string newPassword, string candidatePath, long revision, CancellationToken) -> LicenseEditorResult`
- `SaveAsync(ConfigurationDraft, string newPassword, long revision, string? proofToken, LicenseChange, CancellationToken) -> ConfigurationEditorSaveResult`
- `RecoverAsync(ConfigurationDraft, string newPassword, bool confirmed, CancellationToken) -> ConfigurationEditorSaveResult`
- `Invalidate()`

Invariantes: ningún retorno expone secreto/config completa/path de licencia; password vacío conserva baseline; proof coincide sesión/epoch/revisión/snapshot; fallo/cancel/stale no persiste; licencia inválida no reemplaza.

## Catalog boundaries

- `IPrinterCatalog.GetAsync(CancellationToken) -> CatalogResult`
- `ITemplateCatalog.GetAsync(CancellationToken) -> CatalogResult`

`CatalogResult`: `Available`, `Empty`, `Unavailable` y opciones read-only. Data no registra nombres/paths ni abre/imprime/carga plantillas.

## Presentation boundary

- `IConfigurationDialogService.ShowDialog(ConfigurationDialogMode, CancellationToken) -> ConfigurationDialogResult`
- `CloseActive()`
- `IConfigurationFilePicker.PickDatabase/PickLicense` devuelve path/cancelación y no lee.
- `IClipboardService.TrySetText` y `IConfirmationService` encapsulan efectos visuales.

Solo `Saved` provoca re-preparación.

## Visual contract

- Owner/CenterOwner/ShowInTaskbar=false, una instancia.
- Cuatro secciones con scroll y barra fija Probar/Guardar/Cancelar.
- PasswordBox vacío, sin binding/AutomationProperties/copia del valor.
- Ctrl+S, Escape, access keys y foco al primer error.
- Cierre dirty confirmado; code-behind solo password/foco/picker/clipboard/cierre.

## Recovery contract

Solo tras `InvalidContent`/`SecretUnavailable`, con confirmación y draft completo/password nuevo. Data respalda dentro de directorio administrado antes de reemplazo atómico. Paths/excepciones no aparecen en UI/log.
