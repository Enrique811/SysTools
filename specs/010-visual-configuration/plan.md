# Implementation Plan: Configuración visual del sistema

**Branch**: `[010-visual-configuration]` | **Date**: 2026-09-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/010-visual-configuration/spec.md`

## Summary

Agregar un diálogo modal WPF para configuración inicial y edición general sin entregar secretos a Presentation. Un `IConfigurationEditorWorkflow` transitorio mantendrá la configuración completa y confirmaciones privadas por sesión, coordinará carga, prueba Firebird, licencia, UUID y catálogos, y expondrá snapshots/resultados seguros. La ventana se abrirá automáticamente después de mostrar la shell o manualmente desde administración y el verificador; un guardado exitoso volverá a preparar el módulo. Data enumerará impresoras/plantillas y endurecerá recuperación/logging sin generar ni imprimir reportes.

## Technical Context

**Language/Version**: C# / .NET 10 LTS

**Primary Dependencies**: WPF, Microsoft.Extensions.DependencyInjection/Logging, Serilog, FirebirdSql.Data.FirebirdClient, DPAPI, servicios existentes de configuración/licencia; System.Drawing.Printing para solo enumerar nombres

**Storage**: JSON atómico protegido bajo AppData; archivos `.lic`; catálogo bajo `Reports/Templates`

**Testing**: xUnit, dobles deterministas, pruebas WPF STA, integración Firebird/licencia opt-in y quickstart manual

**Target Platform**: Windows Desktop x64, mínimo 1280x720 y 125% DPI

**Project Type**: Aplicación desktop de 3 capas + Entities

**Performance Goals**: carga/catálogos normalmente <1 s; conexión limitada por timeout existente de 5 s; UI receptiva; 100 operaciones sin duplicados

**Constraints**: password almacenado jamás cruza a Presentation; prueba ligada al draft exacto; cero paths/licencia/UUID/password en logs; no impresión/report engine; recuperación explícita con respaldo

**Scale/Scope**: un diálogo/sesión, 11 campos existentes, 4 secciones, 3 catálogos, 8 estados de conexión y 11 de licencia

## Constitution Check

| Gate | Estado | Evidencia de diseño |
|---|---|---|
| 3 capas + Entities | PASS | Presentation consume workflow/puertos; Data conserva archivos, DPAPI, impresoras y plantillas. |
| WPF + MVVM | PASS | ViewModel coordina estado/comandos; code-behind solo password, foco y puentes visuales. |
| Migración incremental | PASS | Feature acotada a configuración visual; reportes/impresión siguen fuera. |
| Compatibilidad/mejora | PASS | Conserva campos/opciones y agrega prueba exacta, recuperación y errores controlados. |
| Seguridad/logging | PASS | Baseline secreto privado, snapshots seguros y logging sin excepciones/rutas. |
| Validación proporcional | PASS | Matrices 20/50/100, STA, seguridad e integración controlada. |

**Post-design re-check**: PASS. Los contratos separan secretos, procesos, archivos y catálogos de UI. Enumerar impresoras no imprime. Listar `.frx`/`.jasper` no adopta motor. Recuperar un archivo inválido requiere confirmación y respaldo.

## Project Structure

```text
specs/010-visual-configuration/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/configuration-editor-contract.md
└── tasks.md

src/
├── Entities/ConfigurationEditor/
├── Business/ConfigurationEditor/
├── Data/ConfigurationEditor/
├── Data/Configuration/
└── Presentation/Modules/Configuration/

tests/
├── SysTools.Configuration.Tests/ConfigurationEditor/
├── SysTools.Firebird.Tests/Integration/
├── SysTools.Licensing.Tests/
└── SysTools.Presentation.Tests/VisualConfiguration/
```

**Structure Decision**: Se conservan proyectos. Entities define drafts/resultados; Business baseline/secretos/tokens; Data catálogos/recuperación; Presentation diálogo/adapters/coordinación. No se agrega proyecto ni motor de reportes.

## Complexity Tracking

No hay violaciones que justificar.
