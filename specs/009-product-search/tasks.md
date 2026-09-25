# Tasks: Búsqueda de productos por descripción

**Input**: Design documents from `/specs/009-product-search/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: La especificación exige matrices de 50 prefijos, casing, 100 búsquedas, single-flight, selección, cancelación, privacidad, accesibilidad, layout e integración controlada. Los contratos críticos se validan con pruebas antes del código correspondiente.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo porque toca archivos distintos y no depende de trabajo incompleto.
- **[Story]**: Historia funcional cubierta por la tarea.
- Cada tarea incluye rutas exactas.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirmar la línea base de Feature 008 y preparar evidencia de 009 sin agregar proyectos o paquetes.

- [X] T001 Ejecutar build y regresión actuales y registrar proyectos, paquetes, conteos y skips base en `specs/009-product-search/validation/baseline.md`
- [X] T002 [P] Extender dobles configurables del workflow/productos/formato/logger para búsqueda sin depender todavía de tipos UI en `tests/SysTools.BusinessRules.Tests/TestDoubles/PriceVerifierWorkflowDoubles.cs`
- [X] T003 [P] Extender el doble de `IPriceVerifierWorkflow` y preparar doble de diálogo/colector para pruebas del padre en `tests/SysTools.Presentation.Tests/TestDoubles.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Crear resultados seguros y fronteras de diálogo que bloquean las tres historias.

**CRITICAL**: No iniciar historias hasta completar esta fase.

- [X] T004 [P] Escribir primero pruebas fallidas de `PriceVerifierSearchItem`: Barcode obligatorio/no vacío/máximo 50; Description no nula/máximo 255; FormattedPrice opcional; Stock no vacío, en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierSearchResultTests.cs`
- [X] T005 Escribir primero pruebas fallidas de `PriceVerifierSearchResult`: Success exige al menos un item; NoMatches/MissingInput/InputTooLong/OperationalFailure exigen lista vacía; snapshot read-only; `IsSuccess` solo Success, en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierSearchResultTests.cs`
- [X] T006 Crear `PriceVerifierSearchStatus`, `PriceVerifierSearchItem` y `PriceVerifierSearchResult` inmutables con las restricciones textuales de T004-T005 en `src/Entities/PriceVerifier/PriceVerifierSearchStatus.cs`, `src/Entities/PriceVerifier/PriceVerifierSearchItem.cs` y `src/Entities/PriceVerifier/PriceVerifierSearchResult.cs`
- [X] T007 Extender `IPriceVerifierWorkflow` con `SearchAsync(string?, CancellationToken)` sin exponer configuración en `src/Business/PriceVerifier/IPriceVerifierWorkflow.cs`
- [X] T008 [P] Definir `ProductSearchDialogOutcome`, `ProductSearchDialogResult` e `IProductSearchDialogService.ShowDialog/CloseActive` con Selected=barcode obligatorio y otros outcomes sin barcode en `src/Presentation/Modules/PriceVerifier/Search/ProductSearchDialogResult.cs` y `src/Presentation/Modules/PriceVerifier/Search/IProductSearchDialogService.cs`
- [X] T009 Ejecutar pruebas de entidades/contratos, confirmar rojo-verde y registrar invariantes en `specs/009-product-search/validation/foundation.md`

**Checkpoint**: Los estados y fronteras no transportan `Product`, configuración ni secretos.

---

## Phase 3: User Story 1 - Encontrar productos sin conocer el código (Priority: P1) MVP

**Goal**: Abrir la búsqueda, confirmar un prefijo literal y mostrar resultados ordenados con precio/existencia seguros.

**Independent Test**: Con sesión Ready y catálogo controlado, 50 prefijos y pares de casing producen únicamente coincidencias de inicio ordenadas; input inválido no accede al catálogo.

### Tests for User Story 1

- [X] T010 [P] [US1] Escribir pruebas fallidas del repositorio para `STARTING WITH` parametrizado literal, sin `%` agregado, símbolos `%/_`, orden descripción+id y colección read-only en `tests/SysTools.Repositories.Tests/Data/FirebirdProductRepositorySearchTests.cs`
- [X] T011 [P] [US1] Escribir pruebas fallidas de SearchAsync para sesión ausente, null/vacío/espacios, límites 1/255/256, trim exterior, una llamada, NoMatches y formato solo con precio en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierSearchTests.cs`
- [X] T012 [P] [US1] Escribir matriz fallida de al menos 50 prefijos/productos con casing, acentos, contenido interior, símbolos, descripciones/precios/stock ausentes y orden preservado en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierSearchReliabilityTests.cs`
- [X] T013 [P] [US1] Escribir pruebas fallidas del `ProductSearchViewModel` para estado inicial, foco, input, busy, single-flight, publicación atómica, NoMatches y primera selección en `tests/SysTools.Presentation.Tests/ProductSearch/ProductSearchViewModelTests.cs`
- [X] T014 [P] [US1] Escribir pruebas WPF STA fallidas para binding TwoWay, Enter contextual, progreso/texto accesible, tabla read-only y medida del diálogo en `tests/SysTools.Presentation.Tests/ProductSearch/ProductSearchAccessibilityTests.cs`

### Implementation for User Story 1

- [X] T015 [US1] Cambiar la búsqueda Firebird a prefijo literal `UPPER(DESCRIPCION) STARTING WITH UPPER(@descriptionPrefix)` preservando parametrización/orden/timeout en `src/Data/Repositories/FirebirdProductRepository.cs`
- [X] T016 [US1] Implementar `SearchAsync` con sesión privada, validación <=255, epoch, una llamada, orden preservado, formato condicional, NoMatches y logging sanitizado en `src/Business/PriceVerifier/PriceVerifierWorkflow.cs`
- [X] T017 [US1] Implementar estado/comandos/generación/CTS/foco y single-flight de búsqueda en `src/Presentation/Modules/PriceVerifier/Search/ProductSearchViewModel.cs`
- [X] T018 [US1] Crear diálogo accesible con criterio, botón Buscar, estado, progreso y DataGrid Código/Descripción/Precio/Existencia en `src/Presentation/Modules/PriceVerifier/Search/ProductSearchWindow.xaml`
- [X] T019 [US1] Implementar únicamente foco inicial/resultados y puente visual de doble clic/cierre en `src/Presentation/Modules/PriceVerifier/Search/ProductSearchWindow.xaml.cs`
- [X] T020 [US1] Implementar servicio modal transitorio con owner/CenterOwner/ShowInTaskbar=false y cierre activo seguro en `src/Presentation/Modules/PriceVerifier/Search/ProductSearchDialogService.cs`
- [X] T021 [US1] Registrar ViewModel transitorio, factoría/servicio de diálogo y dependencias con validación DI en `src/Presentation/App.xaml.cs` y `tests/SysTools.Presentation.Tests/Composition/DependencyInjectionTests.cs`
- [X] T022 [US1] Ejecutar pruebas Repository/Business/Dialog y documentar prefijos, casing, límites, orden, formato y accesibilidad en `specs/009-product-search/validation/us1-search.md`

**Checkpoint**: El operador puede buscar por prefijo literal y revisar una lista accesible sin afectar el producto principal.

---

## Phase 4: User Story 2 - Seleccionar y cargar un producto (Priority: P1)

**Goal**: Confirmar una fila por Enter o doble clic y cargar una sola vez el producto exacto en el verificador.

**Independent Test**: Con resultados controlados, ambas formas de selección devuelven el mismo código exacto, cierran el diálogo y reutilizan LookupAsync una vez.

### Tests for User Story 2

- [X] T023 [P] [US2] Escribir pruebas fallidas de selección válida/inválida, guardia atómica, Enter/doble clic equivalentes y código con ceros en `tests/SysTools.Presentation.Tests/ProductSearch/ProductSearchSelectionTests.cs`
- [X] T024 [P] [US2] Escribir pruebas fallidas del padre para apertura condicionada, cancelación preservando producto, Selected asignando Barcode y exactamente un LookupAsync, NotFound no bloqueante y foco final en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierSearchIntegrationTests.cs`
- [X] T025 [P] [US2] Escribir prueba fallida de 100 búsquedas/selecciones secuenciales con cero duplicados/cruces y pantalla final correspondiente a la selección 100 en `tests/SysTools.Presentation.Tests/ProductSearch/ProductSearchReliabilityTests.cs`
- [X] T026 [P] [US2] Agregar pruebas WPF STA para Ctrl+F7, Enter en tabla, Escape, doble clic de fila y retorno de foco/select-all en `tests/SysTools.Presentation.Tests/ProductSearch/ProductSearchAccessibilityTests.cs`

### Implementation for User Story 2

- [X] T027 [US2] Completar `SelectCommand`/`CancelCommand`, resultado único y cierre solicitado en `src/Presentation/Modules/PriceVerifier/Search/ProductSearchViewModel.cs`
- [X] T028 [US2] Implementar `OpenSearchCommand`, `IsSearchAvailable`, guardia de diálogo, resultado Selected/Canceled y reutilización de lookup exacto en `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs`
- [X] T029 [US2] Habilitar Buscar, Ctrl+F7, tabulación y enlace del nuevo comando manteniendo Imprimir/Configuración deshabilitados en `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml`
- [X] T030 [US2] Coordinar cierre/resultado modal para que Enter y doble clic invoquen el mismo comando una sola vez en `src/Presentation/Modules/PriceVerifier/Search/ProductSearchWindow.xaml.cs` y `src/Presentation/Modules/PriceVerifier/Search/ProductSearchDialogService.cs`
- [X] T031 [US2] Ejecutar pruebas Selection/Parent/100 búsquedas/WPF y documentar selección exacta, consulta única, cancelación y foco en `specs/009-product-search/validation/us2-selection.md`

**Checkpoint**: El flujo completo permite localizar y cargar un producto sin conocer su código y sin duplicar reglas comerciales.

---

## Phase 5: User Story 3 - Cancelar o recuperarse sin perder el flujo (Priority: P2)

**Goal**: Cancelar/cerrar sin alterar el resultado y bloquear con Retry solo ante fallo operativo, sin publicaciones tardías ni filtraciones.

**Independent Test**: Cancelar conserva el producto; cerrar durante búsqueda suprime respuestas; fallo de catálogo cierra, limpia, bloquea y habilita Retry con logs seguros.

### Tests for User Story 3

- [X] T032 [P] [US3] Escribir pruebas fallidas de fallo/cancelación Business: propagación, invalidación solo del epoch vigente, respuesta antigua y búsqueda posterior bloqueada en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierSearchFailureTests.cs`
- [X] T033 [P] [US3] Escribir pruebas fallidas de cierre/deactivate, cancelación cooperativa/no cooperativa, reabrir y respuesta antigua posterior en `tests/SysTools.Presentation.Tests/ProductSearch/ProductSearchLifecycleTests.cs`
- [X] T034 [P] [US3] Escribir auditoría fallida con centinelas para criterio, descripción, barcode, host, ruta, usuario, password, licencia y excepción en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierSearchSecurityTests.cs` y `tests/SysTools.Presentation.Tests/ProductSearch/ProductSearchSecurityTests.cs`

### Implementation for User Story 3

- [X] T035 [US3] Completar traducción de fallos, invalidación vigente, cancelación y logs Stage/Status/Category/DurationMs sin payload en `src/Business/PriceVerifier/PriceVerifierWorkflow.cs`
- [X] T036 [US3] Completar Close/Deactivate, generation guards, OperationalFailure y cero publicación/log por cancelación en `src/Presentation/Modules/PriceVerifier/Search/ProductSearchViewModel.cs`
- [X] T037 [US3] Cerrar diálogo activo al desactivar módulo/cerrar shell y publicar bloqueo coherente con Retry en `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs` y `src/Presentation/Modules/PriceVerifier/Search/ProductSearchDialogService.cs`
- [X] T038 [US3] Ejecutar pruebas Failure/Lifecycle/Security y documentar carreras, bloqueo y cero filtraciones en `specs/009-product-search/validation/us3-recovery-security.md`

**Checkpoint**: Ninguna cancelación/falla deja ventana, datos o mensajes obsoletos, y los fallos operativos se recuperan con Retry.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Accesibilidad, layout, arquitectura, alcance, integraciones controladas y regresión.

- [X] T039 [P] Extender smoke WPF STA para diálogo a 1280x720/125% DPI, textos largos, scroll, AutomationProperties y estados no dependientes solo de color en `tests/SysTools.Presentation.Tests/ProductSearch/ProductSearchAccessibilityTests.cs`
- [X] T040 [P] Extender guards para prohibir Data/Firebird/IO/licencia/criptografía/ZXing/reportes/impresión/configuración en Search View/ViewModel y limitar code-behind a visual en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`
- [X] T041 [P] Extender integración controlada de repositorio para acentos/casing, `%/_`, descripciones largas, nulls, stock, decimales, encoding y orden repetible en `tests/SysTools.Repositories.Tests/Integration/FirebirdRepositoryIntegrationTests.cs`
- [X] T042 [P] Auditar que no se agregaron paquetes/proyectos y registrar fronteras/DI/cambios Data acotados en `specs/009-product-search/validation/dependency-architecture-audit.md`
- [X] T043 Ejecutar restore, build sin warnings y regresión completa; corregir regresiones y registrar conteos/omisiones en `specs/009-product-search/validation/full-regression.md`
- [X] T044 Ejecutar integración Repository opt-in y registrar PASS o SKIPPED sin secretos en `specs/009-product-search/validation/controlled-integration.md`
- [X] T045 Ejecutar recorrido manual de `quickstart.md` a 1280x720, maximizado y 125% DPI con teclado/doble clic/cancelación/fallo+Retry, registrando PASS/FAIL/SKIPPED en `specs/009-product-search/validation/manual-wpf.md`
- [X] T046 Ejecutar todos los escenarios automatizados de `quickstart.md` y registrar comandos/resultados en `specs/009-product-search/validation/quickstart-results.md`
- [X] T047 Auditar FR-001..FR-028, SC-001..SC-010 y gates constitucionales contra código/pruebas/evidencias en `specs/009-product-search/validation/constitution-traceability.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: inicia de inmediato.
- **Foundational**: depende de Setup y bloquea todas las historias.
- **US1**: depende de Foundational y entrega búsqueda/lista funcional.
- **US2**: depende de US1 porque selecciona resultados y los integra con lookup exacto.
- **US3**: depende de US1/US2 para endurecer el flujo completo.
- **Polish**: depende de las tres historias.

### Within Each User Story

- Escribir y ejecutar pruebas rojas antes del código correspondiente.
- Entities/contratos preceden al workflow; workflow precede al ViewModel; ViewModel precede a ventana/servicio.
- Cambios compartidos en workflow, ViewModel de búsqueda, padre y servicio de diálogo son secuenciales.
- Cada checkpoint queda verde antes de avanzar.

### Parallel Opportunities

- T002-T003, T004/T008, T010-T014, T023-T026, T032-T034 y T039-T042 pueden ejecutarse en paralelo según sus archivos.
- Pruebas Business, Repository y Presentation de una historia pueden prepararse en paralelo.
- Auditorías y evidencia pueden avanzar mientras se ejecutan validaciones controladas.

## Parallel Example: User Story 1

```text
Task T010: contrato literal del repositorio
Task T011: límites y estados del workflow
Task T012: matriz de 50 prefijos
Task T013: estado/comandos del diálogo
Task T014: contrato WPF accesible
```

## Parallel Example: User Story 2

```text
Task T023: selección válida/inválida
Task T024: integración con el padre
Task T025: 100 búsquedas/selecciones
Task T026: teclado/doble clic/foco WPF
```

## Parallel Example: User Story 3

```text
Task T032: fallos/cancelación Business
Task T033: lifecycle del diálogo
Task T034: privacidad con centinelas
```

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Setup y Foundational.
2. Implementar búsqueda literal y resultados seguros.
3. Entregar diálogo con lista accesible.
4. Validar US1 independientemente antes de selección.

### Incremental Delivery

1. US1 permite encontrar/revisar productos.
2. US2 permite cargar la selección y completa el flujo principal.
3. US3 agrega cancelación, carreras, privacidad y recuperación.
4. Polish cierra integración, accesibilidad, regresión y trazabilidad.

## Notes

- Marcar `[X]` solo después de ejecutar y verificar cada tarea.
- No registrar criterio, códigos, Product, configuración, licencia, credenciales ni excepciones contaminadas.
- No agregar paquetes/proyectos ni acceso Data desde Presentation.
- Las integraciones externas omitidas se documentan como SKIPPED; no se inventa evidencia.
