---

description: "Task list for price verifier business rules"
---

# Tasks: Reglas de negocio del verificador

**Input**: Design documents from `/specs/005-price-verifier-rules/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/business-rules-contract.md, quickstart.md

**Tests**: Las reglas Business son criticas segun la constitucion; las pruebas se escriben y ejecutan en rojo antes de cada implementacion.

**Organization**: Las tareas se agrupan por historia para conservar incrementos comprobables sin UI ni Firebird real.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo porque afecta archivos distintos y no depende de una tarea incompleta.
- **[Story]**: Historia funcional cubierta por la tarea.
- Todas las tareas incluyen rutas exactas.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Crear el arnes de pruebas dedicado sin cambiar dependencias de produccion.

- [X] T001 Crear `tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj` para `net10.0-windows` con xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, coverlet 6.0.4 y referencias solo a `src/Business/SysTools.Business.csproj` y `src/Entities/SysTools.Entities.csproj`
- [X] T002 Agregar `tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj` bajo la carpeta de solucion `tests` en `SysTools.sln`
- [X] T003 [P] Crear fabrica de productos de prueba con precios nulos/no nulos, codigos con ceros y texto Unicode en `tests/SysTools.BusinessRules.Tests/TestDoubles/ProductTestFactory.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Fijar dobles y restricciones arquitectonicas compartidas antes de implementar historias.

- [X] T004 Crear doble configurable de `IProductRepository` que cuente llamadas, capture argumentos y pueda devolver, cancelar o lanzar `RepositoryAccessException` en `tests/SysTools.BusinessRules.Tests/TestDoubles/ProductRepositoryStub.cs`
- [X] T005 [P] Escribir prueba arquitectonica roja que exija que Business/Entities no referencien Data, Presentation, WPF, Firebird ni reportes en `tests/SysTools.BusinessRules.Tests/Architecture/BusinessRulesDependencyTests.cs`

**Checkpoint**: El proyecto de pruebas compila una vez creados los contratos de cada historia y puede validar reglas sin infraestructura real.

---

## Phase 3: User Story 1 - Consultar productos sin depender de la pantalla (Priority: P1) MVP

**Goal**: Normalizar consultas vacias/no vacias sobre el repositorio y conservar resultados, orden, cancelacion y fallos seguros.

**Independent Test**: Con `ProductRepositoryStub`, entradas vacias hacen cero llamadas; entradas validas se delegan recortadas y resultados/excepciones se conservan.

### Tests for User Story 1

- [X] T006 [P] [US1] Escribir primero pruebas fallidas para configuracion nula, codigo nulo/vacio/whitespace, recorte exterior, ceros iniciales, producto encontrado e inexistente en `tests/SysTools.BusinessRules.Tests/Products/ProductServiceLookupTests.cs`
- [X] T007 [P] [US1] Escribir primero pruebas fallidas para busqueda vacia, prefijo recortado, orden e identidad de resultados en `tests/SysTools.BusinessRules.Tests/Products/ProductServiceSearchTests.cs`
- [X] T008 [P] [US1] Escribir primero pruebas fallidas que conserven `OperationCanceledException` y la misma `RepositoryAccessException` sin agregar payload en `tests/SysTools.BusinessRules.Tests/Products/ProductServiceFailureTests.cs`

### Implementation for User Story 1

- [X] T009 [P] [US1] Definir las dos operaciones asincronas y sus tipos nullable/cancellation exactos del contrato en `src/Business/Products/IProductService.cs`
- [X] T010 [US1] Implementar `ProductService` con validacion de configuracion, retornos tempranos, `Trim` exterior y delegacion transparente en `src/Business/Products/ProductService.cs`
- [X] T011 [US1] Ejecutar las pruebas de Products y documentar resultado, conteo y cobertura de FR-001..FR-006 en `specs/005-price-verifier-rules/validation/us1-product-service.md`

**Checkpoint**: US1 funciona con dobles y no requiere Data, WPF ni Firebird.

---

## Phase 4: User Story 2 - Mostrar precios compatibles con cada pais (Priority: P1)

**Goal**: Producir formatos `MX` y fallback `CO` deterministas bajo cualquier cultura activa.

**Independent Test**: Una tabla de importes se ejecuta bajo `es-MX`, `es-CO` y `en-US`; el resultado no cambia, usa `ToEven`, dos decimales para MX y cero para CO.

### Tests for User Story 2

- [X] T012 [P] [US2] Escribir primero pruebas fallidas para `MX` con cero, negativos, importes grandes, exactamente dos decimales y separador `.` en `tests/SysTools.BusinessRules.Tests/Pricing/PriceFormatterMxTests.cs`
- [X] T013 [P] [US2] Escribir primero pruebas fallidas para `CO`, fallback de null/vacio/minusculas/desconocido y cero decimales en `tests/SysTools.BusinessRules.Tests/Pricing/PriceFormatterCoTests.cs`
- [X] T014 [P] [US2] Escribir primero pruebas fallidas de `MidpointRounding.ToEven` y estabilidad al cambiar `CurrentCulture`/`CurrentUICulture` en `tests/SysTools.BusinessRules.Tests/Pricing/PriceFormatterCultureTests.cs`

### Implementation for User Story 2

- [X] T015 [P] [US2] Definir `Format(decimal amount, string? formatCode)` en `src/Business/Labels/IPriceFormatterService.cs`
- [X] T016 [US2] Implementar redondeo explicito `ToEven`, salida MX invariante y fallback moneda `es-CO` en `src/Business/Labels/PriceFormatterService.cs`
- [X] T017 [US2] Ejecutar las pruebas de Pricing con al menos 20 casos y documentar culturas, mitades y resultados en `specs/005-price-verifier-rules/validation/us2-price-formatting.md`

**Checkpoint**: US2 entrega precios reproducibles sin depender de la cultura del equipo.

---

## Phase 5: User Story 3 - Preparar etiquetas y completar filas (Priority: P2)

**Goal**: Capturar instantaneas inmutables y completar filas ordenadas de 1, 2 o 3 columnas sin residuos.

**Independent Test**: Secuencias de captura por cada capacidad producen pendientes base 1 hasta completar, luego una fila ordenada y estado vacio; entradas invalidas no mutan la cola.

### Tests for User Story 3

- [X] T018 [P] [US3] Escribir primero pruebas fallidas de `LabelData`: producto obligatorio con precio no nulo, precio formateado no whitespace, null informacion a vacio, preservacion exacta Unicode/espacios y `ToString` seguro en `tests/SysTools.BusinessRules.Tests/Labels/LabelDataTests.cs`
- [X] T019 [P] [US3] Escribir primero pruebas fallidas de `PendingLabel` y `LabelQueueResult` para posiciones 1..3, capacidades 1..3, exclusividad pending/completed, conteos e instantaneas no mutables en `tests/SysTools.BusinessRules.Tests/Labels/LabelQueueEntityTests.cs`
- [X] T020 [P] [US3] Escribir primero pruebas fallidas para captura de 1, 2 y 3 columnas, orden, posiciones, reinicio, capacidad invalida y cambio de capacidad sin mutacion en `tests/SysTools.BusinessRules.Tests/Labels/LabelQueueCaptureTests.cs`
- [X] T021 [P] [US3] Escribir primero prueba de 100 ciclos por capacidad, identidad exacta y menos de 5 ms p95 por operacion local en `tests/SysTools.BusinessRules.Tests/Labels/LabelQueueReliabilityTests.cs`

### Implementation for User Story 3

- [X] T022 [P] [US3] Implementar `LabelData` inmutable con `Product` requerido, `Product.PriceWithTax` no nulo, `FormattedPrice` no whitespace, `Information` null a `string.Empty` sin recortar y `ToString` sin payload en `src/Entities/Labels/LabelData.cs`
- [X] T023 [P] [US3] Implementar `PendingLabel` inmutable con `Position` exclusivamente 1..3 y `Data` no nulo en `src/Entities/Labels/PendingLabel.cs`
- [X] T024 [P] [US3] Implementar `LabelQueueResult` inmutable: `Capacity` solo 1..3, `Pending` y `Completed` mutuamente exclusivos, `Completed.Count == Capacity` al completar, copias de solo lectura y derivados `Remaining`/`IsComplete` en `src/Entities/Labels/LabelQueueResult.cs`
- [X] T025 [US3] Definir `Capture`, `Cancel` y `GetPending` segun el contrato en `src/Business/Labels/ILabelQueueService.cs`
- [X] T026 [US3] Implementar captura atomica bajo bloqueo, capacidad activa, posiciones base 1, snapshots, finalizacion y reinicio; dejar `Cancel` para US4 en `src/Business/Labels/LabelQueueService.cs`
- [X] T027 [US3] Ejecutar pruebas de captura/entidades/confiabilidad y documentar 100 ciclos por capacidad, inmutabilidad y p95 en `specs/005-price-verifier-rules/validation/us3-label-queue.md`

**Checkpoint**: US3 prepara datos y filas completas sin UI, motor de reportes ni estado residual.

---

## Phase 6: User Story 4 - Cancelar etiquetas pendientes (Priority: P3)

**Goal**: Descartar explicitamente una fila incompleta y reiniciar la siguiente captura en posicion 1.

**Independent Test**: Cancelar colas vacias y parciales devuelve 0/1/2, deja snapshot vacio y permite cambiar capacidad en la captura siguiente.

### Tests for User Story 4

- [X] T028 [US4] Escribir primero pruebas fallidas para cancelacion idempotente, conteo descartado, reinicio de posiciones, cambio posterior de capacidad e independencia de filas ya completadas en `tests/SysTools.BusinessRules.Tests/Labels/LabelQueueCancellationTests.cs`

### Implementation for User Story 4

- [X] T029 [US4] Implementar `Cancel` atomico que devuelve el conteo, vacia pendientes y capacidad activa sin afectar snapshots previos en `src/Business/Labels/LabelQueueService.cs`
- [X] T030 [US4] Ejecutar pruebas de cancelacion y documentar escenarios vacio/parcial/post-completado en `specs/005-price-verifier-rules/validation/us4-cancellation.md`

**Checkpoint**: Las cuatro historias son funcionales e independientemente verificables.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Integrar composicion, comprobar limites de arquitectura y cerrar la validacion completa.

- [X] T031 [P] Escribir primero prueba de composicion para resolucion singleton de `IProductService`, `IPriceFormatterService` e `ILabelQueueService` en `tests/SysTools.Presentation.Tests/Composition/BusinessRulesDependencyInjectionTests.cs`
- [X] T032 Registrar las tres interfaces e implementaciones como singleton, sin conectar aun el ViewModel placeholder, en `src/Presentation/App.xaml.cs`
- [X] T033 [P] Actualizar el alcance implementado y limites sin UI/impresion en `src/Business/Services/README.md`
- [X] T034 Ejecutar `dotnet build SysTools.sln --no-restore`, la prueba arquitectonica y la suite de `SysTools.BusinessRules.Tests`; registrar comandos y resultados en `specs/005-price-verifier-rules/validation/build-and-tests.md`
- [X] T035 Ejecutar `dotnet test SysTools.sln --no-build`, confirmar regresion de features 001-004 y registrar conteos/fallos omitidos en `specs/005-price-verifier-rules/validation/full-regression.md`
- [X] T036 Auditar FR-001..FR-021, SC-001..SC-007, constitucion, ausencia de secretos/payloads y limites de alcance en `specs/005-price-verifier-rules/validation/constitution-scope-audit.md`
- [X] T037 Ejecutar todos los pasos de `specs/005-price-verifier-rules/quickstart.md` y registrar el resultado reproducible en `specs/005-price-verifier-rules/validation/quickstart-results.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: inicia inmediatamente; T002 depende de T001.
- **Foundational (Phase 2)**: depende de T001-T003 y bloquea historias.
- **US1 (Phase 3)**: depende de T004; T006-T008 deben estar rojas antes de T009-T010.
- **US2 (Phase 4)**: depende del proyecto de pruebas; puede avanzar en paralelo con US1.
- **US3 (Phase 5)**: depende del proyecto y fabrica de productos; T018-T021 deben estar rojas antes de T022-T026.
- **US4 (Phase 6)**: depende de US3 y prueba T028 roja antes de T029.
- **Polish (Phase 7)**: depende de todas las historias implementadas.

### User Story Dependencies

- **US1**: independiente; solo consume `IProductRepository` existente.
- **US2**: independiente; regla pura sin estado.
- **US3**: independiente de US1/US2 al recibir `LabelData`, aunque el flujo real usa sus resultados.
- **US4**: depende del estado de cola de US3.

### Parallel Opportunities

- T003 y T005 pueden ejecutarse en paralelo despues de T001.
- T006-T008 son pruebas paralelizables; T009 puede crearse en paralelo antes de T010.
- T012-T014 son pruebas paralelizables; T015 puede crearse en paralelo antes de T016.
- T018-T021 son pruebas paralelizables; T022-T024 implementan archivos independientes en paralelo.
- T031 y T033 pueden ejecutarse en paralelo despues de las historias.

## Parallel Example: User Story 3

```text
Task: "Pruebas de LabelData en tests/SysTools.BusinessRules.Tests/Labels/LabelDataTests.cs"
Task: "Pruebas de entidades de cola en tests/SysTools.BusinessRules.Tests/Labels/LabelQueueEntityTests.cs"
Task: "Pruebas de captura en tests/SysTools.BusinessRules.Tests/Labels/LabelQueueCaptureTests.cs"
Task: "Pruebas de confiabilidad en tests/SysTools.BusinessRules.Tests/Labels/LabelQueueReliabilityTests.cs"
```

## Implementation Strategy

### MVP First

1. Completar Setup y Foundational.
2. Implementar US1 y validar consultas sin UI.
3. Implementar US2 y validar precios bajo culturas distintas.
4. Detenerse para demostrar consulta + presentacion de precio como MVP de reglas.

### Incremental Delivery

1. US1 entrega acceso Business a productos.
2. US2 agrega precio compatible.
3. US3 agrega preparacion y filas 1/2/3.
4. US4 agrega recuperacion por cancelacion.
5. Polish integra DI y ejecuta regresion total.

## Notes

- No crear UI funcional, barcode, licenciamiento, reportes ni impresion en 005.
- No modificar Data ni consultas SQL; la feature consume contratos existentes.
- No registrar ni incluir en excepciones codigo, descripcion, precio, informacion o configuracion.
- Marcar cada tarea `[X]` solo despues de comprobar su resultado.
