---

description: "Implementation tasks for Firebird connection and connectivity testing"
---

# Tasks: Conexión y prueba Firebird

**Input**: Design documents from `/specs/003-firebird-connection/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [connection-contract.md](contracts/connection-contract.md), [quickstart.md](quickstart.md)

**Tests**: Esta feature crítica requiere pruebas unitarias, de ciclo de vida, seguridad, arquitectura e integración controlada. En cada historia, escribir primero las pruebas indicadas y confirmar que fallan por la capacidad ausente antes de implementar.

**Organization**: Las tareas se agrupan por historia para entregar primero conectividad exitosa, luego fallos controlados y finalmente cancelación/diagnóstico seguro.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo porque trabaja en archivos distintos y no depende de otra tarea incompleta.
- **[Story]**: Historia de usuario atendida (`US1`, `US2`, `US3`).
- Cada tarea incluye la ruta exacta que debe crear o modificar.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Incorporar el proveedor aprobado y preparar una suite aislada para la feature.

- [X] T001 [P] Agregar `FirebirdSql.Data.FirebirdClient` versión `10.3.4` únicamente a `src/Data/SysTools.Data.csproj`, conservando `net10.0` y sin agregar EF Core
- [X] T002 [P] Crear `tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj` para `net10.0-windows` con xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, coverlet 6.0.4 y referencias a Entities, Business y Data
- [X] T003 Agregar `tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj` a `SysTools.sln` después de completar T001 y T002

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Crear los tipos y contratos compartidos que bloquean todas las historias.

**⚠️ CRITICAL**: Ninguna historia puede implementarse hasta completar esta fase.

- [X] T004 [P] Crear `ConnectionTestStatus` con exactamente `Success`, `InvalidConfiguration`, `AuthenticationFailed`, `ServerUnavailable`, `DatabaseUnavailable`, `Timeout`, `Canceled` y `UnexpectedFailure` en `src/Entities/Connection/ConnectionTestStatus.cs`
- [X] T005 [P] Crear el tipo inmutable `ConnectionConfigurationIssue` en `src/Entities/Connection/ConnectionConfigurationIssue.cs`; `Field` solo identifica `ipEmpresa`, `rutaEmpresa`, `usuario` o `password`, y `Message` nunca copia el valor recibido
- [X] T006 [P] Crear `ConnectionTestResult` inmutable en `src/Entities/Connection/ConnectionTestResult.cs`: `Status` es exactamente uno de los ocho estados, `Message` usa texto controlado, `Duration` es no negativa, `Issues` nunca es nulo y solo contiene uno o más elementos para `InvalidConfiguration`; los demás estados usan lista vacía y no existen propiedades para configuración, conexión, cadena o excepción
- [X] T007 [P] Definir `IConnectionTestService.TestAsync(AppConfiguration, CancellationToken)` en `src/Business/Connection/IConnectionTestService.cs` conforme al contrato Business
- [X] T008 [P] Definir `IFirebirdConnectionProbe.ProbeAsync(AppConfiguration, CancellationToken)` con retorno `ConnectionTestStatus` en `src/Business/Connection/IFirebirdConnectionProbe.cs` sin tipos Firebird
- [X] T009 [P] Definir `IFirebirdConnectionFactory.Create(AppConfiguration)` con retorno `DbConnection` y propiedad exclusiva del llamador en `src/Data/Connection/IFirebirdConnectionFactory.cs`
- [X] T010 [P] Crear constantes internas de puerto `3050`, charset `ISO8859_1`, dialecto `3`, timeout `5` segundos y pooling habilitado en `src/Data/Connection/FirebirdConnectionDefaults.cs`
- [X] T011 [P] Crear probes, factories, `DbConnection` rastreable y logger en memoria sin secretos para pruebas en `tests/SysTools.Firebird.Tests/TestDoubles/FirebirdTestDoubles.cs`

**Checkpoint**: Los contratos compilan sin referencias Firebird desde Entities o Business y las historias pueden comenzar.

---

## Phase 3: User Story 1 - Confirmar una configuración válida (Priority: P1) 🎯 MVP

**Goal**: Crear, abrir y cerrar una conexión independiente desde configuración válida, devolviendo éxito y duración sin exigir persistencia previa.

**Independent Test**: Con una configuración válida y un probe/factory controlado, el resultado es `Success`, los cuatro valores llegan sin modificación, cada solicitud usa una conexión distinta y cada recurso se dispone una vez; con entorno Firebird autorizado, 10 de 10 intentos reales completan dentro de 6 segundos.

### Tests for User Story 1

- [X] T012 [P] [US1] Escribir pruebas fallidas de los ocho estados, duración no negativa, issues no nulos e invariantes de resultado en `tests/SysTools.Firebird.Tests/Entities/ConnectionTestResultTests.cs`
- [X] T013 [P] [US1] Escribir pruebas fallidas de `FirebirdConnectionFactory` que verifiquen mapeo exacto de los cuatro valores, puerto 3050, charset `ISO8859_1`, dialecto 3, timeout 5, pooling y una instancia nueva/cerrada por llamada en `tests/SysTools.Firebird.Tests/Data/FirebirdConnectionFactoryTests.cs`
- [X] T014 [P] [US1] Escribir pruebas fallidas de apertura exitosa, propagación del token y disposición exactamente una vez en `tests/SysTools.Firebird.Tests/Data/FirebirdConnectionProbeSuccessTests.cs`
- [X] T015 [P] [US1] Escribir pruebas fallidas de `ConnectionTestService` para configuración guardada o no guardada, resultado `Success`, mensaje seguro, duración e input sin modificación en `tests/SysTools.Firebird.Tests/Business/ConnectionTestServiceSuccessTests.cs`
- [X] T016 [P] [US1] Crear la prueba opt-in de 10 conexiones reales consecutivas, límite menor de 6 segundos y omisión explícita sin variables requeridas en `tests/SysTools.Firebird.Tests/Integration/FirebirdConnectionSuccessIntegrationTests.cs`

### Implementation for User Story 1

- [X] T017 [P] [US1] Implementar `FirebirdConnectionFactory` con `FbConnectionStringBuilder` y el mapeo/constantes exactos, sin logging ni retención de configuración/cadena, en `src/Data/Connection/FirebirdConnectionFactory.cs`
- [X] T018 [US1] Implementar el camino de apertura exitosa con `OpenAsync`, token enlazado de 5 segundos y `await using` en `src/Data/Connection/FirebirdConnectionProbe.cs`, dependiendo de T017
- [X] T019 [P] [US1] Implementar el camino exitoso, medición de duración, mensaje `La conexión con Firebird se estableció correctamente.` y evento estructurado de inicio/fin en `src/Business/Connection/ConnectionTestService.cs`
- [X] T020 [US1] Registrar `IFirebirdConnectionFactory`, `IFirebirdConnectionProbe` e `IConnectionTestService` como servicios stateless en `src/Presentation/App.xaml.cs` sin abrir red durante startup
- [X] T021 [US1] Agregar pruebas de resolución DI, ausencia de apertura automática y shell operativa sin servidor en `tests/SysTools.Presentation.Tests/Composition/FirebirdDependencyInjectionTests.cs`
- [X] T022 [US1] Ejecutar la suite US1 con dobles y, si existe entorno autorizado, la integración de éxito; registrar comandos, conteos y cualquier omisión en `specs/003-firebird-connection/validation/us1-success.md`

**Checkpoint**: La aplicación dispone de una fábrica reutilizable y puede demostrar conectividad válida como MVP sin UI nueva ni consultas.

---

## Phase 4: User Story 2 - Entender por qué no fue posible conectar (Priority: P2)

**Goal**: Rechazar datos incompletos antes de Data y traducir autenticación, servidor, base, timeout y fallos desconocidos a estados y mensajes controlados.

**Independent Test**: Configuración con varios campos vacíos crea cero conexiones y devuelve todos los issues; códigos conocidos producen la categoría exacta, texto localizado distinto no cambia el resultado y cualquier señal ambigua retorna `UnexpectedFailure` sin cerrar la shell ni modificar configuración.

### Tests for User Story 2

- [X] T023 [P] [US2] Escribir pruebas fallidas de validación conjunta usando `string.IsNullOrWhiteSpace`, orden `ipEmpresa`, `rutaEmpresa`, `usuario`, `password`, no mutación y no bloqueo por catálogos ajenos en `tests/SysTools.Firebird.Tests/Business/ConnectionConfigurationValidatorTests.cs`
- [X] T024 [P] [US2] Escribir pruebas fallidas de precedencia y códigos exactos de autenticación `335544472|335545106`, red `335544421|335544721|335544726|335544727`, base `335544323|335544344|335544375|335544379` y fallback desconocido en `tests/SysTools.Firebird.Tests/Data/FirebirdErrorClassifierTests.cs`
- [X] T025 [P] [US2] Escribir pruebas fallidas para `TimeoutException`, deadline interno, excepciones/códigos conocidos y desconocidos, y disposición única en todos los fallos en `tests/SysTools.Firebird.Tests/Data/FirebirdConnectionProbeFailureTests.cs`
- [X] T026 [P] [US2] Escribir pruebas fallidas de los mensajes seguros por estado, lista completa de faltantes, cero invocaciones al probe en configuración inválida y configuración intacta tras fallos en `tests/SysTools.Firebird.Tests/Business/ConnectionTestServiceFailureTests.cs`
- [X] T027 [P] [US2] Crear pruebas opt-in de password inválido y base inexistente autorizados, con fallback cuando el proveedor no diferencie la causa, en `tests/SysTools.Firebird.Tests/Integration/FirebirdConnectionFailureIntegrationTests.cs`

### Implementation for User Story 2

- [X] T028 [P] [US2] Implementar `ConnectionConfigurationValidator` para devolver todos los issues seguros en orden estable sin recortar ni modificar valores en `src/Business/Connection/ConnectionConfigurationValidator.cs`
- [X] T029 [P] [US2] Implementar `FirebirdErrorClassifier` por tipos y colección numérica `FbException.Errors`, sin comparar mensajes localizados ni devolver excepciones, en `src/Data/Connection/FirebirdErrorClassifier.cs`
- [X] T030 [US2] Completar `FirebirdConnectionProbe` con distinción de deadline, extracción de códigos, clasificación conservadora y `UnexpectedFailure` para señales ambiguas en `src/Data/Connection/FirebirdConnectionProbe.cs`, dependiendo de T029
- [X] T031 [US2] Completar `ConnectionTestService` con validación previa, cero llamadas Data al fallar, catálogo exacto de mensajes y resultados controlados para los ocho estados en `src/Business/Connection/ConnectionTestService.cs`, dependiendo de T028 y T030
- [X] T032 [US2] Registrar `ConnectionConfigurationValidator` sin cambiar el ciclo de vida stateless de los servicios en `src/Presentation/App.xaml.cs`
- [X] T033 [US2] Agregar una prueba de composición que confirme que cada fallo es recuperable y no impide resolver/usar la shell en `tests/SysTools.Presentation.Tests/Composition/FirebirdFailureIsolationTests.cs`
- [X] T034 [US2] Ejecutar los filtros US2, documentar categorías, códigos probados, fallback y estado de fallos reales autorizados en `specs/003-firebird-connection/validation/us2-controlled-failures.md`

**Checkpoint**: Todas las causas confiables tienen salida estable y las ambiguas nunca producen un diagnóstico inventado.

---

## Phase 5: User Story 3 - Cancelar y diagnosticar sin exponer secretos (Priority: P3)

**Goal**: Cancelar intentos, aislar solicitudes concurrentes y producir diagnósticos útiles sin exponer configuración o secretos.

**Independent Test**: Una apertura pendiente cancelada retorna `Canceled` y se dispone; si cancelación y deadline coinciden gana `Canceled`; 100 intentos mixtos disponen exactamente una conexión y dos solicitudes concurrentes no comparten estado; ningún token centinela aparece en resultados o logs.

### Tests for User Story 3

- [X] T035 [P] [US3] Escribir pruebas fallidas de cancelación previa/durante apertura, precedencia sobre deadline y disposición única en `tests/SysTools.Firebird.Tests/Data/FirebirdConnectionCancellationTests.cs`
- [X] T036 [P] [US3] Escribir pruebas fallidas de dos solicitudes concurrentes con resultados y ciclos de vida independientes en `tests/SysTools.Firebird.Tests/Data/FirebirdConnectionConcurrencyTests.cs`
- [X] T037 [P] [US3] Escribir la prueba de 100 éxitos, fallos y cancelaciones que exige exactamente una disposición por conexión en `tests/SysTools.Firebird.Tests/Data/FirebirdConnectionLifecycleTests.cs`
- [X] T038 [P] [US3] Escribir pruebas fallidas con password, licencia, host, ruta y cadena centinela que auditen resultados/eventos y prohíban excepción cruda, destructuring o payload en `tests/SysTools.Firebird.Tests/Security/FirebirdConnectionLoggingTests.cs`

### Implementation for User Story 3

- [X] T039 [US3] Implementar precedencia de cancelación del consumidor, resultado `Canceled` y cierre determinista frente a carrera con timeout en `src/Data/Connection/FirebirdConnectionProbe.cs`
- [X] T040 [US3] Eliminar cualquier estado compartido por solicitud y asegurar que factory/probe toleren concurrencia sin compartir conexiones en `src/Data/Connection/FirebirdConnectionFactory.cs` y `src/Data/Connection/FirebirdConnectionProbe.cs`
- [X] T041 [P] [US3] Limitar logs Business a operación, outcome, duración, issue fields/count y eventos `ConnectionTestStarted|ConnectionValidationFailed|ConnectionTestCompleted` en `src/Business/Connection/ConnectionTestService.cs`
- [X] T042 [US3] Limitar logs Data a outcome, nombre de tipo y códigos numéricos en `src/Data/Connection/FirebirdConnectionProbe.cs`, sin pasar `Exception`, `Message`, configuración, host, ruta, usuario o cadena al logger
- [X] T043 [US3] Ejecutar filtros de cancelación, concurrencia, lifecycle y seguridad; registrar los 100 casos, carrera y búsqueda de centinelas en `specs/003-firebird-connection/validation/us3-cancellation-security.md`

**Checkpoint**: Las tres historias funcionan con recursos aislados, cancelación determinista y diagnóstico seguro.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verificar arquitectura, regresiones, seguridad, alcance y evidencia operacional de toda la feature.

- [X] T044 [P] Ampliar reglas arquitectónicas para prohibir `FirebirdSql`, `FbConnection` y Data Connection desde Entities, Business, Views y ViewModels en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`
- [X] T045 [P] Sustituir el placeholder por la descripción de la fábrica, propiedad de conexiones y límites de la feature en `src/Data/Connection/README.md`
- [X] T046 [P] Auditar ausencia de SQL, productos/fecha, licencia funcional, UI, reintentos, puerto configurable, EF Core y conexión global; registrar comandos/resultados en `specs/003-firebird-connection/validation/scope-audit.md`
- [X] T047 [P] Auditar referencias de paquete/capa, patrones de logging y tokens centinela; registrar cero secretos y excepciones crudas en `specs/003-firebird-connection/validation/security-architecture-audit.md`
- [X] T048 Ejecutar restore, build y todas las pruebas Debug sin warnings; registrar versiones y conteos por suite en `specs/003-firebird-connection/validation/build-and-tests.md`
- [X] T049 Ejecutar pruebas Release, incluyendo 100 casos lifecycle y concurrencia, y registrar tiempos/resultados en `specs/003-firebird-connection/validation/release-lifecycle.md`
- [X] T050 Ejecutar integración real solo con variables autorizadas; documentar 10 éxitos, fallos disponibles o el bloqueo operativo explícito sin copiar datos del entorno en `specs/003-firebird-connection/validation/firebird-integration.md`
- [X] T051 Ejecutar de principio a fin `specs/003-firebird-connection/quickstart.md` y registrar cada paso PASS, FAIL o SKIPPED con razón en `specs/003-firebird-connection/validation/quickstart-results.md`
- [X] T052 Revalidar todos los gates de la constitución, incluyendo el diferimiento justificado de datos de producto/encoding a repositorios, en `specs/003-firebird-connection/validation/constitution-compliance.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias; T001 y T002 pueden avanzar en paralelo, T003 depende de ambas.
- **Foundational (Phase 2)**: Depende de Setup y bloquea todas las historias.
- **User Story 1 (Phase 3)**: Depende de Foundational y entrega el MVP reutilizable.
- **User Story 2 (Phase 4)**: Depende del núcleo de US1; sus pruebas nuevas pueden escribirse en paralelo antes de completar la clasificación.
- **User Story 3 (Phase 5)**: Depende del núcleo de US1, no de las decisiones funcionales de US2; coordinar ediciones a `FirebirdConnectionProbe.cs` si US2 y US3 avanzan simultáneamente.
- **Polish (Phase 6)**: Depende de las historias que se vayan a entregar; para cerrar la feature deben completarse US1, US2 y US3.

### User Story Dependency Graph

```text
Setup → Foundational → US1 (MVP)
                           ├─→ US2 (fallos controlados) ─┐
                           └─→ US3 (cancelación/seguridad) ─┤
                                                          └─→ Polish
```

### Within Each User Story

- Escribir y observar fallar las pruebas antes de su implementación correspondiente.
- Tipos/contratos antes de servicios.
- Factory antes de probe; probe/validator antes de coordinación Business.
- Implementación antes de DI y pruebas de composición.
- Validar el checkpoint antes de avanzar a la siguiente prioridad.

### Parallel Opportunities

- T001 y T002 pueden ejecutarse en paralelo.
- T004–T011 trabajan en archivos distintos y pueden prepararse en paralelo tras Setup.
- En US1, T012–T016 pueden escribirse en paralelo; T017 y T019 también pueden implementarse en paralelo.
- En US2, T023–T027 y luego T028–T029 son pares paralelizables.
- En US3, T035–T038 pueden escribirse en paralelo; T041 puede avanzar separado de los cambios Data.
- T044–T047 pueden auditar/documentar áreas distintas en paralelo después de estabilizar el código.

---

## Parallel Example: User Story 1

```text
Task T012: Probar entidades en tests/SysTools.Firebird.Tests/Entities/ConnectionTestResultTests.cs
Task T013: Probar builder/factory en tests/SysTools.Firebird.Tests/Data/FirebirdConnectionFactoryTests.cs
Task T014: Probar apertura/disposición en tests/SysTools.Firebird.Tests/Data/FirebirdConnectionProbeSuccessTests.cs
Task T015: Probar coordinación Business en tests/SysTools.Firebird.Tests/Business/ConnectionTestServiceSuccessTests.cs
Task T016: Preparar integración opt-in en tests/SysTools.Firebird.Tests/Integration/FirebirdConnectionSuccessIntegrationTests.cs
```

## Parallel Example: User Story 2

```text
Task T023: Probar validación en tests/SysTools.Firebird.Tests/Business/ConnectionConfigurationValidatorTests.cs
Task T024: Probar códigos en tests/SysTools.Firebird.Tests/Data/FirebirdErrorClassifierTests.cs
Task T025: Probar fallos del probe en tests/SysTools.Firebird.Tests/Data/FirebirdConnectionProbeFailureTests.cs
Task T026: Probar resultados Business en tests/SysTools.Firebird.Tests/Business/ConnectionTestServiceFailureTests.cs
Task T027: Preparar fallos reales opt-in en tests/SysTools.Firebird.Tests/Integration/FirebirdConnectionFailureIntegrationTests.cs
```

## Parallel Example: User Story 3

```text
Task T035: Probar cancelación en tests/SysTools.Firebird.Tests/Data/FirebirdConnectionCancellationTests.cs
Task T036: Probar concurrencia en tests/SysTools.Firebird.Tests/Data/FirebirdConnectionConcurrencyTests.cs
Task T037: Probar 100 ciclos en tests/SysTools.Firebird.Tests/Data/FirebirdConnectionLifecycleTests.cs
Task T038: Probar no filtración en tests/SysTools.Firebird.Tests/Security/FirebirdConnectionLoggingTests.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Setup.
2. Completar Foundational.
3. Escribir pruebas US1 y confirmar el estado rojo esperado.
4. Implementar la fábrica, probe, servicio y DI de US1.
5. Ejecutar el checkpoint y registrar `validation/us1-success.md`.
6. Detenerse aquí permite demostrar conectividad válida sin mezclar fallos avanzados o UI.

### Incremental Delivery

1. **US1**: conexión válida independiente y reusable.
2. **US2**: validación y categorías de fallo controladas.
3. **US3**: cancelación, concurrencia y diagnóstico seguro.
4. **Polish**: regresión completa, auditorías y evidencia real/pendiente.

### Parallel Team Strategy

Después de US1, una línea puede preparar pruebas/validador/clasificador de US2 y otra pruebas de cancelación/seguridad de US3. Las modificaciones a `FirebirdConnectionProbe.cs` deben integrarse de forma secuencial para evitar conflictos y preservar precedencias.

## Notes

- `[P]` significa archivo distinto y ausencia de dependencia pendiente.
- Cada historia conserva criterio de prueba independiente y un checkpoint documentado.
- No registrar ni imprimir valores de variables de integración.
- No agregar consultas, UI, reintentos, puertos configurables ni conexión global.
- Una integración omitida puede documentarse, pero no permite afirmar validación operativa completa.
- Marcar cada checkbox solo después de verificar su resultado y archivo asociado.

---

## Phase 7: Convergence

**Purpose**: Cerrar brechas de inmutabilidad y cobertura arquitectónica/integrada detectadas al contrastar la implementación terminada con los artefactos de la feature.

- [X] T053 [P] Hacer que `ConnectionTestResult.Issues` conserve una copia realmente inmutable y agregar una prueba que intente modificar la colección/entrada original en `src/Entities/Connection/ConnectionTestResult.cs` y `tests/SysTools.Firebird.Tests/Entities/ConnectionTestResultTests.cs` per T006 y `data-model.md: ConnectionTestResult` (partial)
- [X] T054 [P] Ampliar `LayerDependencyTests` para prohibir explícitamente el namespace y `ProjectReference` de Data desde Entities/Business, además de Views/ViewModels, en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs` per T044 y Constitution I (partial)
- [X] T055 [P] Agregar una prueba puente que cargue una `AppConfiguration` recuperada por `IConfigurationService` con password en memoria y la entregue sin modificación a `ConnectionTestService` en `tests/SysTools.Firebird.Tests/Business/SavedConfigurationConnectionTests.cs` per US1/AC2 y T015 (partial)
