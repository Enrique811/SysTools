# Tasks: Reportes, etiquetas e impresión

**Input**: Design documents from `/specs/011-label-reports-printing/`

**Tests**: Impresión y reportes son críticos; cada historia exige pruebas automatizadas y la salida física se conserva opt-in.

## Phase 1: Setup

- [X] T001 Registrar baseline restore/build/test en `specs/011-label-reports-printing/validation/baseline.md`
- [X] T002 [P] Crear dobles de template/renderer/printer/config/clock en `tests/SysTools.BusinessRules.Tests/Labels/LabelOutputDoubles.cs`
- [X] T003 [P] Preparar assets ficticios y directorios de reports en `tests/SysTools.Configuration.Tests/Reports/Assets/`
- [X] T004 Documentar evaluación FastReport/fallback/licencia en `specs/011-label-reports-printing/validation/engine-adoption.md`

## Phase 2: Foundational

- [X] T005 [P] Probar invariantes de contexto, fila preparada, template, documento y resultados en `tests/SysTools.BusinessRules.Tests/Labels/LabelOutputEntityTests.cs`
- [X] T006 Crear entidades inmutables con capacity 1..3, documento <=10 MiB e IDs opacos en `src/Entities/Labels/LabelOutputModels.cs`
- [X] T007 Definir workflow y puertos renderer/template/printer en `src/Business/Labels/ILabelOutputWorkflow.cs` y `src/Business/Labels/LabelOutputContracts.cs`
- [X] T008 [P] Definir contrato neutral del diálogo de preview en `src/Presentation/Modules/Labels/LabelPreviewContracts.cs`
- [X] T009 Cambiar Data a Windows Desktop sin filtrar WPF y confirmar referencias en `src/Data/SysTools.Data.csproj`
- [X] T010 Ejecutar foundation y registrar fronteras en `specs/011-label-reports-printing/validation/foundation.md`

## Phase 3: User Story 1 - Preview seguro (P1) MVP

**Independent Test**: ambiente `a`, filas 1/2/3 producen preview única y cero spool.

- [X] T011 [P] [US1] Probar routing preview, snapshot, fecha servidor y fallos tipados en `tests/SysTools.BusinessRules.Tests/Labels/LabelPreviewWorkflowTests.cs`
- [X] T012 [P] [US1] Probar manifest containment/hash/capacidad/medidas/JSON dañado en `tests/SysTools.Configuration.Tests/Reports/ManagedLabelTemplateProviderTests.cs`
- [X] T013 [P] [US1] Probar render 1/2/3, textos extremos, geometría y PNG válido en `tests/SysTools.Configuration.Tests/Reports/NativeLabelDocumentRendererTests.cs`
- [X] T014 [P] [US1] Decodificar barcode desde cada slot final renderizado en `tests/SysTools.Configuration.Tests/Reports/RenderedBarcodeRoundTripTests.cs`
- [X] T015 [P] [US1] Probar diálogo STA, Escape, zoom, scroll, owner y 1280x720@125% en `tests/SysTools.Presentation.Tests/VisualLabels/LabelPreviewAccessibilityTests.cs`
- [X] T016 [US1] Implementar proveedor seguro `.label.json` y manifest en `src/Data/Reports/ManagedLabelTemplateProvider.cs`
- [X] T017 [US1] Implementar renderer nativo 300 DPI de campos/barcodes en `src/Data/Reports/NativeLabelDocumentRenderer.cs`
- [X] T018 [US1] Implementar workflow snapshot/queue/barcode/server-date/single-flight/retry en `src/Business/Labels/LabelOutputWorkflow.cs`
- [X] T019 [US1] Crear templates administrados 1/2/3 no aprobados para producción en `src/Reports/Templates/`
- [X] T020 [US1] Implementar ventana/VM/servicio modal de preview neutral en `src/Presentation/Modules/Labels/`
- [X] T021 [US1] Registrar copia de templates y DI de preview/render/workflow en `src/Presentation/SysTools.Presentation.csproj` y `src/Presentation/App.xaml.cs`
- [X] T022 [US1] Ejecutar matrices preview/barcode y documentar en `specs/011-label-reports-printing/validation/preview-routing.md`

## Phase 4: User Story 2 - Impresión productiva (P1)

**Independent Test**: ambiente `b` usa printer exacta, una aceptación y cero preview/fallback.

- [X] T023 [P] [US2] Probar exact printer, missing/offline/rejected/accepted/unknown/cancel en `tests/SysTools.Configuration.Tests/Reports/WindowsLabelPrinterTests.cs`
- [X] T024 [P] [US2] Probar routing productivo, aprobación, retry y 50 duplicados en `tests/SysTools.BusinessRules.Tests/Labels/LabelProductionWorkflowTests.cs`
- [X] T025 [P] [US2] Probar 100 filas por capacidad y exactamente una salida en `tests/SysTools.BusinessRules.Tests/Labels/LabelOutputWorkflowTests.cs`
- [X] T026 [US2] Implementar adaptador de spool exacto silencioso sin fallback en `src/Data/Reports/WindowsLabelPrinter.cs`
- [X] T027 [US2] Integrar estados Accepted/Rejected/Unknown y política de consumo/retry en `src/Business/Labels/LabelOutputWorkflow.cs`
- [X] T028 [US2] Crear protocolo opt-in con `TEST / NO VENDER` en `tests/SysTools.Configuration.Tests/Reports/PhysicalPrinterIntegrationTests.cs`
- [X] T029 [US2] Documentar impresión, SKIP/PASS físico y fidelidad heredada en `specs/011-label-reports-printing/validation/physical-printer.md`

## Phase 5: User Story 3 - Operación desde verificador (P2)

**Independent Test**: Ctrl+F8 captura/progresa/saca una fila y Ctrl+F10 cancela pendientes.

- [X] T030 [P] [US3] Probar producto privado, disponibilidad, progreso, retry, cancel y late response en `tests/SysTools.Presentation.Tests/VisualLabels/PriceVerifierLabelOutputTests.cs`
- [X] T031 [P] [US3] Extender accesibilidad real del verificador para Ctrl+F8/Ctrl+F10/foco/texto/busy en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierAccessibilityTests.cs`
- [X] T032 [US3] Integrar producto/precio privados y comandos capture/retry/cancel en `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs`
- [X] T033 [US3] Activar botones, atajos y progreso textual en `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml`
- [X] T034 [US3] Coordinar preview, foco y lifecycle sin publicaciones tardías en `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs`
- [X] T035 [US3] Extender DI/guards de composición para workflow singleton y servicios correctos en `tests/SysTools.Presentation.Tests/Composition/DependencyInjectionTests.cs`
- [X] T036 [US3] Ejecutar recorrido teclado y documentar en `specs/011-label-reports-printing/validation/manual-wpf.md`

## Phase 6: Polish & Cross-Cutting

- [X] T037 [P] Ejecutar auditoría de 100 operaciones/sentinels/logs/payloads en `tests/SysTools.BusinessRules.Tests/Labels/LabelOutputSecurityTests.cs`
- [X] T038 [P] Agregar guards sin IO/printing/ZXing/FastReport en Presentation y sin WPF/Data en Business en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`
- [X] T039 [P] Ejecutar 20 cancelaciones/cierres y respuesta no cooperativa en `tests/SysTools.BusinessRules.Tests/Labels/LabelOutputCancellationTests.cs`
- [X] T040 [P] Crear goldens deterministas y manifest SHA-256 en `tests/SysTools.Configuration.Tests/Reports/Assets/Goldens/`
- [X] T041 Validar límites de 2 s/10 MiB en `tests/SysTools.Configuration.Tests/Reports/LabelRenderPerformanceTests.cs`
- [X] T042 Auditar paquetes/DI/templates/fronteras en `specs/011-label-reports-printing/validation/dependency-architecture-audit.md`
- [X] T043 Ejecutar build 0 warnings y regresión completa en `specs/011-label-reports-printing/validation/full-regression.md`
- [X] T044 Registrar integraciones externas PASS/SKIPPED en `specs/011-label-reports-printing/validation/controlled-integrations.md`
- [X] T045 Ejecutar quickstart automatizado y registrar en `specs/011-label-reports-printing/validation/quickstart-results.md`
- [X] T046 Auditar FR-001..030, SC-001..010 y constitución en `specs/011-label-reports-printing/validation/constitution-traceability.md`

## Dependencies

- Setup -> Foundation -> US1 -> US2 -> US3 -> Polish.
- US2 reutiliza documento/workflow de US1; US3 coordina ambas rutas.
- Tests [P] pueden prepararse en archivos separados, pero cambios al workflow son secuenciales.

## Implementation Strategy

1. Foundation y preview como MVP seguro sin hardware.
2. Spool productivo detrás de `ProductionApproved` y protocolo opt-in.
3. Integración del verificador y lifecycle.
4. Seguridad, goldens, rendimiento y trazabilidad.

## Notes

- No instalar FastReport ni otra dependencia comercial.
- No marcar impresión productiva aprobada sin prueba física documentada.
- No incluir producto, barcode, printer, rutas o bytes en logs.
