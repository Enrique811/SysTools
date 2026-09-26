# Data Model: Correos, soporte y logs

## SupportMailDraft

- `Kind`: LicenseRequest o ErrorReport.
- `Recipient`: opcional, máximo 254; vacío mientras no exista destinatario aprobado.
- `Subject`: requerido, 1..160 caracteres.
- `Body`: requerido, 1..6000 caracteres.
- `DiagnosticId`: GUID opaco formato `N`.
- Inmutable; no contiene rutas, credenciales, archivos ni configuración.

## SupportChannel

- DefaultClient, GmailWeb, OutlookWeb.

## SupportActionResult

- Estado: Ready, Opened, InvalidInput, HardwareUnavailable, Busy, LaunchFailed, Canceled o Stale.
- Mensaje seguro, `CanRetry` y borrador solo para Ready.

## LogFileSummary

- `Id`: nombre simple `.log`, 1..255 caracteres.
- `LastWriteUtc`: fecha UTC.
- `Length`: 0..50 MiB.
- No expone ruta completa.

## LogCatalogResult

- Estado: Available, Empty, DirectoryUnavailable, AccessDenied, Failed o Canceled.
- Colección read-only de máximo 20 elementos, orden descendente.

## LogPreviewResult

- Estado: Loaded, Missing, InvalidIdentifier, TooLarge, AccessDenied, InvalidContent, Failed o Canceled.
- `FileId`, contenido de máximo 500 líneas y 256 KiB, y `IsTruncated`.

## SupportViewState

```text
Inactive -> Loading -> Ready
Ready -> Preparing -> Ready | Error
Ready -> LoadingLogs -> Ready | Error
Any active -> Invalidated; respuestas anteriores -> Stale
```

