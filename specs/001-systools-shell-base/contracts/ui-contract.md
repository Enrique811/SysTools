# UI Contract: SysTools Shell and Price Verifier Placeholder

Superficie observable de la primera pantalla, comprobable desde UI y bindings; no es un API externo.

## Window contract

| Element | Contract |
|---|---|
| Main window | Primera y única ventana; título `SysTools`; mínimo 1280 × 720. |
| Initial focus | Ningún campo de captura deshabilitado recibe foco; la ventana conserva un inicio sin acción operativa. |
| Root layout | Sidebar oscuro y área clara flexible; sin landing ni diálogo previo. |
| Resize | Hasta pantalla completa no se superponen sidebar, encabezado, tarjetas, precio o estado. |

## Navigation contract

| Visible item | Section | Enabled | Selected initially | Result |
|---|---|---:|---:|---|
| Verificador de precios | Utilerías | Yes | Yes | Muestra PriceVerifierView. |
| Impresión de etiquetas | Utilerías | No | No | Sin interacción/cambio. |
| Inventario rápido | Utilerías | No | No | Sin interacción/cambio. |
| Nueva utilería | Utilerías | No | No | Sin interacción/cambio. |
| Configuración | Administración | No | No | Sin interacción/cambio. |
| Licencias | Administración | No | No | Sin interacción/cambio. |
| Logs del sistema | Administración | No | No | Sin interacción/cambio. |

- Selección activa: fondo azul y texto de alto contraste.
- Entradas deshabilitadas: legibles, atenuadas, sin foco ni comando.
- Agregar un módulo posterior requiere registrar item/contenido y DataTemplate, no editar PriceVerifierView.

## Price Verifier view contract

| Region | Required content | Binding / behavior |
|---|---|---|
| Header | Título, descripción, Buscar, Imprimir, Configuración | Los tres botones visibles y deshabilitados. |
| Barcode | Etiqueta, campo y ayuda | Visible y deshabilitado; sin foco, escritura, envío ni consulta. |
| Additional information | Etiqueta y campo visual | Visible y deshabilitado; sin foco, escritura ni persistencia. |
| Product details | Descripción, Presentación, Existencia | Valores iniciales `—`. |
| Final price | Etiqueta y valor destacado | `—`, nunca `$0` simulado. |
| Shortcuts | Referencia opcional del mockup | No disponibles; teclas sin acción. |
| Status area | Estado/mensaje | Severidad no dependiente solo del color. |

## Global status contract

| State | Initial visible text | Semantic state |
|---|---|---|
| Connection | `Conexión: No disponible` | Unavailable |
| License | `Licencia: No disponible` | Unavailable |
| Message | `Módulo en preparación` | Information |

No se muestran estados de conexión/licencia exitosos o inválidos sin validación real.

## Visual token contract

| Token | Value / intention |
|---|---|
| Sidebar background | `#111827` |
| Sidebar secondary | `#1F2937` |
| Main background | `#F3F4F6` |
| Card background | `#FFFFFF` |
| Card border | `#D7DDE5` |
| Action accent | `#2563EB` |
| Primary text | `#111827` |
| Secondary text | `#6B7280` |
| Font | Segoe UI con fallback de sistema |

Los recursos usan claves semánticas; los estados siempre incluyen texto o etiqueta accesible.

## Binding and code-behind contract

- Los bindings de producción no producen errores de diagnóstico.
- Views/code-behind no crean repositorios, servicios, loggers ni módulos.
- Code-behind se limita a `InitializeComponent` y comportamiento estrictamente visual no declarativo, documentando excepciones.
- Comandos/estado residen en ViewModels; controles deshabilitados no requieren comandos ficticios.

## Failure contract

| Failure | User-facing result | Diagnostic result |
|---|---|---|
| Sin módulo activo al iniciar | Recupera Verificador. | Warning estructurado. |
| Error recuperable de activación | Shell abierta y mensaje amigable. | Error con excepción y ModuleId. |
| Error fatal de arranque | Mensaje genérico previo al cierre cuando sea posible. | Fatal con excepción y flush. |

Los mensajes visibles no incluyen stack traces, rutas, clases internas ni información sensible.
