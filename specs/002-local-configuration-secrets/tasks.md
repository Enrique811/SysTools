---

description: "Dependency-ordered tasks for local configuration and protected secrets"
---

# Tasks: Configuración local y secretos

**Input**: Design documents from `/specs/002-local-configuration-secrets/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/configuration-contract.md`, `quickstart.md`

**Tests**: Esta feature requiere pruebas unitarias e integración reproducible porque persiste configuración y protege un secreto. En cada historia, las pruebas se escriben y ejecutan en rojo antes de implementar.

**Organization**: Las tareas se agrupan por historia para entregar primero defaults seguros, después round-trip protegido y finalmente recuperación ante fallos.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo porque toca archivos distintos y no depende de otra tarea incompleta inmediata.
- **[Story]**: Mantiene trazabilidad a `US1`, `US2` o `US3` de `spec.md`.
- Cada tarea incluye una ruta exacta.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar dependencias y el proyecto de pruebas de la feature.

- [X] T001 Crear `tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj` como xUnit `net10.0-windows` con las mismas versiones de test del repositorio, referencias a Entities/Business/Data, y agregarlo a `SysTools.sln`
- [X] T002 [P] Agregar `Microsoft.Extensions.Logging.Abstractions` 10.0.12 a `src/Business/SysTools.Business.csproj`
- [X] T003 [P] Agregar `Microsoft.Extensions.Logging.Abstractions` 10.0.12 y `System.Security.Cryptography.ProtectedData` 10.0.12 a `src/Data/SysTools.Data.csproj`
- [X] T004 Ejecutar restore/build de `SysTools.sln` y dejar la nueva suite vacía compilando antes de agregar comportamiento

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Definir tipos y contratos compartidos que bloquean las tres historias.

**⚠️ CRITICAL**: No iniciar implementación de historias hasta completar esta fase.

- [X] T005 [P] Crear `AppConfiguration` en `src/Entities/Configuration/AppConfiguration.cs` con los once campos no nulos y defaults exactos: IpEmpresa `""`, RutaEmpresa `""`, Usuario `"SYSDBA"`, Password `""`, Ambiente `"a"`, Impresora `""`, FormatoPrecio `"MX"`, Reporte `""`, Columnas `1`, Informacion `""`, Licencia `""`
- [X] T006 [P] Crear `ConfigurationIssueCode`, `ConfigurationIssueSeverity` y `ConfigurationIssue` sin valores sensibles en `src/Entities/Configuration/ConfigurationIssue.cs`
- [X] T007 [P] Crear `ConfigurationValidationResult` con `IsPersistable`, `IsConnectionReady` e Issues nunca nulo en `src/Entities/Configuration/ConfigurationValidationResult.cs`
- [X] T008 Crear estados y resultados tipados de carga, guardado, lectura y escritura descritos en `data-model.md` en `src/Entities/Configuration/ConfigurationOperationResults.cs`
- [X] T009 [P] Definir `IConfigurationRepository` con lectura/escritura async y cancelación en `src/Business/Configuration/IConfigurationRepository.cs`
- [X] T010 [P] Definir `IConfigurationService` con LoadAsync, Validate y SaveAsync según el contrato en `src/Business/Configuration/IConfigurationService.cs`
- [X] T011 [P] Definir seams `IConfigurationPathProvider`, `ISecretProtector` e `IAtomicFileWriter` en `src/Data/Configuration/ConfigurationInfrastructureContracts.cs`
- [X] T012 Crear fakes configurables de repositorio, path, protector, writer y logger capturable en `tests/SysTools.Configuration.Tests/TestDoubles/ConfigurationTestDoubles.cs`

**Checkpoint**: Modelos, resultados y fronteras de capa compilan sin acceso a archivos o criptografía desde Entities/Business.

---

## Phase 3: User Story 1 - Iniciar con una configuración local segura (Priority: P1) 🎯 MVP

**Goal**: Cargar una configuración existente o crear defaults persistibles cuando el archivo no existe, informando que faltan datos de conexión.

**Independent Test**: Con repositorio temporal vacío, LoadAsync crea y devuelve los once defaults, persiste un documento base y devuelve juntos los tres issues de conexión aún faltantes (host, ruta y password) sin intentar Firebird.

### Tests for User Story 1

- [X] T013 [P] [US1] Escribir primero pruebas fallidas de defaults, normalización nula y ausencia de representación textual sensible en `tests/SysTools.Configuration.Tests/Entities/AppConfigurationTests.cs`
- [X] T014 [P] [US1] Escribir primero pruebas fallidas de validación completa/orden estable en `tests/SysTools.Configuration.Tests/Business/ConfigurationValidatorTests.cs`: catálogos solo `a|b`, `CO|MX`, columnas `1|2|3`, y readiness exige ipEmpresa/rutaEmpresa/usuario/password no whitespace
- [X] T015 [P] [US1] Escribir primero pruebas fallidas de LoadAsync para archivo ausente, defaults creados y fallo al persistir defaults en `tests/SysTools.Configuration.Tests/Business/ConfigurationServiceLoadTests.cs`
- [X] T016 [P] [US1] Escribir primero pruebas fallidas de ruta `%APPDATA%\SysUtilerias\configuracion.json` y AppData vacío en `tests/SysTools.Configuration.Tests/Data/AppDataConfigurationPathProviderTests.cs`
- [X] T017 [US1] Ejecutar T013–T016 y confirmar fallos por tipos/comportamiento ausentes antes de implementar

### Implementation for User Story 1

- [X] T018 [P] [US1] Implementar validación pura que recopile todos los issues y separe IsPersistable/IsConnectionReady en `src/Business/Configuration/ConfigurationValidator.cs`
- [X] T019 [P] [US1] Implementar resolución de ApplicationData sin fallback al ejecutable en `src/Data/Configuration/AppDataConfigurationPathProvider.cs`
- [X] T020 [P] [US1] Crear `StoredConfigurationDocument` v1 con nombres JSON explícitos, `passwordProtegido`, nulls normalizados y extension data en `src/Data/Configuration/StoredConfigurationDocument.cs`
- [X] T021 [US1] Implementar escritura temporal UTF-8, flush, Move inicial, Replace de actualización y limpieza en `src/Data/Configuration/AtomicFileWriter.cs`
- [X] T022 [US1] Implementar lectura `NotFound` y round-trip base mediante seams, límite de 1 MB y serialización por instancia en `src/Data/Configuration/JsonConfigurationRepository.cs`
- [X] T023 [US1] Implementar LoadAsync y creación de defaults solo para `NotFound`, sin sobrescribir InvalidContent en `src/Business/Configuration/ConfigurationService.cs`
- [X] T024 [US1] Ejecutar las pruebas US1 de `tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj` y confirmar defaults, issues de readiness y creación controlada sin dependencias externas

**Checkpoint**: US1 funciona con seams controlados y entrega un MVP interno, aunque el protector DPAPI productivo se completa en US2.

---

## Phase 4: User Story 2 - Guardar y recuperar preferencias completas (Priority: P2)

**Goal**: Guardar y recargar los once campos, protegiendo el password para el usuario actual y conservando propiedades futuras.

**Independent Test**: Una configuración válida con Unicode se guarda, se recarga en otra instancia y conserva todos los valores; el password centinela no aparece en JSON ni logs y una propiedad desconocida sobrevive.

### Tests for User Story 2

- [X] T025 [P] [US2] Escribir primero pruebas fallidas de SaveAsync, rechazo sin I/O cuando IsPersistable=false y permiso de guardar cuando solo IsConnectionReady=false en `tests/SysTools.Configuration.Tests/Business/ConfigurationServiceSaveTests.cs`
- [X] T026 [P] [US2] Escribir primero 20 round-trips de los once campos, Unicode, espacios internos, nombres JSON v1 y ausencia del password claro en `tests/SysTools.Configuration.Tests/Data/JsonConfigurationRepositoryRoundTripTests.cs`
- [X] T027 [P] [US2] Escribir primero pruebas Windows de DPAPI CurrentUser: round-trip, ciphertext no determinista/no igual al claro y Base64 válido en `tests/SysTools.Configuration.Tests/Security/DpapiSecretProtectorTests.cs`
- [X] T028 [P] [US2] Escribir primero pruebas de propiedades desconocidas preservadas y versión mayor compatible en `tests/SysTools.Configuration.Tests/Data/JsonForwardCompatibilityTests.cs`
- [X] T029 [P] [US2] Escribir primero pruebas de dos operaciones cercanas que terminan en JSON completo y sin temporales huérfanos en `tests/SysTools.Configuration.Tests/Data/ConfigurationConcurrencyTests.cs`
- [X] T030 [US2] Ejecutar T025–T029 y confirmar que fallan por guardado/protector/forward compatibility incompletos

### Implementation for User Story 2

- [X] T031 [P] [US2] Implementar `DpapiSecretProtector` con ProtectedData CurrentUser, UTF-8, Base64 y limpieza viable de buffers en `src/Data/Configuration/DpapiSecretProtector.cs`
- [X] T032 [US2] Completar mapeo bidireccional de los once campos, protección obligatoria del password no vacío, extension data merge y operaciones concurrentes en `src/Data/Configuration/JsonConfigurationRepository.cs`
- [X] T033 [US2] Implementar SaveAsync con validación previa, cero I/O en ValidationFailed y traducción del resultado de repositorio en `src/Business/Configuration/ConfigurationService.cs`
- [X] T034 [US2] Registrar ConfigurationValidator, ConfigurationService, JsonConfigurationRepository, AppDataConfigurationPathProvider, DpapiSecretProtector y AtomicFileWriter en `src/Presentation/App.xaml.cs`
- [X] T035 [US2] Agregar prueba de composición que resuelva IConfigurationService con implementaciones productivas sin leer AppData en `tests/SysTools.Configuration.Tests/Composition/ConfigurationDependencyInjectionTests.cs`
- [X] T036 [US2] Ejecutar pruebas US1+US2 de `tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj` y confirmar 20 round-trips, DPAPI real, forward compatibility, concurrencia y composición

**Checkpoint**: US1 y US2 son funcionales; ningún consumidor necesita conocer ruta, JSON o DPAPI.

---

## Phase 5: User Story 3 - Detectar errores sin perder una configuración válida (Priority: P3)

**Goal**: Traducir contenido, secreto y almacenamiento fallidos en resultados seguros, preservando destino y shell.

**Independent Test**: JSON malformado, secreto no recuperable y fallos inyectados de write/replace devuelven estados distintos, conservan los bytes del archivo previo y ningún log contiene los centinelas de password/licencia/ciphertext.

### Tests for User Story 3

- [X] T037 [P] [US3] Escribir primero pruebas de JSON vacío/truncado/tipos incompatibles/mayor de 1 MB preservado como InvalidContent en `tests/SysTools.Configuration.Tests/Data/ConfigurationMalformedContentTests.cs`
- [X] T038 [P] [US3] Escribir primero pruebas de Base64 inválido y Unprotect fallido como SecretUnavailable sin AppConfiguration parcial en `tests/SysTools.Configuration.Tests/Security/ConfigurationSecretFailureTests.cs`
- [X] T039 [P] [US3] Escribir primero pruebas de fallo antes de replace, acceso denegado y cancelación conservando byte por byte el destino en `tests/SysTools.Configuration.Tests/Data/AtomicFileWriterFailureTests.cs`
- [X] T040 [P] [US3] Escribir primero pruebas de eventos/códigos y auditoría de password, ciphertext, licencia y payload completo ausentes en `tests/SysTools.Configuration.Tests/Security/ConfigurationLoggingTests.cs`
- [X] T041 [P] [US3] Escribir primero prueba de que un resultado fallido de configuración no impide resolver ShellWindow/ShellViewModel en `tests/SysTools.Configuration.Tests/Composition/ConfigurationFailureIsolationTests.cs`
- [X] T042 [US3] Ejecutar T037–T041 en `tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj` y confirmar fallos antes de completar traducción, preservación y logging

### Implementation for User Story 3

- [X] T043 [US3] Completar traducción de JsonException, CryptographicException, UnauthorizedAccessException, IOException y cancelación sin payload en `src/Data/Configuration/JsonConfigurationRepository.cs`
- [X] T044 [US3] Completar limpieza de temporales y preservación del destino para todos los puntos de fallo/cancelación en `src/Data/Configuration/AtomicFileWriter.cs`
- [X] T045 [US3] Agregar logging estructurado con nombres de evento, Outcome, IssueCodes/IssueFields y prohibición de destructuring en `src/Business/Configuration/ConfigurationService.cs`
- [X] T046 [US3] Agregar logging técnico de archivo/protección sin datos del documento en `src/Data/Configuration/JsonConfigurationRepository.cs`
- [X] T047 [US3] Ejecutar pruebas US3 de `tests/SysTools.Configuration.Tests/SysTools.Configuration.Tests.csproj` y confirmar estados controlados, archivo previo intacto, shell resoluble y cero secretos en diagnósticos

**Checkpoint**: Las tres historias son funcionales y los fallos esperables no escapan como texto técnico hacia Presentation.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verificar arquitectura, seguridad, rendimiento, regresiones y alcance.

- [X] T048 [P] Ampliar `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs` para rechazar File/ProtectedData/JsonConfigurationRepository en Views, ViewModels y Commands, y dependencias externas en Entities
- [X] T049 [P] Agregar la medición de 100 ciclos y umbral 95/100 menor a 1 segundo en `tests/SysTools.Configuration.Tests/Performance/ConfigurationPerformanceTests.cs`
- [X] T050 Ejecutar `dotnet restore`, build Debug/Release y todas las pruebas de `SysTools.sln`; registrar duración, warnings y conteos en `specs/002-local-configuration-secrets/validation/build-and-tests.md`
- [X] T051 Ejecutar los escenarios completos de `specs/002-local-configuration-secrets/quickstart.md` y registrar resultados en `specs/002-local-configuration-secrets/validation/quickstart-results.md`
- [X] T052 Auditar archivo, temporales y logs con password/licencia centinela y documentar cero filtraciones en `specs/002-local-configuration-secrets/validation/security-audit.md`
- [X] T053 Auditar ausencia de Firebird, UI de configuración, licencia funcional, impresoras, reportes e importación Java, y registrar el resultado en `specs/002-local-configuration-secrets/validation/scope-audit.md`
- [X] T054 Revisar cumplimiento de la constitución y contratos, resolver cualquier desviación y marcar todas las tareas completadas en `specs/002-local-configuration-secrets/tasks.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 — Setup**: Sin dependencias; T001 bloquea pruebas y T002/T003 pueden avanzar en paralelo.
- **Phase 2 — Foundational**: Depende de Setup y bloquea todas las historias.
- **US1 (Phase 3)**: Depende de Foundational y entrega defaults/lectura MVP.
- **US2 (Phase 4)**: Depende de US1 porque completa el mismo repositorio con secretos y update.
- **US3 (Phase 5)**: Depende de US1+US2 para provocar fallos sobre el flujo completo.
- **Polish (Phase 6)**: Depende de las tres historias.

### User Story Dependency Graph

```text
Setup → Foundational → US1 (MVP) → US2 → US3 → Polish
```

### Within Each User Story

- Escribir y ejecutar primero las pruebas enumeradas para confirmar rojo.
- Crear modelos/contratos antes de servicios consumidores.
- Completar Data antes de cerrar la orquestación Business que depende de ella.
- Ejecutar pruebas de la historia y regresiones anteriores en cada checkpoint.
- No habilitar UI ni servicios excluidos para demostrar la historia.

### Parallel Opportunities

- T002 y T003 pueden ejecutarse en paralelo después de T001.
- T005–T007, T009–T011 pueden ejecutarse en paralelo; T008 consolida resultados y T012 usa los contratos.
- T013–T016 son pruebas independientes; T018–T020 tocan capas/archivos distintos.
- T025–T029 son pruebas independientes; T031 puede avanzar antes de T032/T033.
- T037–T041 son pruebas independientes.
- T048 y T049 pueden avanzar en paralelo al comenzar Polish.

## Parallel Example: User Story 1

```text
Task T013: AppConfigurationTests.cs
Task T014: ConfigurationValidatorTests.cs
Task T015: ConfigurationServiceLoadTests.cs
Task T016: AppDataConfigurationPathProviderTests.cs
```

## Parallel Example: User Story 2

```text
Task T025: ConfigurationServiceSaveTests.cs
Task T026: JsonConfigurationRepositoryRoundTripTests.cs
Task T027: DpapiSecretProtectorTests.cs
Task T028: JsonForwardCompatibilityTests.cs
Task T029: ConfigurationConcurrencyTests.cs
```

## Parallel Example: User Story 3

```text
Task T037: ConfigurationMalformedContentTests.cs
Task T038: ConfigurationSecretFailureTests.cs
Task T039: AtomicFileWriterFailureTests.cs
Task T040: ConfigurationLoggingTests.cs
Task T041: ConfigurationFailureIsolationTests.cs
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Setup y Foundational.
2. Escribir las pruebas US1 y confirmar rojo.
3. Implementar defaults, path, documento, writer, repositorio y LoadAsync.
4. Validar creación base y tres issues de conexión, considerando que `SYSDBA` ya es el usuario default.
5. Detenerse si se necesita demostrar la base antes de introducir DPAPI real.

### Incremental Delivery

1. Setup + Foundational → contratos compilables.
2. US1 → primer uso y defaults seguros.
3. US2 → round-trip completo con DPAPI y DI.
4. US3 → recuperación, diagnóstico y preservación.
5. Polish → evidencia integral y auditorías.

### Parallel Team Strategy

1. Completar Setup/Foundation de forma conjunta.
2. Dentro de cada historia, repartir primero archivos de prueba marcados [P].
3. Un responsable integra Data y otro Business cuando los contratos ya compilan.
4. Consolidar composición, arquitectura y evidencias al final.

## Notes

- `[P]` solo aparece cuando no existe dependencia inmediata ni solapamiento de archivo.
- Los tests de DPAPI requieren Windows y nunca usan credenciales reales.
- Base64 solo transporta ciphertext DPAPI; ninguna tarea codifica el password en claro.
- No serializar ni destructurar `AppConfiguration` en logging.
- No leer `configuracion.properties` ni introducir Firebird en esta feature.
