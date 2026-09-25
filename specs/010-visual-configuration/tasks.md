# Tasks: Configuración visual del sistema

**Implementation result (2026-09-25)**: COMPLETE. La evidencia ejecutada está en `validation/`; regresión final: 632 PASS, 0 FAIL, 6 integraciones opt-in SKIPPED. Los checkboxes inferiores conservan el plan generado original; prevalece la evidencia trazable de validación.

**Input**: Design documents from `/specs/010-visual-configuration/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: La feature toca secretos, AppData, Firebird, licencia, hardware, catálogos y WPF; los contratos críticos se prueban antes de implementación y las integraciones externas se documentan PASS o SKIPPED.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Phase 1: Setup

- [ ] T001 Ejecutar restore/build/regresión base y registrar conteos/skips en `specs/010-visual-configuration/validation/baseline.md`
- [ ] T002 [P] Crear dobles de workflow, catálogos, picker, clipboard, confirmación y diálogo en `tests/SysTools.Presentation.Tests/VisualConfiguration/ConfigurationTestDoubles.cs`
- [ ] T003 [P] Crear dobles configurables de configuración/conexión/licencia/hardware/catálogos en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorDoubles.cs`

## Phase 2: Foundational

- [ ] T004 [P] Escribir pruebas de `ConfigurationDraft`: nueve campos no sensibles, null a vacío, contenido interior intacto y columnas 1..3 en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorEntityTests.cs`
- [ ] T005 [P] Escribir pruebas de snapshots/resultados/opciones: colecciones read-only, estados válidos, opción unavailable única y cero `AppConfiguration`/password/licencia en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorEntityTests.cs`
- [ ] T006 Crear entidades seguras draft/snapshot/results/options/license en `src/Entities/ConfigurationEditor/`
- [ ] T007 Definir `IConfigurationEditorWorkflow`, `IPrinterCatalog`, `ITemplateCatalog` y contratos de recuperación en `src/Business/ConfigurationEditor/`
- [ ] T008 [P] Definir servicios visuales de diálogo/picker/clipboard/confirmación sin IO en ViewModel en `src/Presentation/Modules/Configuration/Services/`
- [ ] T009 Endurecer logging del repositorio para no adjuntar excepción/path y probar sentinels en `src/Data/Configuration/JsonConfigurationRepository.cs` y `tests/SysTools.Configuration.Tests/Data/ConfigurationSecurityTests.cs`
- [ ] T010 Ejecutar contratos fundacionales y documentar invariantes en `specs/010-visual-configuration/validation/foundation.md`

## Phase 3: User Story 1 - Configuración inicial (P1) MVP

**Goal**: Abrir configuración inicial, probar conexión exacta y guardar de forma segura.

**Independent Test**: Perfil sin readiness abre un diálogo; guardar se rechaza antes de prueba exacta y, tras éxito, persiste una vez y re-prepara el módulo.

- [ ] T011 [P] [US1] Escribir pruebas rojas de Open/initial/default/fallos y snapshot sin password en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorInitialTests.cs`
- [ ] T012 [P] [US1] Escribir pruebas rojas de prueba exacta, revisión, cambio/reversión, categorías y stale epoch en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorConnectionTests.cs`
- [ ] T013 [P] [US1] Escribir matriz roja de 20 flujos iniciales, single-flight y persistencia única en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorInitialReliabilityTests.cs`
- [ ] T014 [P] [US1] Escribir pruebas ViewModel de carga, errores, busy, foco y comandos test/save/cancel en `tests/SysTools.Presentation.Tests/VisualConfiguration/ConfigurationViewModelInitialTests.cs`
- [ ] T015 [P] [US1] Escribir pruebas WPF STA de owner, PasswordBox vacío, etiquetas, teclado, scroll y barra fija en `tests/SysTools.Presentation.Tests/VisualConfiguration/ConfigurationAccessibilityTests.cs`
- [ ] T016 [US1] Implementar `ConfigurationEditorWorkflow.OpenAsync/Validate/TestConnectionAsync` con baseline privado, epoch, revision y proof opaco en `src/Business/ConfigurationEditor/ConfigurationEditorWorkflow.cs`
- [ ] T017 [US1] Implementar guardado inicial que exige password efectivo y proof exacto y traduce todos los issues en `src/Business/ConfigurationEditor/ConfigurationEditorWorkflow.cs`
- [ ] T018 [US1] Implementar `ConfigurationViewModel` con generation/CTS/single-flight/foco/dirty y sin propiedad password en `src/Presentation/Modules/Configuration/ConfigurationViewModel.cs`
- [ ] T019 [US1] Crear diálogo accesible de cuatro secciones y barra fija en `src/Presentation/Modules/Configuration/ConfigurationWindow.xaml`
- [ ] T020 [US1] Implementar puentes visuales de PasswordBox/foco/cierre únicamente en `src/Presentation/Modules/Configuration/ConfigurationWindow.xaml.cs`
- [ ] T021 [US1] Implementar servicio modal transitorio con owner/guardia/resultado tipado en `src/Presentation/Modules/Configuration/Services/ConfigurationDialogService.cs`
- [ ] T022 [US1] Integrar auto-apertura posterior a shell visible, acción manual, botón Configuración y una re-preparación tras Saved en `src/Presentation/Shell/ViewModels/ShellViewModel.cs`, `src/Presentation/Shell/Views/ShellWindow.xaml.cs`, `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs` y `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml`
- [ ] T023 [US1] Registrar workflow/VM/servicios transitorios y validar DI en `src/Presentation/App.xaml.cs` y `tests/SysTools.Presentation.Tests/Composition/DependencyInjectionTests.cs`
- [ ] T024 [US1] Ejecutar suites iniciales y documentar 20/20, proof y shell refresh en `specs/010-visual-configuration/validation/us1-initial-configuration.md`

## Phase 4: User Story 2 - Edición segura (P1)

**Goal**: Editar preferencias preservando secreto/configuración anterior y recuperar almacenamiento dañado explícitamente.

**Independent Test**: 50 cambios operativos con password visual vacío conservan credencial; cancelar/fallar conserva bytes; recovery requiere confirmación/respaldo.

- [ ] T025 [P] [US2] Escribir 50 pruebas de preservación/reemplazo de password y cambios operativos sin retest en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorSecureEditTests.cs`
- [ ] T026 [P] [US2] Escribir validación combinatoria de required/ambiente/formato/columnas con issues completos y cero writes en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorValidationTests.cs`
- [ ] T027 [P] [US2] Escribir pruebas de cancelación/fallo byte-a-byte y recovery confirmado con backup atómico en `tests/SysTools.Configuration.Tests/Data/ConfigurationRecoveryTests.cs`
- [ ] T028 [P] [US2] Escribir pruebas ViewModel dirty/descartar/permanecer/reintento/respuesta tardía en `tests/SysTools.Presentation.Tests/VisualConfiguration/ConfigurationLifecycleTests.cs`
- [ ] T029 [US2] Implementar merge privado del password, cambios operativos sin proof y limpieza tras save en `src/Business/ConfigurationEditor/ConfigurationEditorWorkflow.cs`
- [ ] T030 [US2] Extender repositorio/servicio con recovery explícito, backup administrado y escritura atómica en `src/Business/Configuration/IConfigurationService.cs`, `src/Business/Configuration/IConfigurationRepository.cs`, `src/Business/Configuration/ConfigurationService.cs` y `src/Data/Configuration/JsonConfigurationRepository.cs`
- [ ] T031 [US2] Completar validación visual por campo, cierre dirty, retry y estados seguros en `src/Presentation/Modules/Configuration/ConfigurationViewModel.cs` y `src/Presentation/Modules/Configuration/ConfigurationWindow.xaml.cs`
- [ ] T032 [US2] Ejecutar suites de 50 ediciones/recovery/lifecycle y documentar evidencia en `specs/010-visual-configuration/validation/us2-secure-editing.md`

## Phase 5: User Story 3 - Licencia, UUID y opciones (P2)

**Goal**: Validar licencia, copiar UUID y elegir opciones disponibles sin imprimir.

**Independent Test**: Matriz de licencia segura, UUID copiable y catálogos 0/1/n con valores persistidos unavailable.

- [ ] T033 [P] [US3] Escribir pruebas de licencia válida/11 estados/cambio Keep-Replace-Clear/invalidez no reemplaza en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorLicenseTests.cs`
- [ ] T034 [P] [US3] Escribir pruebas de hardware disponible/no disponible y cero logging UUID en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorHardwareTests.cs`
- [ ] T035 [P] [US3] Escribir pruebas de catálogos 0/1/n, orden, duplicados, fallo, unicode, extensión/traversal y unavailable en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationCatalogTests.cs`
- [ ] T036 [P] [US3] Escribir pruebas Presentation de picker/clipboard/license/options sin payload en `tests/SysTools.Presentation.Tests/VisualConfiguration/ConfigurationLicenseOptionsTests.cs`
- [ ] T037 [US3] Implementar validación privada de licencia ligada a epoch/conexión y resumen seguro en `src/Business/ConfigurationEditor/ConfigurationEditorWorkflow.cs`
- [ ] T038 [US3] Implementar hardware/catálogos tipados y fusión unavailable en `src/Business/ConfigurationEditor/ConfigurationEditorWorkflow.cs`
- [ ] T039 [US3] Implementar enumeración Windows de impresoras y templates administrados sin impresión/traversal en `src/Data/ConfigurationEditor/WindowsPrinterCatalog.cs` y `src/Data/ConfigurationEditor/ManagedTemplateCatalog.cs`
- [ ] T040 [US3] Implementar picker/clipboard/confirmación WPF y controles de licencia/UUID/opciones en `src/Presentation/Modules/Configuration/Services/` y `src/Presentation/Modules/Configuration/ConfigurationWindow.xaml`
- [ ] T041 [US3] Ejecutar suites licencia/UUID/catálogos y documentar en `specs/010-visual-configuration/validation/us3-license-options.md`

## Phase 6: Polish & Cross-Cutting

- [ ] T042 [P] Ejecutar auditoría sentinel password/licencia/path/UUID/host y logs/excepciones en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorSecurityTests.cs` y `tests/SysTools.Presentation.Tests/VisualConfiguration/ConfigurationSecurityTests.cs`
- [ ] T043 [P] Ejecutar 100 operaciones/carreras y cero publicaciones stale en `tests/SysTools.Configuration.Tests/ConfigurationEditor/ConfigurationEditorReliabilityTests.cs`
- [ ] T044 [P] Extender guards de capas y alcance sin IO/Firebird/DPAPI/process/printing en View/ViewModel en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`
- [ ] T045 [P] Extender STA para 1280x720/125%, textos largos, AutomationProperties y teclado completo en `tests/SysTools.Presentation.Tests/VisualConfiguration/ConfigurationAccessibilityTests.cs`
- [ ] T046 Auditar paquetes/proyectos/DI/fronteras y documentar en `specs/010-visual-configuration/validation/dependency-architecture-audit.md`
- [ ] T047 Ejecutar restore/build sin warnings/regresión completa y registrar en `specs/010-visual-configuration/validation/full-regression.md`
- [ ] T048 Ejecutar Firebird/licencia opt-in y registrar PASS/SKIPPED sin secretos en `specs/010-visual-configuration/validation/controlled-integrations.md`
- [ ] T049 Ejecutar quickstart automatizado/manual y registrar 1280x720/max/125% en `specs/010-visual-configuration/validation/quickstart-results.md` y `specs/010-visual-configuration/validation/manual-wpf.md`
- [ ] T050 Auditar FR-001..FR-030, SC-001..SC-010 y constitución en `specs/010-visual-configuration/validation/constitution-traceability.md`

## Dependencies & Execution Order

- Setup precede Foundation; Foundation bloquea historias.
- US1 entrega MVP y precede US2/US3 porque crea workflow/diálogo.
- US2 y US3 son independientes después de US1, salvo cambios secuenciales al workflow.
- Polish requiere todas las historias.
- Tests de cada historia se escriben/ejecutan rojos antes de implementación y el checkpoint debe quedar verde.

## Parallel Opportunities

- T002/T003; T004/T005/T008; T011-T015; T025-T028; T033-T036; T042-T045 tocan archivos separados.
- Suites Business/Data/Presentation pueden prepararse en paralelo; cambios al workflow son secuenciales.

## Implementation Strategy

1. Setup + frontera segura.
2. US1: apertura inicial, prueba exacta, guardado y re-preparación.
3. US2: preservación de secreto, validación, dirty y recovery.
4. US3: licencia, UUID, impresora y templates.
5. Seguridad, carreras, integración controlada, visual y trazabilidad.

## Notes

- Marcar `[X]` solo con evidencia ejecutada.
- No registrar ni exponer password, licencia, UUID completo, host o paths privados.
- `.frx`/`.jasper` son solo IDs de catálogo; no adoptar motor ni imprimir.
