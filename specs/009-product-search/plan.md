# Implementation Plan: Búsqueda de productos por descripción

**Branch**: `[009-product-search]` | **Date**: 2026-09-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/009-product-search/spec.md`

## Summary

Habilitar el control Buscar del verificador para abrir una ventana modal accesible, consultar productos por prefijo literal de descripción y devolver únicamente el código seleccionado al flujo exacto ya implementado. El workflow de Business conserva la configuración privada de la sesión, devuelve resultados de búsqueda seguros con precios formateados e invalida la disponibilidad ante fallos de catálogo. Presentation usa un ViewModel transitorio y un servicio de diálogo para aislar WPF, single-flight, cancelación y selección mediante teclado o doble clic.

## Technical Context

**Language/Version**: C# 14 sobre .NET SDK 10.0.100 / .NET 10 LTS

**Primary Dependencies**: WPF; Microsoft.Extensions.DependencyInjection, Hosting y Logging 10.0.12; Serilog 4.4.0; servicios existentes de productos, formato de precio y workflow del verificador. No se agrega paquete.

**Storage**: Firebird existente mediante `IProductService`/`IProductRepository`; la configuración validada permanece exclusivamente en la sesión privada del workflow y no cruza a Presentation.

**Testing**: xUnit 2.9.3 y Microsoft.NET.Test.Sdk 17.14.1; matrices Business, pruebas de repositorio, 100 búsquedas, carreras/cancelación, seguridad, DI y WPF STA serializado.

**Target Platform**: Windows Desktop; `net10.0` en Entities/Business/Data y `net10.0-windows` con WPF en Presentation/pruebas visuales.

**Project Type**: Aplicación WPF multiproyecto con arquitectura de 3 capas + Entities y shell modular.

**Performance Goals**: Interfaz receptiva durante búsquedas de hasta 6 segundos; una consulta por confirmación; 100 búsquedas secuenciales sin duplicados ni resultados cruzados.

**Constraints**: Prefijo literal insensible a mayúsculas; máximo 255 caracteres; orden de repositorio preservado; ventana modal única; Enter contextual, Escape y Ctrl+F7; no exponer configuración; logs sin criterio/producto/código; selección reutiliza `LookupAsync`; 1280x720 mínimo y 125% DPI.

**Scale/Scope**: Una ventana de búsqueda activa, una operación vigente, listas de productos del catálogo existente, matriz mínima de 50 prefijos y 100 búsquedas; sin configuración visual, etiquetas, reportes o impresión.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **3 capas + Entities**: PASS. Data conserva la consulta Firebird, Business coordina sesión/búsqueda/formato y Presentation consume resultados seguros.
- **WPF + MVVM**: PASS. ViewModels exponen estado/comandos; el servicio de diálogo encapsula la ventana; code-behind se limita a foco y doble clic visual.
- **Migración incremental**: PASS. Feature 009 corresponde a búsqueda después de la UI operativa 008 y antes de configuración visual/reportes.
- **Compatibilidad y mejora**: PASS. Conserva búsqueda por prefijo y selección por Enter/doble clic; mejora cancelación, single-flight, privacidad y accesibilidad.
- **Stack obligatorio**: PASS. Mantiene C#/.NET 10, WPF, MVVM, Repository, DI y Serilog.
- **Seguridad/logging**: PASS. No expone `AppConfiguration`; registra solo etapa, estado/categoría y duración.
- **Validación proporcional**: PASS condicionado a matrices de prefijos/casing, caracteres especiales, 100 búsquedas, carreras, WPF STA, seguridad, integración controlada y recorrido manual.
- **Post-design**: PASS. Los contratos y estados mantienen las fronteras; el quickstart cubre capas, selección y cierre seguro sin ampliar alcance.

## Project Structure

### Documentation (this feature)

```text
specs/009-product-search/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- product-search-contract.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md
```

### Source Code (repository root)

```text
src/
|-- Entities/PriceVerifier/
|   |-- PriceVerifierSearchStatus.cs
|   |-- PriceVerifierSearchItem.cs
|   `-- PriceVerifierSearchResult.cs
|-- Business/PriceVerifier/
|   |-- IPriceVerifierWorkflow.cs
|   `-- PriceVerifierWorkflow.cs
|-- Data/Repositories/
|   `-- FirebirdProductRepository.cs
`-- Presentation/Modules/PriceVerifier/
    |-- Search/
    |   |-- IProductSearchDialogService.cs
    |   |-- ProductSearchDialogResult.cs
    |   |-- ProductSearchDialogService.cs
    |   |-- ProductSearchViewModel.cs
    |   |-- ProductSearchWindow.xaml
    |   `-- ProductSearchWindow.xaml.cs
    |-- ViewModels/PriceVerifierViewModel.cs
    `-- Views/PriceVerifierView.xaml

tests/
|-- SysTools.BusinessRules.Tests/PriceVerifier/
|   |-- PriceVerifierSearchResultTests.cs
|   |-- PriceVerifierSearchTests.cs
|   |-- PriceVerifierSearchReliabilityTests.cs
|   `-- PriceVerifierSearchSecurityTests.cs
|-- SysTools.Repositories.Tests/Data/
|   `-- FirebirdProductRepositorySearchTests.cs
`-- SysTools.Presentation.Tests/ProductSearch/
    |-- ProductSearchViewModelTests.cs
    |-- ProductSearchSelectionTests.cs
    |-- ProductSearchLifecycleTests.cs
    |-- ProductSearchReliabilityTests.cs
    |-- ProductSearchSecurityTests.cs
    `-- ProductSearchAccessibilityTests.cs
```

**Structure Decision**: Mantener los proyectos actuales. Extender el workflow de 008 evita duplicar preparación/licencia o exponer configuración. Los resultados seguros viven en Entities; Data cambia únicamente la semántica del prefijo de `LIKE` con comodines a `STARTING WITH` literal. El diálogo y su servicio permanecen en Presentation.

## Complexity Tracking

No hay violaciones constitucionales que requieran excepción.
