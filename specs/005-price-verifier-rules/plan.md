# Implementation Plan: Reglas de negocio del verificador

**Branch**: `[005-price-verifier-rules]` | **Date**: 2026-09-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-price-verifier-rules/spec.md`

**Note**: This template is filled in by the `$speckit-plan` command; its definition describes the execution workflow.

## Summary

Agregar una capa Business comprobable sin UI que normaliza consultas sobre `IProductRepository`, formatea importes con reglas deterministas `MX`/`CO` y mantiene una cola en memoria de filas de una, dos o tres etiquetas. Los contratos compartidos seran valores inmutables en Entities; Presentation solo recibira interfaces registradas por DI. La implementacion reutiliza los repositorios de la feature 004 y no agrega acceso a datos, persistencia, impresion ni dependencias externas.

## Technical Context

**Language/Version**: C# 14 sobre .NET 10 LTS

**Primary Dependencies**: BCL (`System.Globalization`, colecciones inmutables por copia), `Microsoft.Extensions.DependencyInjection` ya presente en Presentation; sin paquetes nuevos de produccion

**Storage**: N/A; cola efimera en memoria. Las consultas reutilizan `IProductRepository` y `AppConfiguration`, sin modificar Firebird.

**Testing**: xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1 y `dotnet test`

**Target Platform**: Windows Desktop x64/x86/Any CPU, `net10.0` para Business/Entities y `net10.0-windows` para pruebas y Presentation

**Project Type**: Aplicacion WPF en solucion multiproyecto de 3 capas + Entities

**Performance Goals**: Formato y captura de cola por debajo de 5 ms p95 en pruebas locales; ninguna consulta vacia invoca repositorio; 100 ciclos por capacidad sin perdida, duplicacion ni reordenamiento

**Constraints**: Sin WPF, Firebird, archivos, reloj, impresion o codigo de barras en las reglas; redondeo `ToEven`; `MX` invariante con dos decimales; `CO` con cultura explicita `es-CO` y cero decimales; capacidad solo 1..3; cambio de capacidad rechazado mientras haya pendientes; resultados devueltos mediante copias de solo lectura

**Scale/Scope**: Una cola por sesion del modulo, maximo tres pendientes, dos operaciones de consulta, dos formatos monetarios y cuatro historias funcionales; sin cambios de esquema ni UI

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **3 capas + Entities**: PASS. Business consume `IProductRepository`; Entities contiene valores simples; Data no cambia; Presentation solo compone interfaces.
- **WPF + MVVM**: PASS. No se agrega comportamiento visual ni logica a Views/ViewModels.
- **Migracion incremental Spec Kit**: PASS. Es la feature 005 y sigue repositorios de productos antes de licencia, barcode, UI y reportes.
- **Compatibilidad funcional y mejora**: PASS. Conserva `MX`, `CO` y columnas 1/2/3; mejora testabilidad, nulabilidad, inmutabilidad y cancelacion segura.
- **Seguridad, logging y errores**: PASS. No maneja secretos; conserva excepciones seguras catalogadas y no agrega payloads o logs con producto.
- **Validacion minima Business**: PASS condicionado a pruebas unitarias de consultas, culturas, redondeo, nulabilidad, cola y cancelacion antes de considerar completa la feature.
- **Post-design**: PASS. `data-model.md` mantiene Entities sin infraestructura; `contracts/business-rules-contract.md` no permite acceso directo de Presentation a Data y `quickstart.md` valida sin Firebird real.

## Project Structure

### Documentation (this feature)

```text
specs/005-price-verifier-rules/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── business-rules-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── Business/
│   ├── Products/
│   │   ├── IProductService.cs
│   │   └── ProductService.cs
│   └── Labels/
│       ├── IPriceFormatterService.cs
│       ├── PriceFormatterService.cs
│       ├── ILabelQueueService.cs
│       └── LabelQueueService.cs
├── Entities/
│   └── Labels/
│       ├── LabelData.cs
│       ├── PendingLabel.cs
│       └── LabelQueueResult.cs
└── Presentation/
    └── App.xaml.cs

tests/
├── SysTools.BusinessRules.Tests/
│   ├── Products/
│   ├── Pricing/
│   ├── Labels/
│   ├── Architecture/
│   └── TestDoubles/
└── SysTools.Presentation.Tests/
    └── Composition/
```

**Structure Decision**: Mantener los cuatro proyectos existentes. Los contratos de salida compartidos viven en Entities; las interfaces y reglas viven en Business; Presentation registra implementaciones, pero la vista placeholder no se integra todavia. Se agrega un proyecto de pruebas dedicado a reglas puras y una prueba de composicion en el proyecto de Presentation existente. No se modifica Data porque `IProductRepository` ya cubre las consultas requeridas.

## Complexity Tracking

No hay violaciones constitucionales que requieran excepcion.
