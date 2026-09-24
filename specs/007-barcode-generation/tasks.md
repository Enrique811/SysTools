# Tasks: Generacion de codigos de barras

**Input**: Design documents from `/specs/007-barcode-generation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: La especificacion exige una matriz automatizada, round-trip decodificable, fallos inyectados, cancelacion, auditoria de seguridad y 1,000 generaciones. Cada grupo de pruebas se escribe y confirma rojo antes de su implementacion.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo porque toca archivos distintos y no depende de trabajo incompleto.
- **[Story]**: Historia funcional cubierta por la tarea.
- Cada tarea incluye rutas exactas.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar dependencias y el proyecto de pruebas dedicado.

- [X] T001 Crear `tests/SysTools.Barcodes.Tests/SysTools.Barcodes.Tests.csproj` para `net10.0-windows` con xUnit/Test SDK, `ZXing.Net.Bindings.Windows.Compatibility` 0.16.16, referencias a Entities/Business/Data y agregarlo a `SysTools.sln`
- [X] T002 [P] Agregar `ZXing.Net.Bindings.Windows.Compatibility` 0.16.16 a `src/Data/SysTools.Data.csproj` sin agregar ZXing ni System.Drawing a `src/Business/SysTools.Business.csproj` o `src/Entities/SysTools.Entities.csproj`
- [X] T003 [P] Preparar las carpetas `Compatibility`, `Business`, `Data`, `Entities`, `Performance`, `Security` y `TestDoubles` bajo `tests/SysTools.Barcodes.Tests/` mediante los archivos concretos de las tareas siguientes

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Crear tipos, resultado y contratos compartidos que bloquean las tres historias.

**CRITICAL**: No iniciar historias hasta completar esta fase.

- [X] T004 [P] Escribir primero pruebas fallidas para estados excluyentes, fabricas de exito/fallo, mensajes seguros, copia defensiva y bytes vacios en `tests/SysTools.Barcodes.Tests/Entities/BarcodeGenerationResultTests.cs`
- [X] T005 [P] Crear `BarcodeType` con `Ean8`, `Ean13`, `UpcA`, `Code128` y `BarcodeGenerationStatus` con `Success`, `MissingInput`, `UnsupportedValue`, `InputTooLong`, `GenerationFailed` en `src/Entities/Barcodes/BarcodeType.cs` y `src/Entities/Barcodes/BarcodeGenerationStatus.cs`
- [X] T006 Implementar `BarcodeGenerationResult` sellado e inmutable: mensaje seguro por estado; `NormalizedValue` y `Type` solo en `Success`; `Width`/`Height` positivos y `ImageLength` entre `1..1,048,576` solo en `Success`; valores nulos/cero y copia vacia en fallos; copia defensiva al construir y en cada `GetImageBytes()` en `src/Entities/Barcodes/BarcodeGenerationResult.cs`
- [X] T007 [P] Definir `IBarcodeImageEncoder.EncodePng(...)` y `BarcodeEncodingException` sin exponer entrada ni bytes en `src/Business/Barcodes/IBarcodeImageEncoder.cs` y `src/Business/Barcodes/BarcodeEncodingException.cs`
- [X] T008 Definir `IBarcodeService.DetectType(string?)` y `Generate(string?, CancellationToken)` segun `contracts/barcode-contract.md` en `src/Business/Barcodes/IBarcodeService.cs`
- [X] T009 [P] Crear encoder y logger configurables para exito, fallo y cancelacion en `tests/SysTools.Barcodes.Tests/TestDoubles/BarcodeTestDoubles.cs`
- [X] T010 Ejecutar pruebas Entities/compilacion, confirmar la transicion rojo-verde y registrar invariantes y dependencias en `specs/007-barcode-generation/validation/foundation.md`

**Checkpoint**: Resultado, enums y puertos son estables, inmutables y libres de ZXing.

---

## Phase 3: User Story 1 - Elegir automaticamente el tipo compatible (Priority: P1)

**Goal**: Normalizar texto y detectar EAN-8, EAN-13, UPC-A o fallback CODE128 con las reglas Java.

**Independent Test**: Una tabla de al menos 100 codigos obtiene el tipo esperado, incluidos al menos 20 casos por formato numerico y 20 CODE128, sin UI ni generacion de imagen.

### Tests for User Story 1

- [X] T011 [P] [US1] Escribir primero una matriz fallida de al menos 100 casos con 20 EAN-8, 20 EAN-13, 20 UPC-A y 20 CODE128, incluyendo checksums validos/invalidos y ceros iniciales, en `tests/SysTools.Barcodes.Tests/Compatibility/BarcodeClassificationMatrixTests.cs`
- [X] T012 [P] [US1] Escribir primero pruebas fallidas de nulo, vacio, recorte Java exacto de `U+0000..U+0020` en extremos, espacios interiores, solo digitos ASCII, otras longitudes, digitos Unicode y preservacion textual en `tests/SysTools.Barcodes.Tests/Business/BarcodeClassifierTests.cs`

### Implementation for User Story 1

- [X] T013 [US1] Implementar normalizacion, deteccion ASCII y checksums con pesos EAN-8 `3,1,3,1,3,1,3`, EAN-13 `1,3,1,3,1,3,1,3,1,3,1,3`, UPC-A `3,1,3,1,3,1,3,1,3,1,3` y verificador `(10 - suma % 10) % 10` en `src/Business/Barcodes/BarcodeClassifier.cs`
- [X] T014 [US1] Ejecutar pruebas Compatibility/Classifier, confirmar 100% de la matriz y documentar conteos, fallback y ceros iniciales en `specs/007-barcode-generation/validation/us1-classification.md`

**Checkpoint**: La clasificacion compatible queda lista sin usar ZXing, WPF o recursos externos; el MVP funcional se completa con US2 al producir la imagen solicitada.

---

## Phase 4: User Story 2 - Generar una imagen lista para etiquetas (Priority: P1)

**Goal**: Generar un PNG inmutable, dimensionado y decodificable con el tipo realmente usado.

**Independent Test**: Para cada simbologia, el resultado declara tipo/dimensiones correctos y un lector recupera exactamente el texto normalizado.

### Tests for User Story 2

- [X] T015 [P] [US2] Escribir primero pruebas fallidas del encoder real para firma PNG, margen cero, monocromo, EAN/UPC `340 x 56` y CODE128 `max(500, longitud * 18) x 60` en `tests/SysTools.Barcodes.Tests/Data/ZxingBarcodeImageEncoderTests.cs`
- [X] T016 [P] [US2] Escribir primero pruebas fallidas del servicio para recorte exterior unico, preservacion interior/ceros, tipo, dimensiones, una llamada al encoder y resultado inmutable en `tests/SysTools.Barcodes.Tests/Business/BarcodeServiceGenerationTests.cs`
- [X] T017 [P] [US2] Escribir primero pruebas fallidas end-to-end que decodifiquen EAN-8, EAN-13, UPC-A y CODE128 y recuperen exactamente tipo/texto en `tests/SysTools.Barcodes.Tests/Compatibility/BarcodeRoundTripTests.cs`
- [X] T018 [P] [US2] Agregar primero pruebas fallidas de registro y resolucion de `IBarcodeImageEncoder` e `IBarcodeService` en `tests/SysTools.Presentation.Tests/Composition/BarcodeDependencyInjectionTests.cs`
- [X] T019 [P] [US2] Extender primero los guards para prohibir ZXing/System.Drawing en Entities/Business y generacion directa en Views/ViewModels en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`

### Implementation for User Story 2

- [X] T020 [P] [US2] Implementar validacion BCL completa de firma PNG, secuencia de chunks, CRC, `IHDR` unico y primero, dimensiones solicitadas, presencia de `IDAT`/`IEND`, final exacto y longitud `1..1,048,576` en `src/Business/Barcodes/PngOutputValidator.cs`
- [X] T021 [P] [US2] Implementar traduccion `BarcodeType` -> `BarcodeFormat`, margen cero, fondo blanco, barras negras, dimensiones exactas y serializacion PNG en memoria en `src/Data/Barcodes/ZxingBarcodeImageEncoder.cs`
- [X] T022 [US2] Implementar `BarcodeService` para delegar `DetectType`, calcular EAN/UPC `340 x 56`, CODE128 `max(500, longitud * 18) x 60`, llamar una vez al encoder y construir exito solo tras validar PNG en `src/Business/Barcodes/BarcodeService.cs`
- [X] T023 [US2] Registrar `IBarcodeImageEncoder -> ZxingBarcodeImageEncoder` e `IBarcodeService -> BarcodeService` sin agregar logica de generacion a Presentation en `src/Presentation/App.xaml.cs`
- [X] T024 [US2] Ajustar un lector independiente para decodificar el artefacto con margen cero sobre un lienzo blanco externo, verificando formato, texto y dimensiones sin compartir la implementacion productiva en `tests/SysTools.Barcodes.Tests/Compatibility/BarcodeRoundTripTests.cs`
- [X] T025 [US2] Ejecutar pruebas Generation/Data/RoundTrip/Composition/Architecture y documentar los cuatro formatos y sus dimensiones en `specs/007-barcode-generation/validation/us2-generation.md`

**Checkpoint**: Los cuatro formatos generan PNG completos, inmutables y decodificables sin UI.

---

## Phase 5: User Story 3 - Manejar entradas y fallos sin interrumpir la operacion (Priority: P2)

**Goal**: Convertir entradas ausentes/no soportadas/excesivas y fallos recuperables en estados seguros, preservando cancelacion.

**Independent Test**: Entradas limite, encoder fallido, PNG invalido y cancelacion producen el estado o excepcion esperado, cero bytes y logs sin codigo.

### Tests for User Story 3

- [X] T026 [P] [US3] Escribir primero pruebas fallidas para `MissingInput`, `InputTooLong` a 51 caracteres, `UnsupportedValue` fuera de `U+0000..U+007F`, limite aceptado de 50 y encoder no invocado en rechazos en `tests/SysTools.Barcodes.Tests/Business/BarcodeServiceInputFailureTests.cs`
- [X] T027 [P] [US3] Escribir primero pruebas fallidas para `BarcodeEncodingException`, excepcion recuperable inesperada, PNG vacio, firma incorrecta, dimensiones incorrectas, truncamiento, CRC incorrecto, ausencia de `IEND` y mas de 1 MiB sin bytes parciales en `tests/SysTools.Barcodes.Tests/Business/BarcodeServiceGenerationFailureTests.cs`
- [X] T028 [P] [US3] Escribir primero pruebas fallidas de cancelacion antes, durante y despues del encoder, propagacion de `OperationCanceledException` y cero log de error en `tests/SysTools.Barcodes.Tests/Business/BarcodeCancellationTests.cs`
- [X] T029 [P] [US3] Escribir primero una auditoria con centinelas que permita solo etapa, tipo, categoria y longitud y prohiba codigo completo, fragmentos y PNG/base64 en `tests/SysTools.Barcodes.Tests/Security/BarcodeLoggingSecurityTests.cs`

### Implementation for User Story 3

- [X] T030 [P] [US3] Traducir fallos recuperables de ZXing/System.Drawing a `BarcodeEncodingException`, validar argumentos internos y observar cancelacion antes/despues de codificar y serializar en `src/Data/Barcodes/ZxingBarcodeImageEncoder.cs`
- [X] T031 [US3] Implementar limite de 50, representabilidad ASCII, estados y mensajes seguros, validacion de salida, propagacion de cancelacion y logging estructurado sin contenido en `src/Business/Barcodes/BarcodeService.cs`
- [X] T032 [US3] Ejecutar pruebas InputFailure/GenerationFailure/Cancellation/Security y documentar estados, limites y cero filtraciones en `specs/007-barcode-generation/validation/us3-failures-security.md`

**Checkpoint**: Ningun fallo recuperable cierra la aplicacion, filtra el codigo o entrega una imagen parcial.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Rendimiento, estabilidad, regresion, trazabilidad y cierre de alcance.

- [X] T033 [P] Agregar prueba de 1,000 generaciones entre los cuatro tipos, cero corrupcion/referencias compartidas/dimensiones incorrectas y p95 menor de 100 ms en `tests/SysTools.Barcodes.Tests/Performance/BarcodeReliabilityPerformanceTests.cs`
- [X] T034 [P] Auditar dependencias restauradas, licencia, vulnerabilidades conocidas y confinamiento de ZXing/System.Drawing a Data/pruebas y registrar resultado en `specs/007-barcode-generation/validation/dependency-architecture-audit.md`
- [X] T035 Ejecutar `dotnet build SysTools.sln --no-restore` y `dotnet test SysTools.sln --no-build`, corregir regresiones y registrar conteos en `specs/007-barcode-generation/validation/full-regression.md`
- [X] T036 Ejecutar todos los escenarios de `specs/007-barcode-generation/quickstart.md` y registrar comandos/resultados en `specs/007-barcode-generation/validation/quickstart-results.md`
- [X] T037 [P] Auditar que no se agregaron captura visual, busqueda, vista previa, plantillas, reportes, impresoras, impresion, archivos ni acceso Firebird y documentar en `specs/007-barcode-generation/validation/scope-security-audit.md`
- [X] T038 Revisar trazabilidad FR-001..FR-024 y SC-001..SC-008 contra pruebas/artefactos y registrar cumplimiento constitucional en `specs/007-barcode-generation/validation/constitution-traceability.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup**: inicia de inmediato.
- **Foundational**: depende de Setup y bloquea las tres historias.
- **US1**: depende de Foundational; T011-T012 deben estar rojas antes de T013.
- **US2**: depende de Foundational y del clasificador verde de US1; T015-T019 deben estar rojas antes de T020-T023.
- **US3**: depende del pipeline exitoso de US2; T026-T029 deben estar rojas antes de T030-T031.
- **Polish**: depende de las tres historias completas.

### User Story Dependencies

- **US1 (P1)**: entrega clasificacion independiente despues de Foundational.
- **US2 (P1)**: reutiliza US1 para seleccionar formato y agrega encoder, resultado, round-trip y composicion.
- **US3 (P2)**: endurece el pipeline de US2 con limites, fallos, cancelacion y diagnostico seguro.

### Within Each User Story

- Escribir y ejecutar las pruebas rojas antes del codigo correspondiente.
- En US1, checksums y normalizacion preceden cualquier integracion.
- En US2, validador PNG y encoder pueden avanzar en paralelo; ambos preceden al servicio y la DI.
- En US3, traduccion Data y manejo Business se integran despues de sus pruebas de fallo/cancelacion.
- Cada checkpoint debe quedar verde antes de avanzar.

### Parallel Opportunities

- T002-T003, T004-T005/T007/T009, T011-T012, T015-T019, T020-T021, T026-T029 y T033-T034/T037 pueden ejecutarse en paralelo cuando sus prerrequisitos esten completos.
- T022, T023, T024, T030 y T031 son secuenciales respecto a las pruebas o componentes que integran.

---

## Parallel Example: User Story 1

```text
Task T011: matriz de compatibilidad con al menos 100 codigos
Task T012: normalizacion, ASCII y fallback
```

## Parallel Example: User Story 2

```text
Task T015: contrato del encoder PNG real
Task T016: contrato del servicio con doble
Task T017: round-trip de los cuatro formatos
Task T018: composicion DI
Task T019: guards arquitectonicos
```

## Parallel Example: User Story 3

```text
Task T026: entradas ausentes, excesivas y no representables
Task T027: fallos y PNG invalidos
Task T028: cancelacion
Task T029: seguridad de logging
```

## Implementation Strategy

### MVP First

1. Completar Setup y Foundational.
2. Completar US1 con normalizacion, checksums y matriz de compatibilidad.
3. Completar US2 con PNG dimensionado y decodificable; US1 + US2 constituyen el MVP funcional.

### Incremental Delivery

1. US1 entrega deteccion compatible.
2. US2 agrega PNG decodificable y composicion.
3. US3 agrega limites, cancelacion, fallos y diagnostico seguro.
4. Polish cierra rendimiento, regresion y trazabilidad.

## Notes

- Marcar `[X]` solo despues de ejecutar y verificar la tarea.
- No registrar el codigo, fragmentos, bytes, base64 ni excepciones serializadas.
- No agregar UI, Firebird, archivos, reportes o impresion en esta feature.
- No introducir ZXing/System.Drawing en Entities, Business, Views o ViewModels.
