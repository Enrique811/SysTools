# Tasks: Repositorios de productos y fecha del servidor

**Input**: Design documents from `/specs/004-product-repositories-server-clock/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Esta feature toca Firebird y la especificacion exige matrices automatizadas, lifecycle, seguridad e integracion opt-in; se aplica TDD por historia.

**Organization**: Las tareas se agrupan por historia para conservar incrementos probables e independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo por usar archivos distintos y no depender de trabajo incompleto.
- **[Story]**: Historia de usuario cubierta.
- Cada descripcion incluye rutas concretas.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar la suite de feature y su inclusion en la solucion.

- [X] T001 Crear `tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj` con xUnit y referencias a Entities, Business y Data
- [X] T002 Agregar `tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj` a `SysTools.sln` despues de T001
- [X] T003 [P] Crear la estructura `Entities`, `Contracts`, `Data`, `Integration`, `Security` y `TestDoubles` bajo `tests/SysTools.Repositories.Tests/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Definir modelo, contratos, fallo seguro y dobles reutilizados por las tres historias.

**CRITICAL**: Ninguna historia se implementa antes de estabilizar estos contratos.

### Tests for foundational contracts

- [X] T004 [P] Escribir pruebas de `Product`: `Id` positivo; `Barcode` no vacio y maximo 50; `Description` no nula y maximo 255; `Stock` no vacio; inmutabilidad y `ToString` seguro en `tests/SysTools.Repositories.Tests/Entities/ProductTests.cs`
- [X] T005 [P] Escribir pruebas de `RepositoryOperation` y `RepositoryAccessException` que exijan operacion valida, mensaje catalogado, cero inner exception y ausencia de payload en `tests/SysTools.Repositories.Tests/Contracts/RepositoryAccessExceptionTests.cs`

### Implementation for foundational contracts

- [X] T006 Implementar `Product` inmutable con `int Id`, `string Barcode`, `string Description`, `string? Presentation`, `decimal? PriceWithTax` y `string Stock` en `src/Entities/Products/Product.cs` despues de T004
- [X] T007 [P] Implementar `RepositoryOperation` con `GetProductByBarcode`, `SearchProductsByDescription` y `GetServerTimestamp` en `src/Business/Repositories/RepositoryOperation.cs`
- [X] T008 Implementar `RepositoryAccessException` sin inner exception ni payload y con mensajes seguros por operacion en `src/Business/Repositories/RepositoryAccessException.cs` despues de T005 y T007
- [X] T009 [P] Definir `IProductRepository` con consulta exacta y busqueda por prefijo cancelables en `src/Business/Repositories/IProductRepository.cs`
- [X] T010 [P] Definir `IServerClockRepository.GetCurrentAsync` cancelable en `src/Business/Repositories/IServerClockRepository.cs`
- [X] T011 Implementar conexiones, comandos, parametros y lectores ADO.NET rastreables para pruebas en `tests/SysTools.Repositories.Tests/TestDoubles/RepositoryTestDoubles.cs`
- [X] T012 [P] Implementar logger capturable y configuracion centinela sin imprimir secretos en `tests/SysTools.Repositories.Tests/TestDoubles/RepositoryLoggingDoubles.cs`
- [X] T013 Ejecutar pruebas de contratos y confirmar el checkpoint foundational en `specs/004-product-repositories-server-clock/validation/foundational-contracts.md`

**Checkpoint**: Modelo, interfaces, excepcion y dobles estan listos sin tipos Firebird fuera de Data.

---

## Phase 3: User Story 1 - Consultar un producto por codigo de barras (Priority: P1) MVP

**Goal**: Recuperar exactamente un producto o ausencia controlada, con nulos normalizados, parametros y lifecycle seguro.

**Independent Test**: Un codigo existente materializa todos los campos; uno inexistente devuelve `null`; un balance nulo produce `Sin registro`; fallo y cancelacion no se confunden con ausencia y liberan recursos.

### Tests for User Story 1

- [X] T014 [P] [US1] Escribir pruebas del mapper para ID, ceros iniciales, descripcion nula/255 caracteres con acentos, presentacion nula, precio decimal/nulo y stock numerico/nulo en `tests/SysTools.Repositories.Tests/Data/ProductDataReaderMapperTests.cs`
- [X] T015 [P] [US1] Escribir pruebas de consulta exacta para SQL de solo lectura, parametro `@barcode`, timeout 5, fila encontrada y `null` sin fila en `tests/SysTools.Repositories.Tests/Data/FirebirdProductRepositoryLookupTests.cs`
- [X] T016 [P] [US1] Escribir pruebas de codigo invalido, cancelacion en apertura/ejecucion/lectura, fallo seguro y disposicion unica en `tests/SysTools.Repositories.Tests/Data/FirebirdProductRepositoryFailureTests.cs`

### Implementation for User Story 1

- [X] T017 [US1] Implementar conversion tolerante a nulos y cultura invariante en `src/Data/Repositories/ProductDataReaderMapper.cs` despues de T014
- [X] T018 [US1] Implementar base SQL heredada y `GetByBarcodeAsync` parametrizado/cancelable en `src/Data/Repositories/FirebirdProductRepository.cs` despues de T015-T017
- [X] T019 [US1] Implementar traduccion de fallos y logging seguro por operacion, outcome, tipo y codigos numericos en `src/Data/Repositories/FirebirdRepositoryDiagnostics.cs` y conectarlo a `src/Data/Repositories/FirebirdProductRepository.cs`
- [X] T020 [US1] Registrar `IProductRepository` como servicio stateless sin abrir red durante resolucion en `src/Presentation/App.xaml.cs`
- [X] T021 [US1] Agregar prueba de composicion para `IProductRepository` y shell sin apertura de red en `tests/SysTools.Presentation.Tests/Composition/RepositoryDependencyInjectionTests.cs`
- [X] T022 [US1] Ejecutar el filtro US1 y registrar mapeo, parametros, ausencia, cancelacion y lifecycle en `specs/004-product-repositories-server-clock/validation/us1-product-lookup.md`

**Checkpoint**: La consulta exacta es utilizable como MVP sin UI ni reglas de formato.

---

## Phase 4: User Story 2 - Buscar productos por descripcion (Priority: P2)

**Goal**: Devolver coincidencias por prefijo, sin distinguir capitalizacion, ordenadas y seguras ante entrada vacia.

**Independent Test**: Un prefijo con mayusculas/acentos produce la coleccion ordenada esperada; sin coincidencias o con blanco devuelve coleccion vacia; un fallo permanece distinguible.

### Tests for User Story 2

- [X] T023 [P] [US2] Escribir pruebas de prefijo trimmeado con `%`, SQL `UPPER LIKE UPPER`, orden descripcion/ID y parametros sin concatenacion en `tests/SysTools.Repositories.Tests/Data/FirebirdProductRepositorySearchTests.cs`
- [X] T024 [P] [US2] Escribir pruebas de busqueda vacia sin conexion, cero filas, coleccion inmutable, cancelacion, fallo seguro y disposicion en `tests/SysTools.Repositories.Tests/Data/FirebirdProductRepositorySearchFailureTests.cs`

### Implementation for User Story 2

- [X] T025 [US2] Implementar `SearchByDescriptionAsync` parametrizado con prefijo `{texto}%`, orden estable y lista de solo lectura en `src/Data/Repositories/FirebirdProductRepository.cs` despues de T023-T024
- [X] T026 [US2] Ejecutar el filtro US2 y registrar prefijo, orden, vacios, caracteres especiales, cancelacion y fallo en `specs/004-product-repositories-server-clock/validation/us2-product-search.md`

**Checkpoint**: La busqueda prepara la UI posterior sin agregar controles visuales ni limitar resultados.

---

## Phase 5: User Story 3 - Obtener la fecha y hora confiable del servidor (Priority: P3)

**Goal**: Recuperar el timestamp Firebird exacto sin usar o convertir el reloj local.

**Independent Test**: Una respuesta conocida retorna el mismo `DateTime`; fila nula, fallo y cancelacion producen estados distinguibles y recursos liberados.

### Tests for User Story 3

- [X] T027 [P] [US3] Escribir pruebas de SQL `CURRENT_TIMESTAMP`, timestamp exacto, independencia del reloj local y timeout 5 en `tests/SysTools.Repositories.Tests/Data/FirebirdServerClockRepositoryTests.cs`
- [X] T028 [P] [US3] Escribir pruebas de valor/fila nulos, cancelacion, fallo seguro y disposicion unica en `tests/SysTools.Repositories.Tests/Data/FirebirdServerClockRepositoryFailureTests.cs`

### Implementation for User Story 3

- [X] T029 [US3] Implementar `FirebirdServerClockRepository.GetCurrentAsync` con `CURRENT_TIMESTAMP`, cancelacion y sin fallback local en `src/Data/Repositories/FirebirdServerClockRepository.cs` despues de T027-T028
- [X] T030 [US3] Registrar `IServerClockRepository` como servicio stateless en `src/Presentation/App.xaml.cs`
- [X] T031 [US3] Ampliar prueba de composicion para resolver reloj y shell sin apertura de red en `tests/SysTools.Presentation.Tests/Composition/RepositoryDependencyInjectionTests.cs`
- [X] T032 [US3] Ejecutar el filtro US3 y registrar exactitud, nulos, cancelacion, fallo y ausencia de reloj local en `specs/004-product-repositories-server-clock/validation/us3-server-clock.md`

**Checkpoint**: Las tres historias funcionan de forma independiente y exponen contratos listos para reglas de negocio y licencia.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validar concurrencia, seguridad, integracion, arquitectura, alcance y regresion completa.

- [X] T033 [P] Preparar integraciones opt-in de producto exacto, prefijo y timestamp con variables externas en `tests/SysTools.Repositories.Tests/Integration/FirebirdRepositoryIntegrationTests.cs` y `tests/SysTools.Repositories.Tests/Integration/RepositoryIntegrationFactAttribute.cs`
- [X] T034 [P] Agregar 100 operaciones mixtas y solicitudes concurrentes con conteo exacto de disposicion en `tests/SysTools.Repositories.Tests/Data/RepositoryLifecycleTests.cs`
- [X] T035 [P] Agregar auditoria de tokens centinela en excepciones/logs y prohibir configuracion, parametros, filas, SQL y excepcion cruda en `tests/SysTools.Repositories.Tests/Security/RepositoryLoggingTests.cs`
- [X] T036 [P] Ampliar reglas arquitectonicas para contratos/entidades/repositorios y SQL de solo lectura en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`
- [X] T037 Ejecutar restore, build y todas las pruebas Debug/Release sin warnings; registrar versiones y conteos en `specs/004-product-repositories-server-clock/validation/build-and-tests.md`
- [X] T038 Ejecutar auditorias de arquitectura, SQL, alcance y secretos; registrar resultados en `specs/004-product-repositories-server-clock/validation/security-architecture-scope.md`
- [X] T039 Ejecutar integracion real solo con entorno autorizado y registrar exito u omision explicita en `specs/004-product-repositories-server-clock/validation/firebird-integration.md`
- [X] T040 Ejecutar de principio a fin `specs/004-product-repositories-server-clock/quickstart.md` y registrar PASS/FAIL/SKIPPED en `specs/004-product-repositories-server-clock/validation/quickstart-results.md`
- [X] T041 Revalidar los gates constitucionales, incluidos datos nulos, acentos, longitudes, decimales y stock sin registro, en `specs/004-product-repositories-server-clock/validation/constitution-compliance.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: T001 y T003 pueden iniciar; T002 depende de T001.
- **Foundational**: Depende de Setup y bloquea historias; T004/T005 son pruebas rojas, T006-T010 implementan contratos, T011-T012 completan dobles.
- **US1**: Depende de Foundational y entrega el MVP.
- **US2**: Depende del repositorio y mapper creados por US1.
- **US3**: Depende de contratos/dobles foundational, no de US2; comparte diagnostico con US1.
- **Polish**: Depende de US1-US3 completas.

### User Story Dependency Graph

```text
Setup -> Foundational -> US1 (MVP) -> US2
                      `-> US3
US1 + US2 + US3 -> Polish
```

### Within Each User Story

- Escribir y observar fallar pruebas antes de la implementacion correspondiente.
- Modelo/contrato antes de Data; Data antes de DI; implementacion antes de evidencia.
- Cambios al mismo repositorio se realizan secuencialmente.
- Validar el checkpoint antes de avanzar.

### Parallel Opportunities

- T001 y T003; T004 y T005; T007, T009 y T010; T011 y T012.
- T014-T016; T023-T024; T027-T028.
- T033-T036 trabajan en archivos distintos tras estabilizar historias.

## Parallel Example: User Story 1

```text
Task T014: ProductDataReaderMapperTests.cs
Task T015: FirebirdProductRepositoryLookupTests.cs
Task T016: FirebirdProductRepositoryFailureTests.cs
```

## Parallel Example: User Story 2

```text
Task T023: FirebirdProductRepositorySearchTests.cs
Task T024: FirebirdProductRepositorySearchFailureTests.cs
```

## Parallel Example: User Story 3

```text
Task T027: FirebirdServerClockRepositoryTests.cs
Task T028: FirebirdServerClockRepositoryFailureTests.cs
```

## Implementation Strategy

### MVP First

1. Completar Setup y Foundational.
2. Escribir pruebas US1 y confirmar rojo.
3. Implementar mapper, consulta exacta, diagnostico y DI.
4. Validar US1 de forma independiente.

### Incremental Delivery

1. US1: consulta exacta y ausencia controlada.
2. US2: busqueda por prefijo sobre el mismo mapeo.
3. US3: reloj del servidor independiente.
4. Polish: concurrencia, seguridad, arquitectura, integracion y evidencia.

## Notes

- `[P]` significa archivo distinto y ausencia de dependencia pendiente.
- No registrar ni imprimir valores de configuracion, codigos o descripciones.
- Integracion omitida es evidencia de bloqueo, no validacion operacional.
- No agregar UI, reglas de precio, licencia, barcode, reportes, cache, paginacion o escrituras.
- Marcar `[X]` solo despues de verificar el archivo o resultado asociado.

---

## Phase 7: Convergence

**Purpose**: Cerrar brechas de validacion detectadas al contrastar la implementacion con los criterios medibles.

- [X] T042 [P] Agregar un guard automatizado que prohiba `DateTime.Now` y `DateTime.UtcNow` en los repositorios Firebird en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs` per SC-006 (partial)
- [X] T043 [P] Comparar producto exacto, resultados por prefijo y timestamp contra consultas Firebird de referencia independientes en `tests/SysTools.Repositories.Tests/Integration/FirebirdRepositoryIntegrationTests.cs` per SC-005 (partial)

---

## Phase 8: Convergence

**Purpose**: Eliminar documentacion de infraestructura que contradice el estado implementado.

- [X] T044 Actualizar `src/Data/Repositories/README.md` para describir contratos, implementaciones, propiedad de recursos y limites actuales per plan: source structure (contradicts)
