# Data Model: Configuración visual

## ConfigurationEditorSnapshot

Snapshot seguro: `SessionId`, `ConfigurationDraft`, `HasStoredPassword`, `HasStoredLicense`, `RequiresConnectionTest`, `LoadStatus`, issues, opciones read-only y hardware. Nunca password, licencia o `AppConfiguration`.

## ConfigurationDraft

Campos no sensibles: `IpEmpresa`, `RutaEmpresa`, `Usuario`, `Ambiente`, `Impresora`, `FormatoPrecio`, `Reporte`, `Columnas`, `Informacion`. Normaliza null, conserva contenido interior y aplica catálogos cerrados.

## ConnectionProof

Privado: token aleatorio, epoch, revisión y snapshot exacto de servidor, ruta, usuario y password efectivo. Solo `Success` lo crea; cambio/cierre/reapertura lo elimina.

## LicenseCandidate / LicenseSummary

Candidate privado conserva path y `KeepExisting`, `ReplaceValidated` o `ClearExplicitly`. Summary público contiene Status, Issuer, ValidFrom, ValidUntil y mensaje seguro. Solo validación válida ligada al epoch/conexión reemplaza.

## OperationalOption

`Id`, `DisplayName`, `IsAvailable`, `IsPersistedUnavailable`. Printer ID = nombre exacto. Template ID = relativo sin traversal. Duplicados case-insensitive se eliminan establemente; colección read-only.

## ConfigurationEditorResult

Estados: `Loaded`, `RecoveryRequired`, `TestSucceeded`, `TestFailed`, `LicenseValidated`, `Saved`, `ValidationFailed`, `Canceled`, `OperationalFailure`, `Stale`. Mensaje obligatorio; nunca `AppConfiguration`.

## State transitions

```text
Closed -> Loading -> Editing
Loading -> RecoveryRequired | OperationalFailure
Editing --connection change--> Editing (proof cleared, revision++)
Editing --test success--> ConnectionConfirmed
Editing/ConnectionConfirmed --license valid--> LicenseConfirmed
Editing/ConnectionConfirmed/LicenseConfirmed --save--> Saved -> Closed
Editing --recover confirmed--> Editing (new baseline)
Any open --cancel/close--> Closed (epoch++, CTS canceled)
Any late response -> Stale (not published/not committed)
```

Cambios operativos se guardan sin prueba solo si baseline era connection-ready y conexión efectiva no cambió.
