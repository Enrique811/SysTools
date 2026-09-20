---

description: "Tareas de implementación para la base de shell modular de SysTools"
---

# Tasks: Base de shell modular de SysTools

**Input**: Design documents from `/specs/001-systools-shell-base/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contract.md, quickstart.md

**Tests**: El plan exige pruebas xUnit para estado, comandos, ViewModels y composición, además de validación manual WPF mediante quickstart.

**Organization**: Las tareas se agrupan por historia de usuario para conservar trazabilidad, incrementos demostrables y validación independiente.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo porque modifica archivos distintos y no depende de otra tarea incompleta de la misma fase.
- **[Story]**: Historia de usuario atendida (`US1`, `US2`, `US3`).
- Cada tarea incluye una ruta exacta de archivo o directorio.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Crear una solución reproducible con .NET 10 y las cuatro fronteras arquitectónicas requeridas.

- [ ] T001 Crear `global.json` en la raíz con SDK mínimo `10.0.100`, `rollForward` `latestFeature` y `allowPrerelease` `false`, y verificar que `dotnet --version` selecciona .NET 10 antes de continuar
- [ ] T002 Crear `SysTools.sln` y los proyectos `src/Presentation/SysTools.Presentation.csproj`, `src/Business/SysTools.Business.csproj`, `src/Data/SysTools.Data.csproj` y `src/Entities/SysTools.Entities.csproj` con `net10.0-windows` para WPF y `net10.0` para bibliotecas
- [ ] T003 Configurar las referencias de proyecto en los cuatro `.csproj`: Presentation → Business/Entities/Data, Business → Entities, Data → Business/Entities y Entities sin referencias a capas
- [ ] T004 Agregar Microsoft.Extensions.Hosting, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Serilog, Serilog.Extensions.Hosting y Serilog.Sinks.File a `src/Presentation/SysTools.Presentation.csproj` sin agregar Firebird, EF Core, ZXing o FastReport
- [ ] T005 [P] Crear las áreas vacías requeridas con archivos `README.md` de alcance en `src/Business/Services/`, `src/Data/Connection/`, `src/Data/Repositories/`, `src/Presentation/Views/` y `src/Reports/Templates/`
- [ ] T006 Crear `tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj` con `TargetFramework` `net10.0-windows`, `UseWPF` true, xUnit/test SDK, referencia a Presentation e inclusión en `SysTools.sln`; no crear un proyecto Business.Tests mientras Business no tenga comportamiento

**Checkpoint**: La solución restaura con .NET 10, contiene cuatro proyectos de producción y dos de prueba, y sus referencias respetan la arquitectura.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Proveer MVVM, estilos y recursos compartidos que bloquean todas las historias.

**⚠️ CRITICAL**: Ninguna historia puede comenzar hasta completar esta fase.

- [ ] T007 Implementar notificación de propiedades reutilizable en `src/Presentation/ViewModels/ViewModelBase.cs` sin referencias a Business, Data o APIs WPF de vista
- [ ] T008 [P] Implementar `ICommand`, `CanExecute` y notificación de disponibilidad en `src/Presentation/Commands/RelayCommand.cs`, sin lógica de negocio ni service locator
- [ ] T009 [P] Definir recursos semánticos para sidebar `#111827`, sidebar secundario `#1F2937`, fondo principal `#F3F4F6`, tarjeta `#FFFFFF`, borde `#D7DDE5`, acento `#2563EB`, texto primario `#111827` y secundario `#6B7280` en `src/Presentation/Styles/Colors.xaml`
- [ ] T010 [P] Definir Segoe UI, jerarquía tipográfica y tamaños reutilizables en `src/Presentation/Styles/Typography.xaml`
- [ ] T011 Crear estilos reutilizables de tarjetas, navegación, botones, campos deshabilitados y foco visible en `src/Presentation/Styles/Controls.xaml`, consumiendo únicamente claves semánticas de `Colors.xaml` y `Typography.xaml`
- [ ] T012 Fusionar `Colors.xaml`, `Typography.xaml` y `Controls.xaml` desde `src/Presentation/App.xaml` sin declarar `StartupUri`, para que la ventana sea resuelta por DI

**Checkpoint**: Foundation ready; MVVM y el sistema visual pueden consumirse sin duplicar valores ni crear lógica en Views.

---

## Phase 3: User Story 1 - Abrir la suite en el verificador (Priority: P1) 🎯 MVP

**Goal**: Abrir directamente la shell de SysTools con Verificador de precios seleccionado, contenido central cargado y seis opciones futuras visibles/deshabilitadas.

**Independent Test**: Iniciar la aplicación diez veces y comprobar que la única primera ventana es la shell, Verificador está seleccionado/cargado y las otras seis entradas no admiten selección.

### Tests for User Story 1

- [ ] T013 [P] [US1] Escribir primero pruebas fallidas del catálogo inicial en `tests/SysTools.Presentation.Tests/Shell/UtilityModuleItemTests.cs`: Id único/no vacío/kebab-case, DisplayName no vacío, Section `Utilities`/`Administration`, solo `price-verifier` habilitado y exactamente un elemento seleccionado
- [ ] T014 [P] [US1] Escribir primero pruebas fallidas del estado inicial/fallback en `tests/SysTools.Presentation.Tests/Shell/ShellViewModelTests.cs`: siete entradas en orden del mockup, Verificador activo con contenido mínimo de tipo PriceVerifierViewModel, contenido no nulo y recuperación de Verificador si no existe selección válida

### Implementation for User Story 1

- [ ] T015 [P] [US1] Implementar `ModuleSection` y `UtilityModuleItem` en `src/Presentation/Shell/Models/UtilityModuleItem.cs` con las restricciones por elemento: `Id` no vacío y kebab-case; `DisplayName` no vacío; `Section` solo Utilities/Administration; `Content` obligatorio para el habilitado y null para placeholders
- [ ] T016 [US1] Crear el placeholder mínimo `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs` y consumirlo desde `src/Presentation/Shell/ViewModels/ShellViewModel.cs` con siete entradas en orden, validando en el catálogo que todos los Id sean únicos: price-verifier habilitado/seleccionado con contenido no nulo y label-printing, quick-inventory, new-utility, settings, licensing y system-logs visibles/deshabilitados; impedir que una entrada deshabilitada cambie `ActiveModule`
- [ ] T017 [P] [US1] Crear `src/Presentation/Shell/Views/ShellWindow.xaml` y su code-behind mínimo con título SysTools, `MinWidth="1280"`, `MinHeight="720"`, sidebar oscuro, secciones Utilerías/Administración, selección azul, `ContentControl` enlazado a `ActiveModuleContent` y una única superficie inferior reservada para estado/mensajes debajo del módulo activo
- [ ] T018 [US1] Configurar host, DI y lifecycle en `src/Presentation/App.xaml.cs`: registrar ShellWindow/ShellViewModel/ViewModels como singletons, iniciar antes de mostrar una única ventana, resolver ShellWindow sin landing y detener/disponer el host al salir
- [ ] T019 [US1] Crear la vista mínima `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml`, agregar su DataTemplate ViewModel→View en `src/Presentation/App.xaml` y completar bindings de navegación en `src/Presentation/Shell/Views/ShellWindow.xaml`, asegurando contenido visible desde US1 y que incorporar módulos no requiera modificar la vista del Verificador
- [ ] T020 [US1] Ejecutar las pruebas US1 y documentar el resultado de 10 arranques consecutivos sin pantalla intermedia en `specs/001-systools-shell-base/validation/us1-shell-startup.md`

**Checkpoint**: MVP demostrable: la aplicación abre en la shell modular y solo Verificador de precios es seleccionable.

---

## Phase 4: User Story 2 - Reconocer la estructura del verificador (Priority: P2)

**Goal**: Mostrar las seis zonas visuales del Verificador con valores neutrales y toda captura/acción futura visible pero deshabilitada.

**Independent Test**: Abrir el módulo y localizar en menos de 30 segundos encabezado, código, información adicional, detalles, precio y estado; comprobar que campos/botones no reciben foco ni producen efectos.

### Tests for User Story 2

- [ ] T021 [P] [US2] Escribir primero pruebas fallidas en `tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierViewModelTests.cs` para Barcode/AdditionalInformation vacíos e inmutables desde UI, ProductDescription/ProductPresentation/StockDisplay/FinalPriceDisplay iguales a `—`, e IsBarcodeAvailable/IsAdditionalInformationAvailable/IsSearchAvailable/IsPrintAvailable/IsSettingsAvailable iguales a false

### Implementation for User Story 2

- [ ] T022 [US2] Extender `src/Presentation/Modules/PriceVerifier/ViewModels/PriceVerifierViewModel.cs` después de confirmar T021 fallida, agregando `ModuleTitle` no vacío, descripción sin prometer funcionalidad, Barcode/AdditionalInformation vacíos, cuatro valores `—` y cinco banderas de disponibilidad false
- [ ] T023 [US2] Extender `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml` con encabezado, código de barras, información adicional, detalles de producto, precio final y accesos rápidos usando Grid/tamaños relativos, tarjetas compartidas y sin Canvas o coordenadas absolutas; conservar como única zona inferior la superficie de ShellWindow creada en T017
- [ ] T024 [US2] Enlazar en `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml` Código de barras e Información adicional a disponibilidad false para que sean visibles, deshabilitados, no enfocables y no editables; enlazar Buscar/Imprimir/Configuración como visibles/deshabilitados y dejar Enter/Ctrl+F7..F10 sin comandos
- [ ] T025 [US2] Verificar y actualizar los bindings existentes de `price-verifier` en `src/Presentation/Shell/ViewModels/ShellViewModel.cs` y su DataTemplate en `src/Presentation/App.xaml` para exponer las propiedades agregadas en T022, sin volver a registrar el módulo ni instanciar la View desde el ViewModel
- [ ] T026 [US2] Ejecutar pruebas US2 y registrar comparación visual a 1280 × 720, pantalla completa y escalado 125% contra `docs/mockup-shell-utilerias.svg` en `specs/001-systools-shell-base/validation/us2-price-verifier-layout.md`

**Checkpoint**: El placeholder completo es reconocible, adaptable y no aparenta captura o consulta funcional.

---

## Phase 5: User Story 3 - Identificar estados operativos sin funciones simuladas (Priority: P3)

**Goal**: Mostrar conexión, licencia y mensajes neutrales, y mantener la shell ante errores recuperables con diagnóstico estructurado.

**Independent Test**: Iniciar sin servicios externos, verificar textos `No disponible`/`Módulo en preparación`, provocar el seam de error recuperable y comprobar shell abierta, mensaje amigable y log técnico sin secretos.

### Tests for User Story 3

- [ ] T027 [P] [US3] Escribir primero pruebas fallidas de enumeraciones y mensaje en `tests/SysTools.Presentation.Tests/Shell/OperationalMessageTests.cs`: AvailabilityStatus incluye Unavailable/Pending/Available/Error; MessageSeverity incluye Information/Warning/Error; Text no vacío y AccessibilityLabel resume severidad/contenido
- [ ] T028 [P] [US3] Escribir primero pruebas fallidas de estados/error recuperable durante la inicialización del módulo inicial en `tests/SysTools.Presentation.Tests/Shell/ShellStatusTests.cs`: conexión/licencia Unavailable, mensaje inicial `Módulo en preparación`, el fallo de inicialización reemplaza el mensaje sin perder o recuperando ActiveModule y el texto visible no contiene tipo, ruta o stack trace

### Implementation for User Story 3

- [ ] T029 [US3] Implementar `AvailabilityStatus`, `MessageSeverity` y `OperationalMessage` en `src/Presentation/Shell/Models/OperationalMessage.cs` después de confirmar T027/T028 fallidas, con Text no vacío, severidad obligatoria y AccessibilityLabel sin secretos, rutas, tipos internos o stack traces
- [ ] T030 [US3] Extender `src/Presentation/Shell/ViewModels/ShellViewModel.cs` con ConnectionStatus/LicenseStatus Unavailable, StatusMessage informativo inicial y un seam inyectable que provoque y traduzca fallos recuperables durante la inicialización del módulo inicial, conservando o recuperando ActiveModule
- [ ] T031 [US3] Enlazar y completar únicamente en `src/Presentation/Shell/Views/ShellWindow.xaml` la superficie inferior ya reservada con `Conexión: No disponible`, `Licencia: No disponible` y mensaje operativo, usando texto/etiqueta accesible además del color y sin duplicar la barra en `PriceVerifierView.xaml`
- [ ] T032 [US3] Configurar Serilog y manejo de excepciones en `src/Presentation/App.xaml.cs`: archivo diario `%LOCALAPPDATA%/SysTools/Logs/systools-.log`, Information, 14 archivos, contexto Application=SysTools, eventos de inicio/carga/cierre, Stage=Initialization y ModuleId para el fallo recuperable, y `DispatcherUnhandledException` con cierre solo para errores fatales
- [ ] T033 [US3] Ejecutar pruebas US3 y documentar el fallo recuperable durante la inicialización del módulo inicial, la permanencia de la shell y la revisión del log con Stage=Initialization/ModuleId, sin secretos ni stack trace visible, en `specs/001-systools-shell-base/validation/us3-status-and-errors.md`

**Checkpoint**: Estados neutrales, mensajes y logging cumplen el contrato sin implementar conexión ni licencia.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Comprobar composición, arquitectura, calidad visual, rendimiento y límites de alcance.

- [ ] T034 [P] Agregar pruebas de `RelayCommand`/notificación de propiedades en `tests/SysTools.Presentation.Tests/Mvvm/MvvmInfrastructureTests.cs`
- [ ] T035 [P] Agregar pruebas de composición en `tests/SysTools.Presentation.Tests/Composition/DependencyInjectionTests.cs` que resuelvan ShellWindow/ShellViewModel sin servicios externos y registren/muestren un módulo demostrativo en el área central sin modificar PriceVerifierView ni crear otra ventana
- [ ] T036 [P] Agregar una prueba de arquitectura que rechace referencias de Data desde Views/ViewModels y dependencias de Entities hacia otras capas en `tests/SysTools.Presentation.Tests/Architecture/LayerDependencyTests.cs`
- [ ] T037 Revisar `src/Presentation/Shell/Views/ShellWindow.xaml` y `src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml` con nombres y mensajes de al menos 200 caracteres para verificar ajuste/truncamiento sin romper el layout, y eliminar errores de binding, valores duplicados, code-behind no visual y problemas de tabulación/automatización accesible
- [ ] T038 Ejecutar restore/build/test Debug de `SysTools.sln` y registrar comandos, duración, warnings y resultados en `specs/001-systools-shell-base/validation/build-and-tests.md`
- [ ] T039 Medir diez arranques Release de `src/Presentation/SysTools.Presentation.csproj` en Windows 11 x64 con al menos 4 procesadores lógicos, 8 GB RAM y SSD, confirmar shell lista en menos de 2 segundos y registrar hardware, método y resultados en `specs/001-systools-shell-base/validation/startup-performance.md`
- [ ] T040 Ejecutar íntegramente `specs/001-systools-shell-base/quickstart.md` y registrar evidencia de 1280 × 720, pantalla completa, escalado 125%, textos de al menos 200 caracteres, foco, controles deshabilitados, logging y error recuperable en `specs/001-systools-shell-base/validation/quickstart-results.md`
- [ ] T041 Auditar el alcance con búsquedas de Firebird, configuración, licencia funcional, búsqueda de productos, ZXing, FastReport e impresión, y documentar ausencia de implementaciones fuera de alcance en `specs/001-systools-shell-base/validation/scope-audit.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 — Setup**: Sin dependencias; T001 bloquea comandos posteriores y T002–T004 son secuenciales.
- **Phase 2 — Foundational**: Depende de Setup; bloquea todas las historias.
- **US1 (Phase 3)**: Depende de Foundational y entrega el MVP/la shell anfitriona.
- **US2 (Phase 4)**: Sus pruebas/ViewModel pueden prepararse tras Foundational, pero su integración visual T025 depende de US1.
- **US3 (Phase 5)**: Sus modelos/pruebas pueden prepararse tras Foundational, pero la integración T030–T033 depende de US1.
- **Polish (Phase 6)**: Depende de US1, US2 y US3 completas.

### User Story Dependency Graph

```text
Setup → Foundational → US1 (MVP) ─┬→ US2 ─┐
                                  └→ US3 ─┴→ Polish
```

### Within Each User Story

- Escribir las pruebas de la historia y confirmar que fallan antes de implementar.
- Crear modelos/estado antes de ViewModels consumidores.
- Crear ViewModels antes de completar bindings e integración XAML.
- Ejecutar pruebas automáticas antes de documentar la validación manual.
- No iniciar funciones excluidas para satisfacer visualmente un escenario.

### Parallel Opportunities

- T005 puede ejecutarse en paralelo con T006 después de crear la solución.
- T008, T009 y T010 pueden ejecutarse en paralelo después de T007/T002 según corresponda.
- T013 y T014 pueden escribirse en paralelo; T015 y T017 usan archivos distintos.
- T021 y la estructura XAML de T023 pueden avanzar en paralelo; T022 comienza únicamente después de ejecutar T021 y confirmar que falla.
- T027 y T028 pueden escribirse en paralelo; T029 comienza únicamente después de ejecutar ambas y confirmar que fallan.
- Tras US1, US2 y US3 pueden avanzar en paralelo por equipos distintos.
- T034, T035 y T036 pueden ejecutarse en paralelo al inicio de Polish.

---

## Parallel Example: User Story 1

```text
Task T013: Pruebas de UtilityModuleItem en tests/SysTools.Presentation.Tests/Shell/UtilityModuleItemTests.cs
Task T014: Pruebas de ShellViewModel en tests/SysTools.Presentation.Tests/Shell/ShellViewModelTests.cs

Después de confirmar que fallan:
Task T015: Modelo de navegación en src/Presentation/Shell/Models/UtilityModuleItem.cs
Task T017: Vista shell en src/Presentation/Shell/Views/ShellWindow.xaml
```

## Parallel Example: User Story 2

```text
Task T021: Pruebas del placeholder en tests/SysTools.Presentation.Tests/PriceVerifier/PriceVerifierViewModelTests.cs
Task T023: Estructura XAML en src/Presentation/Modules/PriceVerifier/Views/PriceVerifierView.xaml
```

## Parallel Example: User Story 3

```text
Task T027: Pruebas de OperationalMessage en tests/SysTools.Presentation.Tests/Shell/OperationalMessageTests.cs
Task T028: Pruebas de estado de shell en tests/SysTools.Presentation.Tests/Shell/ShellStatusTests.cs

Después de confirmar que ambas fallan:
Task T029: Modelo de estado en src/Presentation/Shell/Models/OperationalMessage.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Setup.
2. Completar Foundational.
3. Completar US1.
4. Detenerse y validar 10 arranques, selección inicial y navegación deshabilitada.
5. Demostrar la shell modular antes de construir el placeholder completo.

### Incremental Delivery

1. Setup + Foundational → base compilable y MVVM compartido.
2. US1 → shell funcional y navegación modular (MVP).
3. US2 → placeholder visual completo sin interacción.
4. US3 → estados, mensajes y observabilidad.
5. Polish → evidencia integral y auditoría de alcance.

### Parallel Team Strategy

1. El equipo completa Setup/Foundation conjuntamente.
2. US1 se completa primero porque provee el host visual.
3. Después de US1, un responsable ejecuta US2 y otro US3.
4. Las pruebas de arquitectura/composición y la documentación de validación se consolidan en Polish.

---

## Notes

- `[P]` solo aparece cuando los archivos no se solapan y no existe dependencia incompleta inmediata.
- `[US1]`, `[US2]`, `[US3]` mantienen trazabilidad con spec.md.
- Presentation es la única capa con estado visual; no crear entidades de dominio ficticias.
- Data solo puede aparecer en el composition root de Presentation.
- No instalar ni simular Firebird, licencias, configuración local, ZXing, FastReport o impresión.
- El entorno debe contar con .NET 10 SDK antes de ejecutar la implementación.
