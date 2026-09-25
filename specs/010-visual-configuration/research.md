# Research: Configuración visual del sistema

## Frontera segura

**Decision**: `IConfigurationEditorWorkflow` transitorio mantiene `AppConfiguration` completa como baseline privado y devuelve snapshots sin password ni ruta de licencia.

**Rationale**: `LoadAsync` contiene password descifrado y guardar vacío lo eliminaría. La fachada conserva secreto sin cruzarlo al ViewModel.

**Alternatives considered**: Servicios individuales en ViewModel; entidad visual en dominio; guardar vacío.

## Password y prueba exacta

**Decision**: `PasswordBox` abre vacío. Vacío exacto conserva baseline; sin secreto exige uno nuevo. Un token opaco queda ligado a epoch, revisión y snapshot ordinal exacto de servidor/ruta/usuario/password. Todo cambio invalida aunque se revierta.

**Rationale**: Evita mostrar secreto y autorizar otro conjunto. WPF advierte que leer `Password` crea texto plano; se limita al salto visual-workflow. [Microsoft PasswordBox](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.passwordbox.password?view=windowsdesktop-10.0)

**Alternatives considered**: Propiedad pública/binding adjunto; `SecureString` extremo a extremo cuando dependencias requieren string.

## Superficie y apertura

**Decision**: Un diálogo modal transitorio sirve para apertura inicial/manual después de mostrar shell, con owner, guardia única y retorno `Saved/Canceled/Unavailable`. Administración y verificador usan el mismo servicio; Saved causa una re-preparación.

**Rationale**: Conserva módulo activo y reutiliza patrón modal de 009.

## Licencia y UUID

**Decision**: UI elige `.lic`; Business valida y retorna estado/emisor/vigencia. Cambio `KeepExisting/ReplaceValidated/ClearExplicitly`; inválido no reemplaza. UUID pasa por workflow y adapter de portapapeles sin logging.

## Catálogos

**Decision**: `IPrinterCatalog`/`ITemplateCatalog` en Business con Data. Impresoras solo por nombre. Plantillas como IDs relativos bajo `Reports/Templates`, `.frx`/`.jasper`, sin adoptar motor. Valor ausente se conserva como `Unavailable`.

**Rationale**: `InstalledPrinters` devuelve nombres instalados y se aísla como fallo recuperable. [Microsoft InstalledPrinters](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.printing.printersettings.installedprinters?view=windowsdesktop-10.0)

**Alternatives considered**: `System.Printing`; enumeración en UI; paths absolutos; FastReport prematuro.

## Recuperación

**Decision**: Operación explícita/confirmada respalda documento inválido dentro del directorio administrado y escribe atómicamente uno completo. Nunca ocurre en carga/guardado normal.

**Rationale**: JSON inválido y secreto irrecuperable bloquean baseline; sin recovery el operador queda atrapado.

## Concurrencia y logging

**Decision**: Single-flight, CTS/generation visual y epoch privado. Solo revisión vigente publica/crea proof. Logs: Stage, Status/Outcome, Category, DurationMs e IssueCodes/Fields; repositorio deja de adjuntar excepciones con paths.

## Validación

**Decision**: Matrices 20 iniciales, 50 ediciones preservando password y 100 operaciones; STA, sentinels, bytes, guards e integración externa opt-in.
