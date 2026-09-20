# Data Model: Base de shell modular de SysTools

No hay persistencia ni entidades de negocio. Estos modelos son estado de presentación en memoria y permanecen en `SysTools.Presentation`; Entities queda preparado sin modelos artificiales.

## UtilityModuleItem

| Field | Type | Required | Rules |
|---|---|---:|---|
| Id | string | Yes | Único, no vacío, kebab-case. |
| DisplayName | string | Yes | Texto visible no vacío. |
| Section | ModuleSection | Yes | `Utilities` o `Administration`. |
| IsEnabled | bool | Yes | Solo `price-verifier` es true. |
| IsSelected | bool | Yes | Exactamente un módulo habilitado está seleccionado. |
| Content | ViewModelBase? | No | Obligatorio para el habilitado; null para placeholders. |

### Initial instances

| Id | DisplayName | Section | Enabled | Selected | Content |
|---|---|---|---:|---:|---|
| price-verifier | Verificador de precios | Utilities | Yes | Yes | PriceVerifierViewModel |
| label-printing | Impresión de etiquetas | Utilities | No | No | null |
| quick-inventory | Inventario rápido | Utilities | No | No | null |
| new-utility | Nueva utilería | Utilities | No | No | null |
| settings | Configuración | Administration | No | No | null |
| licensing | Licencias | Administration | No | No | null |
| system-logs | Logs del sistema | Administration | No | No | null |

```text
Disabled ──future feature──> Available ──selection──> Selected
Selected ──other selected──> Available
```

En esta feature no ocurre la primera transición; seleccionar una entrada deshabilitada no cambia el módulo activo.

## ShellViewModel

| Field | Type | Required | Initial value / rule |
|---|---|---:|---|
| ApplicationName | string | Yes | `SysTools`. |
| ApplicationSubtitle | string | Yes | `Suite operativa`. |
| Modules | IReadOnlyList&lt;UtilityModuleItem&gt; | Yes | Siete entradas únicas en orden del mockup. |
| ActiveModule | UtilityModuleItem | Yes | `price-verifier`, habilitado. |
| ActiveModuleContent | ViewModelBase | Yes | Contenido activo, nunca null con shell visible. |
| ConnectionStatus | AvailabilityStatus | Yes | `Unavailable`. |
| LicenseStatus | AvailabilityStatus | Yes | `Unavailable`. |
| StatusMessage | OperationalMessage | Yes | Información de módulo en preparación. |

Invariants: ActiveModule pertenece al catálogo y es el único seleccionado; conexión/licencia no pueden ser Available sin validación real; un error recuperable cambia el mensaje pero conserva la shell.

## PriceVerifierViewModel

| Field | Type | Required | Initial value / rule |
|---|---|---:|---|
| ModuleTitle | string | Yes | `Verificador de precios`. |
| ModuleDescription | string | Yes | Describe estructura inicial sin prometer funcionalidad. |
| Barcode | string | No | Vacío y sin mutación desde UI durante esta feature. |
| AdditionalInformation | string | No | Vacío y sin mutación desde UI durante esta feature. |
| ProductDescription | string | Yes | `—`. |
| ProductPresentation | string | Yes | `—`. |
| StockDisplay | string | Yes | `—`. |
| FinalPriceDisplay | string | Yes | `—`, nunca `$0`. |
| IsBarcodeAvailable | bool | Yes | false. |
| IsAdditionalInformationAvailable | bool | Yes | false. |
| IsSearchAvailable | bool | Yes | false. |
| IsPrintAvailable | bool | Yes | false. |
| IsSettingsAvailable | bool | Yes | false. |

No hay transición a captura ni resultado de producto. Código de barras e información adicional permanecen deshabilitados, no reciben foco y no mutan desde la UI; Enter/accesos rápidos no ejecutan acciones.

## OperationalMessage

| Field | Type | Required | Rules |
|---|---|---:|---|
| Text | string | Yes | Legible, sin stack trace, rutas, tipos internos ni secretos. |
| Severity | MessageSeverity | Yes | Information, Warning o Error. |
| AccessibilityLabel | string | Yes | Resume severidad y contenido. |

## Enumerations

- **ModuleSection**: Utilities, Administration.
- **AvailabilityStatus**: Unavailable, Pending, Available, Error.
- **MessageSeverity**: Information, Warning, Error.

## Relationships

```text
ShellViewModel 1 ─ contains ─ 7 UtilityModuleItem
ShellViewModel 1 ─ active ─── 1 UtilityModuleItem
UtilityModuleItem 0..1 ────── 1 ViewModelBase
price-verifier content ────── 1 PriceVerifierViewModel
ShellViewModel 1 ─ status ─── 1 OperationalMessage
```
