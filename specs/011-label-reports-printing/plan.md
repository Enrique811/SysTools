# Implementation Plan: Reportes, etiquetas e impresión

**Branch**: `[011-label-reports-printing]` | **Date**: 2026-09-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/011-label-reports-printing/spec.md`

## Summary

Integrar la cola existente con un workflow singleton que congela configuración por fila, genera barcodes existentes y coordina render, preview o impresión exacta con retry explícito. FastReport WPF se evaluó primero pero no se adopta: la edición pública es demo y WPF productivo requiere licencia comercial no autorizada. Se implementará un adaptador nativo Windows limitado a layouts administrados 1/2/3, con documentos raster efímeros a DPI fijo. Cada plantilla tendrá manifest, hash, medidas, capacidad y aprobación productiva; impresión real permanece opt-in hasta comparación física.

## Technical Context

**Language/Version**: C# / .NET 10 LTS

**Primary Dependencies**: WPF, Microsoft.Extensions.DependencyInjection/Logging, Serilog, System.Drawing/Printing ya disponible, ZXing y servicios existentes; sin motor comercial nuevo

**Storage**: manifest y plantillas JSON administradas en `Reports/Templates`; documentos solo en memoria; configuración JSON existente en AppData

**Testing**: xUnit, STA WPF, dobles de renderer/preview/spool, matrices deterministas, decodificación ZXing, integración física opt-in

**Target Platform**: Windows Desktop x64, 1280x720 o superior, 125% soportado

**Project Type**: aplicación WPF en solución 3 capas + Entities

**Performance Goals**: captura pendiente <100 ms; documento 1/2/3 listo en <2 s p95; UI responsiva; máximo un output por operación

**Constraints**: 1/2/3 columnas; `a` preview y cero spool; `b` impresión directa exacta; sin fallback; documento <=10 MiB; 300 DPI; sin reloj local; sin persistir payload; no auto-retry tras submit indeterminado

**Scale/Scope**: tres layouts iniciales de 57x40 mm por celda, una página/fila, hasta tres etiquetas y una salida concurrente

## Constitution Check

*GATE: PASS antes y después de diseño.*

- **3 capas + Entities**: PASS. Entities DTOs inmutables; Business workflow/puertos; Data manifest/render/printing; Presentation preview neutral.
- **WPF + MVVM**: PASS. El ViewModel no abre templates, genera reportes ni usa APIs de impresora.
- **Migración incremental**: PASS. Feature 011 posterior a configuración visual y reutiliza 005/007/008/010.
- **Compatibilidad y mejora**: PASS condicionado. Conserva 1/2/3, ambientes y campos; añade manifest, printer exacta, deduplicación y resultados tipados. Fidelidad física sigue como gate opt-in.
- **Seguridad/logging**: PASS. Documentos efímeros, IDs opacos y logs solo Stage/Status/Duration/OperationId.
- **Motor de reportes**: PASS. FastReport fue evaluado primero. OpenSource falla preview/print local; Demo no es productivo; comercial requiere licencia no autorizada. Fallback nativo documentado y sustituible.
- **Fecha**: PASS. `FECHA` usa `IServerClockRepository`, nunca reloj local.
- **Validación proporcional**: PASS. Matrices 100/50/20, render/barcode, STA, seguridad y protocolo físico opt-in.

## Project Structure

### Documentation (this feature)

```text
specs/011-label-reports-printing/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/label-output-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── Entities/Labels/
├── Business/Labels/
├── Data/Reports/
├── Reports/Templates/
└── Presentation/Modules/{Labels,PriceVerifier}/

tests/
├── SysTools.BusinessRules.Tests/Labels/
├── SysTools.Configuration.Tests/Reports/
└── SysTools.Presentation.Tests/VisualLabels/
```

**Structure Decision**: Mantener las unidades existentes. Data pasa a target Windows Desktop porque impresión/render son infraestructura Windows; no se crea cuarta capa ni se filtran tipos Windows a contratos.

## Complexity Tracking

No hay violaciones constitucionales. El renderer nativo es una alternativa documentada, no una nueva capa.
