# Tasks: Correos, soporte y logs

**Input**: Design documents from `/specs/012-support-mail-logs/`

**Tests**: La feature maneja hardware, procesos y logs; todas las historias incluyen pruebas automatizadas antes de implementación.

## Phase 1: Setup

- [X] T001 Registrar baseline restore/build/test en `specs/012-support-mail-logs/validation/baseline.md`
- [X] T002 [P] Crear dobles de hardware, launcher y log store en `tests/SysTools.BusinessRules.Tests/Support/SupportDoubles.cs`
- [X] T003 [P] Crear helper de directorio temporal para logs en `tests/SysTools.Configuration.Tests/Support/SupportLogTestDirectory.cs`

## Phase 2: Foundational

- [X] T004 [P] Probar invariantes de draft, resultados y archivos con `Subject 1..160`, `Body 1..6000`, colección `<=20` y contenido `<=256 KiB/500 líneas` en `tests/SysTools.BusinessRules.Tests/Support/SupportEntityTests.cs`
- [X] T005 Implementar entidades inmutables y estados tipados en `src/Entities/Support/SupportModels.cs`
- [X] T006 Definir workflow y puertos de launcher/store en `src/Business/Support/ISupportWorkflow.cs` y `src/Business/Support/SupportContracts.cs`
- [X] T007 Ejecutar contratos fundacionales y documentar fronteras en `specs/012-support-mail-logs/validation/foundation.md`

## Phase 3: User Story 1 - Solicitar licencia (P1) MVP

**Independent Test**: UUID disponible produce borrador y cada canal abre exactamente una vez; UUID ausente no abre nada.

- [X] T008 [P] [US1] Probar solicitud de licencia, UUID ausente, versión y cancelación en `tests/SysTools.BusinessRules.Tests/Support/LicenseSupportWorkflowTests.cs`
- [X] T009 [P] [US1] Probar URI Default/Gmail/Outlook con 20 textos Unicode y cero envío en `tests/SysTools.Configuration.Tests/Support/WindowsSupportExternalLauncherTests.cs`
- [X] T010 [P] [US1] Probar ViewModel para carga, copia explícita, preparación, canal, busy y late response en `tests/SysTools.Presentation.Tests/Support/SupportLicenseViewModelTests.cs`
- [X] T011 [US1] Implementar composición y coordinación single-flight/epoch en `src/Business/Support/SupportWorkflow.cs`
- [X] T012 [US1] Implementar destinos fijos y apertura sustituible de Windows en `src/Data/Support/WindowsSupportExternalLauncher.cs`
- [X] T013 [US1] Implementar `SupportViewModel` con lifecycle y comandos de licencia en `src/Presentation/Modules/Support/SupportViewModel.cs`
- [X] T014 [US1] Crear vista accesible de soporte con selector de canal en `src/Presentation/Modules/Support/SupportView.xaml`
- [X] T015 [US1] Integrar módulo habilitado y DI en `src/Presentation/App.xaml.cs` y `src/Presentation/Shell/ViewModels/ShellViewModel.cs`
- [X] T016 [US1] Registrar evidencia del flujo de licencia en `specs/012-support-mail-logs/validation/license-request.md`

## Phase 4: User Story 2 - Reportar un error (P1)

**Independent Test**: resumen/detalle válidos producen borrador seguro por tres canales; valores inválidos no abren destino.

- [X] T017 [P] [US2] Probar límites 1..120/1..4000, código opaco y ausencia de UUID/configuración en `tests/SysTools.BusinessRules.Tests/Support/ErrorReportWorkflowTests.cs`
- [X] T018 [P] [US2] Probar 100 operaciones, single-flight y logging sin payload/excepción en `tests/SysTools.BusinessRules.Tests/Support/SupportSecurityReliabilityTests.cs`
- [X] T019 [P] [US2] Probar bindings, validación y apertura del reporte en `tests/SysTools.Presentation.Tests/Support/SupportErrorReportViewModelTests.cs`
- [X] T020 [US2] Completar composición segura del reporte y resultados recuperables en `src/Business/Support/SupportWorkflow.cs`
- [X] T021 [US2] Completar formulario, mensajes y comandos del reporte en `src/Presentation/Modules/Support/SupportViewModel.cs` y `src/Presentation/Modules/Support/SupportView.xaml`
- [X] T022 [US2] Registrar evidencia de seguridad y canales en `specs/012-support-mail-logs/validation/error-report.md`

## Phase 5: User Story 3 - Consultar y localizar logs (P2)

**Independent Test**: catálogos 0/1/20/25, tail acotado y ataques de ruta producen estados correctos; abrir carpeta usa root administrado.

- [X] T023 [P] [US3] Probar catálogo, orden, límite, metadatos y directorio ausente/vacío en `tests/SysTools.Configuration.Tests/Support/ManagedSupportLogStoreCatalogTests.cs`
- [X] T024 [P] [US3] Probar tail 500 líneas/256 KiB, archivo 50 MiB, rotación y codificación en `tests/SysTools.Configuration.Tests/Support/ManagedSupportLogStoreReadTests.cs`
- [X] T025 [P] [US3] Probar 50 traversal/subdirectorio/extensión/reparse y cero escape en `tests/SysTools.Configuration.Tests/Support/ManagedSupportLogStoreSecurityTests.cs`
- [X] T026 [P] [US3] Probar refresco, selección, preview, apertura de carpeta y late response en `tests/SysTools.Presentation.Tests/Support/SupportLogsViewModelTests.cs`
- [X] T027 [US3] Implementar root AppData y store acotado/seguro en `src/Data/Support/ManagedSupportLogStore.cs`
- [X] T028 [US3] Integrar catálogo, lectura y apertura de carpeta en `src/Business/Support/SupportWorkflow.cs`
- [X] T029 [US3] Completar lista, preview, refresh y abrir carpeta en `src/Presentation/Modules/Support/SupportViewModel.cs` y `src/Presentation/Modules/Support/SupportView.xaml`
- [X] T030 [US3] Registrar evidencia de filesystem y límites en `specs/012-support-mail-logs/validation/log-viewer.md`

## Phase 6: Polish & Cross-Cutting

- [X] T031 [P] Extender guards de capas sin filesystem/process en Presentation y sin Windows/Data en Business en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`
- [X] T032 [P] Probar accesibilidad, teclado, scroll y 1280×720@125% en `tests/SysTools.Presentation.Tests/Support/SupportAccessibilityTests.cs`
- [X] T033 Validar DI, singleton/transient y catálogo de shell en `tests/SysTools.Presentation.Tests/Composition/DependencyInjectionTests.cs`
- [X] T034 Ejecutar auditoría de paquetes, fronteras y seguridad en `specs/012-support-mail-logs/validation/dependency-architecture-audit.md`
- [X] T035 Ejecutar restore/build con cero warnings y regresión completa en `specs/012-support-mail-logs/validation/full-regression.md`
- [X] T036 Ejecutar quickstart automatizado/manual controlado en `specs/012-support-mail-logs/validation/quickstart-results.md`
- [X] T037 Auditar FR-001..030, SC-001..010 y constitución en `specs/012-support-mail-logs/validation/constitution-traceability.md`

## Dependencies

- Setup -> Foundational -> US1 -> US2 -> US3 -> Polish.
- US2 reutiliza borrador/launcher de US1; US3 reutiliza workflow/VM pero es verificable con store independiente.
- Las tareas `[P]` usan archivos distintos y pueden ejecutarse en paralelo tras sus prerrequisitos.

## Implementation Strategy

1. Entidades y puertos sin infraestructura.
2. Solicitud de licencia como MVP sin envío automático.
3. Reporte seguro reutilizando canales.
4. Logs acotados y protegidos.
5. Integración WPF, seguridad, accesibilidad y regresión.

## Notes

- Nunca registrar o adjuntar contenido, UUID, rutas o configuración.
- No agregar SMTP, OAuth ni dependencias de correo.
- No abrir procesos reales desde pruebas; usar backend sustituible.

