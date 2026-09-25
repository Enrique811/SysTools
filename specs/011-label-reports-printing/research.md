# Research: Reportes, etiquetas e impresión

## Decision 1: FastReport no se adopta en esta iteración

**Decision**: Mantener FastReport WPF 2026.2.x como candidato futuro y seleccionar ahora un renderer nativo Windows para tres layouts fijos.

**Rationale**: FastReport comercial declara .NET 10, preview WPF e impresión, pero `FastReport.WPF.Demo` altera contenido y no es productivo; la edición WPF requiere compra y confirmación de EULA/CI. FastReport OpenSource no ofrece preview embebido ni impresión local/network. No existe autorización de compra ni `.frx` aprobados. Windows proporciona rutas nativas y el alcance solo necesita tres layouts controlados.

**Alternatives considered**: FastReport OpenSource/comercial, DevExpress, Stimulsoft, Jasper externo y PDF-first.

Fuentes: [FastReport comparison](https://github.com/FastReports/FastReport.Documentation/blob/master/COMPARISON.md), [FastReport .NET/WPF](https://www.fast-report.com/products/fast-report-net), [WPF printing](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/documents/printing-overview).

## Decision 2: Documento neutral raster a 300 DPI

**Decision**: Renderizar una página PNG efímera con medidas físicas, 300 DPI y copia defensiva de bytes; Presentation la visualiza y Data la imprime a tamaño exacto.

**Rationale**: Evita tipos WPF/printing fuera de infraestructura, permite goldens y decodificar el barcode final, y funciona con drivers GDI. El máximo de tres celdas 57x40 mm queda bajo 10 MiB.

**Alternatives considered**: `FixedDocument` público, XPS bytes y primitivas vectoriales en contratos.

## Decision 3: Plantillas administradas

**Decision**: Tres `.label.json` definen ID, capacidad, tamaño, DPI, versión, hash y aprobación productiva. El layout usa regiones fijas de código, descripción, presentación, precio, fecha, información y barcode.

**Rationale**: Sin scripts/conexiones/referencias externas; permite containment, compatibilidad y distribución verificable. `.jasper` queda como referencia; `.frx` solo sería válido con FastReport futuro.

## Decision 4: Snapshot y retry seguro

**Decision**: Al iniciar fila se congelan ambiente, columnas, template, printer e información. La fila completa conserva ID opaco hasta preview, spool aceptado o descarte. Fallos pre-submit admiten retry manual; `Accepted`/`OutcomeUnknown` nunca auto-retry.

**Rationale**: Evita pérdida al vaciar la cola, mezcla de configuración y duplicación física.

## Decision 5: Fecha del servidor

**Decision**: `FECHA` proviene de `IServerClockRepository` al completar la fila y es igual para todas sus posiciones.

**Rationale**: Es campo heredado y no debe usar reloj local.

## Decision 6: Printer exacta y gate físico

**Decision**: Revalidar nombre exacto justo antes de imprimir, usar controlador silencioso y nunca default. `productionApproved=false` bloquea impresión. Aprobar exige equipo no productivo, medidas y escaneo.

**Rationale**: Evita destinos incorrectos y fidelidad simulada. Aceptación de spool no prueba salida física.

## Decision 7: Validación

**Decision**: 100 filas/capacidad; 50 duplicados/ambiente; 20 cancelaciones; matriz tipada; barcode desde render final; goldens; STA 1280x720/125%; auditoría de 100 operaciones; impresión física opt-in.

**Rationale**: Separa lógica, raster, UX y hardware; un SKIP externo nunca se convierte en PASS simulado.
