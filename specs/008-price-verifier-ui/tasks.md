# Tasks: Integracion operativa del verificador de precios

**Input**: Design documents from `/specs/008-price-verifier-ui/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: La especificacion exige matrices automatizadas de preparacion y productos, 100 consultas, single-flight, cancelacion, privacidad, accesibilidad, layout y regresion. Cada grupo de pruebas se escribe y confirma rojo antes de su implementacion.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo porque toca archivos distintos y no depende de trabajo incompleto.
- **[Story]**: Historia funcional cubierta por la tarea.
- Cada tarea incluye rutas exactas.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirmar la linea base y preparar evidencia sin agregar proyectos o paquetes.

- [X] T001 Ejecutar build y pruebas actuales antes de Feature 008 y registrar proyectos, paquetes y conteos base en `specs/008-price-verifier-ui/validation/baseline.md`
- [X] T002 [P] Preparar dobles configurables de configuracion, conexion, licencia, productos, formato y logger para TDD del workflow en `tests/SysTools.BusinessRules.Tests/TestDoubles/PriceVerifierWorkflowDoubles.cs`
- [X] T003 [P] Preparar helpers genericos de operacion asincrona controlada y logger colector, sin depender aun de tipos Feature 008, en `tests/SysTools.Presentation.Tests/TestDoubles.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Crear resultados seguros, contratos compartidos, comando asincrono y lifecycle que bloquean las tres historias.

**CRITICAL**: No iniciar historias hasta completar esta fase.

- [X] T004 [P] Escribir primero pruebas fallidas de `PriceVerifierPreparationResult`: status definido, mensaje no vacio/seguro, Ready exige conexion Success y licencia Valid, ConfigurationUnavailable no admite estados posteriores, ConnectionUnavailable exige conexion no Success y licencia null, LicenseUnavailable exige conexion Success/licencia no Valid, y nunca contiene `AppConfiguration`, en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierResultTests.cs`
- [X] T005 Escribir primero pruebas fallidas de `PriceVerifierLookupResult`: Success exige Product y conserva barcode; precio solo puede aparecer con Success y `PriceWithTax`; estados no exitosos contienen Product/precio null; `IsSuccess` solo para Success, en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierResultTests.cs`
- [X] T006 Crear `PriceVerifierPreparationStatus` con `Ready`, `ConfigurationUnavailable`, `ConnectionUnavailable`, `LicenseUnavailable` y `PriceVerifierLookupStatus` con `Success`, `MissingInput`, `InputTooLong`, `NotFound`, `OperationalFailure` en `src/Entities/PriceVerifier/PriceVerifierPreparationStatus.cs` y `src/Entities/PriceVerifier/PriceVerifierLookupStatus.cs`
- [X] T007 Implementar resultados sellados/inmutables con las invariantes textuales de T004-T005, mensajes estables y sin configuracion/secretos en `src/Entities/PriceVerifier/PriceVerifierPreparationResult.cs` y `src/Entities/PriceVerifier/PriceVerifierLookupResult.cs`
- [X] T008 [P] Definir `IPriceVerifierWorkflow.PrepareAsync`, `LookupAsync`, `Invalidate` y `IAsyncModuleLifecycle.ActivateAsync`/`Deactivate` conforme al contrato en `src/Business/PriceVerifier/IPriceVerifierWorkflow.cs` y `src/Presentation/Shell/Services/IAsyncModuleLifecycle.cs`
- [X] T009 [P] Escribir primero pruebas fallidas de `AsyncRelayCommand`: ExecuteAsync awaitable, CanExecute falso durante ejecucion, reentrada ignorada sin cola/cancelacion, dos notificaciones y propagacion controlada en `tests/SysTools.Presentation.Tests/Mvvm/MvvmInfrastructureTests.cs`
- [X] T010 Implementar `AsyncRelayCommand` sin paquete nuevo, con guardia atomica, `ExecuteAsync` testeable y frontera `ICommand` segura en `src/Presentation/Commands/AsyncRelayCommand.cs`
- [X] T011 Ejecutar pruebas foundational, confirmar rojo-verde y registrar invariantes/fronteras en `specs/008-price-verifier-ui/validation/foundation.md`

**Checkpoint**: Resultados, contratos y comando async son estables, seguros y no exponen infraestructura.

---

## Phase 3: User Story 1 - Preparar el verificador para operar (Priority: P1)

**Goal**: Cargar configuracion, comprobar conexion/licencia, mostrar estados coherentes y habilitar captura solo cuando la sesion queda Ready.

**Independent Test**: Una matriz controlada de estados de configuracion, conexion y licencia produce Ready o el bloqueo exacto, respeta short-circuit y Retry repite las tres comprobaciones sin abrir Firebird real.

### Tests for User Story 1

- [X] T012 [P] [US1] Escribir primero la matriz fallida de todos los `ConfigurationLoadStatus`, `ConnectionTestStatus` y `LicenseValidationStatus`, verificando orden, short-circuit, conteos y que solo configuracion connection-ready + Success + Valid produce Ready, en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierPreparationTests.cs`
- [X] T013 [P] [US1] Agregar el doble compartido de `IPriceVerifierWorkflow` en `tests/SysTools.Presentation.Tests/TestDoubles.cs` y escribir primero pruebas fallidas de activacion, Pending, Ready, bloqueos, Retry completo, informacion adicional y solicitud inicial de foco en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierViewModelTests.cs`
- [X] T014 [P] [US1] Escribir primero pruebas fallidas que exijan que la shell refleje en tiempo real conexion, licencia y mensaje del modulo sin copias divergentes en `tests/SysTools.Presentation.Tests/Shell/ShellStatusTests.cs`
- [X] T015 [P] [US1] Agregar primero pruebas fallidas de registro singleton y resolucion de `IPriceVerifierWorkflow`, `PriceVerifierViewModel` y shell con ValidateOnBuild/ValidateScopes en `tests/SysTools.Presentation.Tests/Composition/DependencyInjectionTests.cs`

### Implementation for User Story 1

- [X] T016 [US1] Implementar `PrepareAsync` con epoch, configuracion privada, orden config -> conexion -> licencia, short-circuit, mensajes/logging sanitizados e `Invalidate` en `src/Business/PriceVerifier/PriceVerifierWorkflow.cs`
- [X] T017 [US1] Convertir `PriceVerifierViewModel` en estado observable con lifecycle, CTS/epoch, Prepare/Retry, estados Pending/Ready/Blocked, informacion adicional, `FocusRequestVersion` y comandos disponibles sin I/O/reglas directas en `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs`
- [X] T018 [US1] Reexponer y propagar desde el modulo `ConnectionStatus`, `LicenseStatus` y `StatusMessage`, conservando el manejo base de inicializacion sin estado duplicado, en `src/Presentation/Shell/ViewModels/ShellViewModel.cs`
- [X] T019 [US1] Enlazar Loaded/Unloaded exclusivamente al lifecycle y atender la señal inicial de foco mediante Dispatcher sin logica de negocio en `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml.cs`
- [X] T020 [US1] Registrar singleton `IPriceVerifierWorkflow -> PriceVerifierWorkflow` y conservar una sola instancia compartida del ViewModel en `src/Presentation/App.xaml.cs`
- [X] T021 [US1] Ejecutar pruebas Preparation/ViewModel/Shell/Composition y documentar la matriz, short-circuit y Retry en `specs/008-price-verifier-ui/validation/us1-readiness.md`

**Checkpoint**: El modulo muestra preparacion/bloqueos seguros y habilita captura solo con configuracion, conexion y licencia validas.

---

## Phase 4: User Story 2 - Consultar un producto por codigo (Priority: P1)

**Goal**: Capturar por teclado/lector, consultar exactamente una vez y mostrar producto/precio con foco listo para la siguiente captura.

**Independent Test**: Con workflow preparado, al menos 50 productos y 100 consultas secuenciales producen datos/precio exactos, una llamada por Enter, cero duplicados y codigo visible seleccionado para reemplazo.

### Tests for User Story 2

- [X] T022 [P] [US2] Escribir primero pruebas fallidas de Lookup para espacios exteriores, ceros/contenido interior, vacio, limite 50/51, exactamente una llamada de producto, NotFound y formatter solo con precio usando FormatoPrecio cacheado, en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierLookupTests.cs`
- [X] T023 [P] [US2] Crear primero matriz fallida de al menos 50 productos con ceros, acentos, descripcion larga, presentacion/precio/stock ausentes y formatos MX/CO, verificando identidad y neutralidad sin inventar datos, en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierReliabilityTests.cs`
- [X] T024 [P] [US2] Escribir primero pruebas fallidas del ViewModel para Enter, Busy, IsAvailable, mapeo atomico de campos, codigo retenido, incremento de foco y segunda confirmacion ignorada con `TaskCompletionSource`, en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierQueryTests.cs`
- [X] T025 [P] [US2] Escribir primero prueba fallida de 100 consultas secuenciales con exactamente 100 llamadas, cero cruces/duplicados y pantalla final correspondiente a la consulta 100 en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierReliabilityTests.cs`
- [X] T026 [P] [US2] Escribir primero pruebas WPF STA fallidas para binding TwoWay inmediato, KeyBinding Enter, campo deshabilitado durante busy, señal repetible de Focus+SelectAll y Dispatcher receptivo durante una operacion controlada de 6 segundos en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierAccessibilityTests.cs`

### Implementation for User Story 2

- [X] T027 [US2] Implementar `LookupAsync`: normalizar una vez, rechazar vacio/>50 sin repositorio, consulta exacta unica, NotFound, formato condicional y Success seguro con la sesion privada en `src/Business/PriceVerifier/PriceVerifierWorkflow.cs`
- [X] T028 [US2] Implementar confirmacion single-flight, limpieza al iniciar, publicacion atomica de Product/precio/neutrales, codigo visible e incremento de foco tras intentos no bloqueantes en `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs`
- [X] T029 [US2] Habilitar TextBox operativo TwoWay/PropertyChanged, Enter, estado Busy/progreso, mensajes accesibles, Retry condicionado y mantener Buscar/Imprimir/Configuracion deshabilitados en `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml`
- [X] T030 [US2] Implementar Focus+SelectAll condicionado a campo habilitado/visible y sin perder confirmaciones de teclado en `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml.cs`
- [X] T031 [US2] Agregar estilo de captura con foco visible, tabulacion natural, estados disabled/busy y contraste consistente sin reutilizar el placeholder no enfocable en `src/Presentation/Styles/Controls.xaml`
- [X] T032 [US2] Ejecutar pruebas Lookup/matriz/Query/100 consultas/WPF y documentar conteos, formatos, single-flight y foco en `specs/008-price-verifier-ui/validation/us2-query.md`

**Checkpoint**: US1 + US2 forman el MVP: el operador prepara y consulta productos exactos sin mouse ni resultados cruzados.

---

## Phase 5: User Story 3 - Recuperarse de consultas sin resultado o con fallo (Priority: P2)

**Goal**: Mantener el modulo utilizable ante input/not-found y bloquear con Retry solo ante fallos operativos, sin resultados tardios ni filtraciones.

**Independent Test**: Inputs invalidos y NotFound conservan Ready; fallos de config/conexion/licencia/catalogo bloquean; deactivate/cierre cancela; una respuesta antigua nunca publica; logs/mensajes no contienen centinelas.

### Tests for User Story 3

- [X] T033 [P] [US3] Escribir primero pruebas fallidas que distingan MissingInput/InputTooLong/NotFound no bloqueantes de OperationalFailure de catalogo bloqueante, invalidacion de sesion y Retry requerido en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierFailureTests.cs`
- [X] T034 [P] [US3] Escribir primero pruebas fallidas de cancelacion en Prepare/Lookup, Deactivate, reactivacion y respuesta no cooperativa antigua completada despues de una nueva usando epochs y `TaskCompletionSource` en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierLifecycleTests.cs`
- [X] T035 [P] [US3] Escribir primero auditoria fallida con centinelas para barcode, descripcion, host, ruta, usuario, password, licencia y excepcion, revisando mensajes y propiedades de log estructurado en `tests/SysTools.BusinessRules.Tests/PriceVerifier/PriceVerifierSecurityTests.cs` y `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierSecurityTests.cs`

### Implementation for User Story 3

- [X] T036 [US3] Completar traduccion de fallos esperados, invalidacion solo del epoch vigente, cancelacion propagada y logs Stage/Status/Category/DurationMs sin payload en `src/Business/PriceVerifier/PriceVerifierWorkflow.cs`
- [X] T037 [US3] Completar limpieza segura, bloqueo solo operacional, Retry, cancelacion/generation guards y cero publicacion/log de error por cancelacion en `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs`
- [X] T038 [US3] Desactivar el lifecycle activo al cerrar la shell sin ejecutar trabajo async no observado en `src/Presentation/Shell/Views/ShellWindow.xaml.cs`
- [X] T039 [US3] Ejecutar pruebas Failure/Lifecycle/Security y documentar estados, cancelacion, carreras y cero filtraciones en `specs/008-price-verifier-ui/validation/us3-recovery-security.md`

**Checkpoint**: Ningun fallo recuperable cierra la shell, deja datos obsoletos o filtra entrada/secretos.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Accesibilidad, layout, arquitectura, alcance, integraciones controladas y regresion.

- [X] T040 [P] Agregar smoke WPF STA a 1280x720 para controles esenciales visibles/dentro del viewport, textos/AutomationProperties y estados no dependientes solo de color en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierAccessibilityTests.cs`
- [X] T041 [P] Extender guards para prohibir Data/Firebird/IO/criptografia/ZXing/reportes/impresion/search/labels en View/ViewModel y mantener controles futuros deshabilitados en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`
- [X] T042 [P] Auditar que no se agregaron paquetes/proyectos/Data y registrar dependencias, confinamiento y superficie de DI en `specs/008-price-verifier-ui/validation/dependency-architecture-audit.md`
- [X] T043 Ejecutar restore, build sin warnings y regresion completa; corregir regresiones y registrar conteos/omisiones en `specs/008-price-verifier-ui/validation/full-regression.md`
- [X] T044 Ejecutar integraciones Firebird/repositorios existentes solo con entorno autorizado y registrar PASS o SKIPPED sin secretos en `specs/008-price-verifier-ui/validation/controlled-integration.md`
- [X] T045 Ejecutar el recorrido manual de `quickstart.md` a 1280x720, maximizado y 125% DPI, incluyendo teclado/lector/foco/Retry, y registrar PASS/FAIL/SKIPPED en `specs/008-price-verifier-ui/validation/manual-wpf.md`
- [X] T046 Ejecutar todos los escenarios automatizados de `specs/008-price-verifier-ui/quickstart.md` y registrar comandos/resultados en `specs/008-price-verifier-ui/validation/quickstart-results.md`
- [X] T047 Auditar FR-001..FR-025, SC-001..SC-010 y gates constitucionales contra pruebas/artefactos en `specs/008-price-verifier-ui/validation/constitution-traceability.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: inicia de inmediato.
- **Foundational**: depende de Setup y bloquea las tres historias.
- **US1**: depende de Foundational y crea la sesion Ready/bloqueada.
- **US2**: depende de US1 porque Lookup requiere una sesion Ready; sus pruebas usan dobles para aislar la consulta.
- **US3**: depende de US1/US2 para endurecer el flujo completo.
- **Polish**: depende de las tres historias completas.

### User Story Dependencies

- **US1 (P1)**: independiente despues de Foundational; entrega preparacion y Retry.
- **US2 (P1)**: reutiliza la sesion de US1; junto con US1 constituye el MVP funcional.
- **US3 (P2)**: endurece US1/US2 con fallos, cancelacion, carreras y seguridad.

### Within Each User Story

- Escribir y ejecutar pruebas rojas antes del codigo correspondiente.
- Entities/contratos preceden al workflow; workflow precede al ViewModel; ViewModel precede a bindings visuales.
- Cambios en `PriceVerifierWorkflow.cs`, `PriceVerifierViewModel.cs` y View/code-behind son secuenciales.
- Cada checkpoint debe quedar verde antes de avanzar.

### Parallel Opportunities

- T002-T003, T004/T009, T012-T015, T022-T026, T033-T035 y T040-T042 pueden avanzar en paralelo cuando sus prerrequisitos esten completos.
- Pruebas Business y Presentation de una historia pueden escribirse en paralelo.
- Las auditorias de arquitectura/dependencias pueden ejecutarse mientras se prepara evidencia visual.

---

## Parallel Example: User Story 1

```text
Task T012: matriz Business de configuracion/conexion/licencia
Task T013: activacion/Retry del ViewModel
Task T014: coherencia de estados de shell
Task T015: composicion DI
```

## Parallel Example: User Story 2

```text
Task T022: contrato Lookup y limites
Task T023: matriz de 50 productos
Task T024: single-flight y publicacion del ViewModel
Task T025: 100 consultas secuenciales
Task T026: bindings, Enter y foco WPF STA
```

## Parallel Example: User Story 3

```text
Task T033: fallos no bloqueantes vs operativos
Task T034: cancelacion, deactivate y respuestas antiguas
Task T035: auditoria de privacidad con centinelas
```

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Completar Setup y Foundational.
2. Completar US1 y validar preparacion/Retry.
3. Completar US2 y validar consulta exacta/teclado/foco.
4. Detenerse y demostrar el verificador operativo antes del endurecimiento US3.

### Incremental Delivery

1. US1 entrega disponibilidad segura del modulo.
2. US2 agrega consulta exacta y completa el MVP.
3. US3 agrega recuperacion, cancelacion, carreras y privacidad.
4. Polish cierra accesibilidad, layout, integracion, regresion y trazabilidad.

## Notes

- Marcar `[X]` solo despues de ejecutar y verificar cada tarea.
- No registrar codigo, Product, configuracion, licencia, credenciales ni excepciones contaminadas.
- No agregar paquetes, proyectos o codigo Data sin replanificar.
- No habilitar busqueda, configuracion visual, barcode visible, etiquetas, reportes o impresion.
- Las integraciones externas omitidas se documentan como SKIPPED; no se inventa evidencia operativa.
