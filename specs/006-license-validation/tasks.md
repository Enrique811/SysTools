# Tasks: Validacion de licencias heredadas

**Input**: Design documents from `/specs/006-license-validation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: La especificacion exige pruebas automatizadas, vectores de compatibilidad, auditoria de seguridad y TDD para reglas criticas. Cada grupo de pruebas se escribe y confirma rojo antes de su implementacion.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo porque toca archivos distintos y no depende de trabajo incompleto.
- **[Story]**: Historia funcional cubierta por la tarea.
- Cada tarea incluye rutas exactas.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar el proyecto de pruebas y las fronteras de la feature.

- [X] T001 Crear `tests/SysTools.Licensing.Tests/SysTools.Licensing.Tests.csproj` para `net10.0-windows` con xUnit/Test SDK, referencias a Entities/Business/Data y agregarlo a `SysTools.sln`
- [X] T002 [P] Crear la estructura `Compatibility`, `Business`, `Data`, `Entities`, `Security` y `TestDoubles` bajo `tests/SysTools.Licensing.Tests/` con archivos de prueba concretos en las tareas siguientes
- [X] T003 [P] Verificar que `.gitignore` conserva `bin/`, `obj/`, `.vs/`, `.idea/`, `.vscode/`, `*.user`, logs, `.env*` y material `private.key` sin eliminar patrones existentes en `.gitignore`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Tipos y contratos compartidos que bloquean todas las historias.

**CRITICAL**: No iniciar historias hasta completar esta fase.

- [X] T004 [P] Escribir pruebas fallidas de invariantes, mensajes controlados, `IsValid` y ausencia de UUID/firma/ruta/excepcion en `tests/SysTools.Licensing.Tests/Entities/LicenseValidationResultTests.cs`
- [X] T005 [P] Crear `LicenseIssuer` con `None`, `DistributorColombia`, `Developer` y `LicenseValidationStatus` con los once estados exactos del modelo en `src/Entities/Licensing/LicenseIssuer.cs` y `src/Entities/Licensing/LicenseValidationStatus.cs`
- [X] T006 Implementar `LicenseValidationResult` inmutable con `Status`, `Message`, `Issuer`, `ValidFrom`, `ValidUntil`, `ServerTime` e invariantes del `data-model.md` en `src/Entities/Licensing/LicenseValidationResult.cs`
- [X] T007 [P] Definir `ILicenseService.ValidateAsync(string?, AppConfiguration, CancellationToken)`, `IHardwareIdProvider` e `ILicenseSourceReader` con resultados de lectura tipados en `src/Business/Licensing/ILicenseService.cs`, `src/Business/Licensing/IHardwareIdProvider.cs` y `src/Business/Licensing/ILicenseSourceReader.cs`
- [X] T008 [P] Crear dobles configurables de fuente, UUID, firma, reloj y logger sin valores sensibles en `tests/SysTools.Licensing.Tests/TestDoubles/LicensingTestDoubles.cs`
- [X] T009 Ejecutar las pruebas de Entities/compilacion, confirmar la transicion rojo-verde y registrar resultado en `specs/006-license-validation/validation/foundation.md`

**Checkpoint**: Resultado y contratos son estables, provider-independent y seguros.

---

## Phase 3: User Story 1 - Validar una licencia Java existente (Priority: P1) MVP

**Goal**: Parsear/canonizar documentos y verificar exactamente las dos firmas heredadas.

**Independent Test**: Vectores firmados para ambos emisores se aceptan; alterar cualquier dato o firma se rechaza y el payload coincide byte por byte con Java.

### Tests for User Story 1

- [X] T010 [P] [US1] Escribir primero pruebas fallidas de objeto raiz, campos string requeridos, extras aceptados, duplicados/tipos/UUID/fechas/rango invalidos en `tests/SysTools.Licensing.Tests/Compatibility/LegacyLicenseParsingTests.cs`
- [X] T011 [P] [US1] Escribir primero pruebas fallidas del payload UTF-8 exacto, UUID normalizado, `LF`, sin salto final e inicio/fin canonicos en `tests/SysTools.Licensing.Tests/Compatibility/LegacyLicenseCanonicalizationTests.cs`
- [X] T012 [P] [US1] Crear vectores no secretos firmados para UUID ficticio y ambas llaves publicas, y pruebas rojas de emisor/alteracion/firma Base64 invalida en `tests/SysTools.Licensing.Tests/Compatibility/LegacyLicenseSignatureTests.cs`

### Implementation for User Story 1

- [X] T013 [US1] Implementar parsing estricto, normalizacion UUID, fechas invariantes y payload canonico en `src/Business/Licensing/LicenseDocumentParser.cs`
- [X] T014 [US1] Implementar las dos llaves publicas X.509 heredadas y verificacion RSA SHA-256 PKCS#1 v1.5 sin material privado en `src/Business/Licensing/LegacyLicenseSignatureVerifier.cs`
- [X] T015 [US1] Integrar parsing y firma con cortocircuito `InvalidJson`/`InvalidFields`/`InvalidSignature` y emisor estable en `src/Business/Licensing/LicenseService.cs`
- [X] T016 [US1] Ejecutar pruebas Compatibility, confirmar ambos emisores y documentar algoritmo, conteo y cero material privado en `specs/006-license-validation/validation/us1-compatibility.md`

**Checkpoint**: El MVP autentica documentos Java existentes sin UUID real ni Firebird.

---

## Phase 4: User Story 2 - Cargar una licencia de forma segura (Priority: P2)

**Goal**: Aceptar JSON directo o archivo UTF-8 con fallos controlados y limite de 64 KiB.

**Independent Test**: JSON, rutas relativas/absolutas y archivos validos se cargan una vez; ausentes, inaccesibles, UTF-8 invalido y grandes producen `SourceUnavailable`.

### Tests for User Story 2

- [X] T017 [P] [US2] Escribir pruebas fallidas de JSON directo que no invoca Data, recorte inicial y fuente llamada una vez en `tests/SysTools.Licensing.Tests/Business/LicenseSourceSelectionTests.cs`
- [X] T018 [P] [US2] Escribir pruebas fallidas de archivo UTF-8, ruta relativa/absoluta, ausente, directorio, acceso, bytes invalidos, cancelacion y limite 64 KiB en `tests/SysTools.Licensing.Tests/Data/FileLicenseSourceReaderTests.cs`

### Implementation for User Story 2

- [X] T019 [US2] Implementar lectura asincrona UTF-8 estricta, resolucion de ruta y limite 64 KiB con resultado seguro en `src/Data/Licensing/FileLicenseSourceReader.cs`
- [X] T020 [US2] Integrar seleccion JSON/ruta y traduccion de cualquier fallo de fuente a `SourceUnavailable` sin incluir ruta o excepcion en `src/Business/Licensing/LicenseService.cs`
- [X] T021 [US2] Ejecutar pruebas Source/Parsing y documentar conteo, una lectura maxima y cancelacion en `specs/006-license-validation/validation/us2-license-source.md`

**Checkpoint**: Ambas formas de entrada funcionan sin filtrar su contenido o ubicacion.

---

## Phase 5: User Story 3 - Comprobar equipo y vigencia (Priority: P2)

**Goal**: Validar UUID local y limites inclusivos con fecha exclusiva del servidor.

**Independent Test**: Dobles controlan UUID y servidor para coincidencia, mismatch, indisponibilidad, bordes, no iniciada, vencida, fallo y cancelacion sin reloj local.

### Tests for User Story 3

- [X] T022 [P] [US3] Escribir pruebas fallidas de parsing de salida PowerShell/WMIC, orden de comandos, timeout, fallback, UUID malformado y cancelacion en `tests/SysTools.Licensing.Tests/Data/WindowsHardwareIdProviderTests.cs`
- [X] T023 [P] [US3] Escribir pruebas fallidas de UUID no disponible/diferente, cortocircuito antes del reloj y normalizacion en `tests/SysTools.Licensing.Tests/Business/LicenseHardwareValidationTests.cs`
- [X] T024 [P] [US3] Escribir pruebas fallidas de limites inclusivos, no iniciada, vencida, `RepositoryAccessException`, una llamada, reloj local ausente y cancelacion en `tests/SysTools.Licensing.Tests/Business/LicenseValidityTests.cs`

### Implementation for User Story 3

- [X] T025 [US3] Implementar proveedor Windows con comandos fijos PowerShell/WMIC, rutas System32/Sysnative, timeout, limite de salida y UUID canonico en `src/Data/Licensing/WindowsHardwareIdProvider.cs`
- [X] T026 [US3] Integrar UUID despues de firma, reloj despues de coincidencia, bordes inclusivos y estados `HardwareIdUnavailable`, `HardwareMismatch`, `NotYetValid`, `Expired`, `ServerTimeUnavailable` en `src/Business/Licensing/LicenseService.cs`
- [X] T027 [US3] Asegurar propagacion de `OperationCanceledException` solicitada desde fuente, UUID y repositorio sin logging de error en `src/Business/Licensing/LicenseService.cs`, `src/Data/Licensing/FileLicenseSourceReader.cs` y `src/Data/Licensing/WindowsHardwareIdProvider.cs`
- [X] T028 [US3] Ejecutar pruebas Hardware/Validity/Cancellation y documentar conteo, bordes y ausencia de reloj local en `specs/006-license-validation/validation/us3-hardware-server-time.md`

**Checkpoint**: Solo una firma del equipo y vigente por reloj Firebird produce `Valid`.

---

## Phase 6: User Story 4 - Diagnosticar sin exponer datos sensibles (Priority: P3)

**Goal**: Resultados/logs seguros, DI correcta y fronteras arquitectonicas automatizadas.

**Independent Test**: Centinelas de licencia, firma, UUID, ruta, host, password y cadena no aparecen; DI resuelve el servicio; Views/ViewModels permanecen libres de I/O/crypto/Data.

### Tests for User Story 4

- [X] T029 [P] [US4] Escribir pruebas fallidas que auditen todos los estados y eventos con centinelas de licencia, firma, UUID, ruta, host, password y cadena en `tests/SysTools.Licensing.Tests/Security/LicenseSecurityLoggingTests.cs`
- [X] T030 [P] [US4] Agregar pruebas fallidas de registro/resolucion de `ILicenseService`, `ILicenseSourceReader` e `IHardwareIdProvider` en `tests/SysTools.Presentation.Tests/Composition/LicensingDependencyInjectionTests.cs`
- [X] T031 [P] [US4] Extender guards para prohibir archivos/procesos/crypto/repositorios en Views/ViewModels, Firebird/Data en Business y llaves privadas/reloj local en licenciamiento en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`

### Implementation for User Story 4

- [X] T032 [US4] Agregar logging estructurado por etapa/estado, sin valores dinamicos sensibles y sin log de cancelacion, en `src/Business/Licensing/LicenseService.cs` y `src/Data/Licensing/WindowsHardwareIdProvider.cs`
- [X] T033 [US4] Registrar `ILicenseSourceReader`, `IHardwareIdProvider` e `ILicenseService` con ciclos correctos en `src/Presentation/App.xaml.cs`
- [X] T034 [US4] Ejecutar pruebas Security/Architecture/Composition y documentar eventos permitidos y cero filtraciones en `specs/006-license-validation/validation/us4-security-architecture.md`

**Checkpoint**: Servicio resoluble y diagnosticable sin violar seguridad o capas.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Regresion, rendimiento, trazabilidad y cierre de alcance.

- [X] T035 [P] Agregar matriz de al menos 25 mutaciones de estructura, UUID, fechas, firma y canonicalizacion en `tests/SysTools.Licensing.Tests/Compatibility/LicenseMutationMatrixTests.cs`
- [X] T036 [P] Agregar prueba de rendimiento local para rechazos <50 ms p95 y validacion completa con dobles <250 ms p95 en `tests/SysTools.Licensing.Tests/Business/LicensePerformanceTests.cs`
- [X] T037 Ejecutar `dotnet build SysTools.sln --no-restore` y `dotnet test SysTools.sln --no-build`, corregir regresiones y registrar conteos en `specs/006-license-validation/validation/full-regression.md`
- [X] T038 Ejecutar los escenarios de `specs/006-license-validation/quickstart.md` y registrar resultados en `specs/006-license-validation/validation/quickstart-results.md`
- [X] T039 [P] Auditar que no existen llave privada, reloj local, UI nueva, generacion, barcode, reportes, impresion, renovacion o revocacion y documentar en `specs/006-license-validation/validation/scope-security-audit.md`
- [X] T040 Revisar trazabilidad FR-001..FR-019 y SC-001..SC-007 contra pruebas/artefactos, y registrar cumplimiento constitucional en `specs/006-license-validation/validation/constitution-traceability.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: inicia de inmediato.
- **Foundational**: depende de Setup y bloquea historias.
- **US1**: depende de Foundational; T010-T012 rojas antes de T013-T015.
- **US2**: depende de Foundational y se integra con el pipeline de US1; T017-T018 rojas antes de T019-T020.
- **US3**: depende del pipeline autenticado de US1; T022-T024 rojas antes de T025-T027.
- **US4**: depende del servicio completo de US1-US3; T029-T031 rojas antes de T032-T033.
- **Polish**: depende de las cuatro historias.

### User Story Dependencies

- **US1 (P1)**: MVP independiente despues de Foundation.
- **US2 (P2)**: agrega una fuente al parser/verificador de US1, pero sus fallos se prueban independientemente.
- **US3 (P2)**: completa la decision de validez sobre documentos autenticados de US1.
- **US4 (P3)**: observa y compone el servicio final sin cambiar su decision.

### Within Each User Story

- Las pruebas se escriben y ejecutan rojas antes del codigo correspondiente.
- Parser antes de verificador y verificador antes del orquestador.
- Fuente/UUID antes de integrarlos en el servicio.
- Seguridad/arquitectura antes de DI final.
- Cada checkpoint debe quedar verde antes de avanzar.

### Parallel Opportunities

- T002/T003; T004/T005/T007/T008; T010-T012; T017/T018; T022-T024; T029-T031; T035/T036/T039 pueden ejecutarse en paralelo por archivo cuando sus prerrequisitos estan completos.
- La implementacion del mismo `LicenseService.cs` es secuencial para evitar conflictos y conservar cortocircuitos.

---

## Parallel Example: User Story 1

```text
Task T010: parsing y estructura JSON
Task T011: canonicalizacion byte por byte
Task T012: vectores RSA para ambos emisores
```

## Implementation Strategy

### MVP First

1. Completar Setup y Foundational.
2. Completar US1 con parser, payload y firmas heredadas.
3. Validar ambos emisores sin equipo o Firebird reales.

### Incremental Delivery

1. US1 autentica licencias.
2. US2 agrega archivos seguros.
3. US3 agrega equipo y vigencia para decision final.
4. US4 endurece diagnostico y composicion.
5. Polish cierra matrices, regresion y trazabilidad.

## Notes

- No copiar ni leer a artefactos ninguna llave privada; solo se permiten llaves publicas y firmas no secretas.
- No incluir licencias reales, UUID de equipos, rutas o credenciales en el repositorio.
- Marcar `[X]` cada tarea solo despues de verificar su resultado.
