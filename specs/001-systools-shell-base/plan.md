# Implementation Plan: Base de shell modular de SysTools

**Branch**: `001-systools-shell-base` | **Date**: 2026-09-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-systools-shell-base/spec.md`

## Summary

Crear una solución Windows Desktop desde cero con una aplicación WPF que abra directamente una shell modular de SysTools. La shell mostrará navegación lateral, cargará el ViewModel del Verificador de precios mediante un `ContentControl` y plantillas de datos, y mantendrá visibles pero deshabilitadas las utilerías futuras. El placeholder reproducirá la intención visual de `docs/mockup-shell-utilerias.svg` sin conectar Firebird, configuración, licencia, reportes o impresión.

La solución separará Presentation, Business, Data y Entities en proyectos independientes. Presentation contendrá XAML, estilos, ViewModels, comandos, navegación y el composition root; Business quedará preparado para servicios y abstracciones; Data para implementaciones externas; Entities permanecerá libre de UI e infraestructura. El arranque usará el host de extensiones de .NET para componer dependencias y conectará Serilog a un archivo rotativo bajo AppData.

## Technical Context

**Language/Version**: C# sobre .NET 10 LTS (`net10.0-windows` para Presentation; `net10.0` para bibliotecas sin UI)

**Primary Dependencies**: WPF, Microsoft.Extensions.Hosting, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Serilog, Serilog.Extensions.Hosting y Serilog.Sinks.File

**Storage**: Sin persistencia de negocio; únicamente logs rotativos en `%LOCALAPPDATA%/SysTools/Logs`

**Testing**: xUnit con `dotnet test` en un proyecto Presentation.Tests para estado, comandos, ViewModels, registro de módulos y composición; quickstart manual para layout WPF, accesibilidad básica y comparación con el mockup

**Target Platform**: Windows 10/11 x64 con .NET 10 Desktop Runtime; desarrollo con .NET 10 SDK y soporte de escritorio

**Project Type**: Aplicación desktop WPF con cuatro proyectos de producción y proyectos de prueba separados

**Performance Goals**: Shell Release lista para interacción en menos de 2 segundos durante 10 arranques consecutivos en Windows 11 x64 con al menos 4 procesadores lógicos, 8 GB RAM y SSD; cambios de módulo sin trabajo bloqueante en el hilo de UI; diseño fluido desde 1280 × 720 hasta pantalla completa

**Constraints**: Local y offline; una ventana principal; mínimo 1280 × 720; sin persistir preferencias; sin Firebird, licencias, configuración, búsqueda, códigos de barras, reportes o impresión; mensajes sin stack traces ni secretos

**Scale/Scope**: Una shell, un módulo placeholder activo, seis opciones futuras deshabilitadas del mockup, estados visuales globales y un catálogo en memoria preparado para módulos posteriores

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-design gate

| Principle | Evaluation | Evidence / decision |
|---|---|---|
| I. Arquitectura de 3 capas + Entities | PASS | Cuatro proyectos separados. Presentation consume Business; Business define futuros contratos; Data los implementa. Presentation → Data se limita al composition root, nunca a Views o ViewModels. |
| II. WPF + MVVM sin lógica de negocio en Views | PASS | Views contienen XAML, recursos y bindings. ViewModels exponen estado. La navegación usa binding y DataTemplates, sin lógica operativa en code-behind. |
| III. Migración incremental guiada por Spec Kit | PASS | Solo cubre base arquitectónica, shell y placeholder. Las integraciones posteriores permanecen fuera de alcance. |
| IV. Compatibilidad funcional y mejora del legado | PASS | No se migran reglas funcionales; se mejora separación, testabilidad, trazabilidad y manejo de errores. |
| V. Seguridad, logging y configuración controlada | PASS | Serilog registra eventos estructurados sin secretos en AppData. Los errores recuperables producen mensaje controlado y los fatales quedan registrados. No se crea configuración local. |
| Restricciones técnicas | PASS | C#, .NET 10 LTS, WPF, MVVM, DI de Microsoft y Serilog. No se introduce ORM, Firebird, ZXing ni motor de reportes. |
| Validación mínima por capa | PASS | Pruebas unitarias para estado/comandos/ViewModels y quickstart manual de Presentation. Data y Business aún no contienen comportamiento operativo. |

No hay violaciones que requieran Complexity Tracking.

### Post-design gate

PASS. `research.md`, `data-model.md`, `contracts/ui-contract.md` y `quickstart.md` conservan las fronteras anteriores. Los modelos son estado de presentación en memoria, no entidades persistentes; no se agregaron accesos externos ni lógica de negocio a Views. El contrato fija estados neutrales, controles deshabilitados y navegación extensible. La guía cubre arranque, layout, composición, logging y recuperación proporcional al riesgo.

## Project Structure

### Documentation (this feature)

```text
specs/001-systools-shell-base/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ui-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # Se generará con $speckit-tasks
```

### Source Code (repository root)

```text
SysTools.sln
global.json

src/
├── Presentation/
│   ├── SysTools.Presentation.csproj
│   ├── App.xaml
│   ├── App.xaml.cs                     # Composition root y ciclo de vida
│   ├── Shell/
│   │   ├── Views/ShellWindow.xaml
│   │   ├── ViewModels/ShellViewModel.cs
│   │   └── Models/UtilityModuleItem.cs
│   ├── Modules/PriceVerifier/
│   │   ├── Views/PriceVerifierView.xaml
│   │   └── ViewModels/PriceVerifierViewModel.cs
│   ├── Views/
│   ├── ViewModels/ViewModelBase.cs
│   ├── Commands/RelayCommand.cs
│   └── Styles/
│       ├── Colors.xaml
│       ├── Typography.xaml
│       └── Controls.xaml
├── Business/
│   ├── SysTools.Business.csproj
│   └── Services/
├── Data/
│   ├── SysTools.Data.csproj
│   ├── Connection/
│   └── Repositories/
├── Entities/
│   └── SysTools.Entities.csproj
└── Reports/Templates/

tests/
└── SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj
```

**Structure Decision**: Se usan proyectos por frontera arquitectónica, no carpetas dentro de un único ensamblado. `SysTools.Presentation` referencia Business y Entities; referencia Data exclusivamente desde `App.xaml.cs` para componer implementaciones. Business referencia Entities. Data referencia Business y Entities. Entities no referencia ningún proyecto. `Reports/Templates` se reserva sin motor ni código hasta su feature.

## Dependency Rules

```text
Presentation Views ──bindings──> Presentation ViewModels
Presentation composition root ──> Business + Data registrations
Presentation ViewModels ────────> Business abstracciones/casos de uso
Business ───────────────────────> Entities
Data ───────────────────────────> Business abstracciones + Entities
Entities ───────────────────────> ninguna capa de SysTools
```

- Ninguna View o ViewModel puede referenciar tipos concretos de Data.
- `App.xaml.cs` es el único punto autorizado a conocer Presentation, Business y Data.
- Las carpetas vacías se conservan con archivos mínimos solo cuando Git lo requiera; no se crean servicios ficticios.
- El estado visual permanece en Presentation y no se promueve a Entities sin un concepto real de dominio.

## Runtime Composition

1. `App.OnStartup` crea el host y configura Serilog antes de resolver la ventana.
2. El contenedor registra `ShellWindow`, `ShellViewModel` y `PriceVerifierViewModel` como singletons para la única ventana y sesión.
3. `ShellViewModel` recibe el catálogo en memoria. Verificador está habilitado y activo; Impresión de etiquetas, Inventario rápido, Nueva utilería, Configuración, Licencias y Logs del sistema están visibles y deshabilitados.
4. `ShellWindow` usa un `ContentControl` enlazado a `ActiveModuleContent`; un DataTemplate relaciona `PriceVerifierViewModel` con `PriceVerifierView`.
5. El host inicia antes de mostrar la ventana, se detiene en `App.OnExit` y Serilog se vacía al finalizar.

## UI and Styling Strategy

- `MinWidth="1280"` y `MinHeight="720"`; Grid con sidebar estable y contenido central flexible.
- Colores, tipografías, bordes, radios y espaciados se centralizan en ResourceDictionaries.
- Tokens base del SVG: sidebar `#111827`, superficie `#F3F4F6`, tarjetas blancas, bordes `#D7DDE5`, acento `#2563EB`.
- Se usan Grid y tamaños relativos; no Canvas ni posicionamiento absoluto.
- Los campos de código de barras e información adicional y los controles futuros usan `IsEnabled="False"` por binding; no reciben foco, no admiten escritura y no ejecutan acciones.
- Conexión y licencia muestran `No disponible`; el mensaje inicial indica módulo en preparación.
- `ShellWindow` es la única propietaria de la zona inferior de estado/mensajes y la presenta debajo del contenido activo; `PriceVerifierView` no duplica esa barra ni usa bindings cruzados hacia ShellViewModel.
- Se mantienen contraste, tabulación, foco visible y nombres accesibles básicos.

## Logging and Failure Handling

- Serilog se configura en código porque la configuración local está fuera de alcance.
- Archivo diario `%LOCALAPPDATA%/SysTools/Logs/systools-.log`, nivel Information y 14 archivos retenidos.
- Eventos: inicio, carga de módulo, cierre y excepciones, con contexto `Application=SysTools`.
- `DispatcherUnhandledException` registra errores. Solo los recuperables se marcan controlados; los fatales se registran y cierran ordenadamente.
- Los mensajes visibles no incluyen tipos internos, rutas, stack traces ni valores sensibles.

## Testing Strategy

- **Presentation unitarias**: estado inicial, catálogo/selección, módulos deshabilitados, campos de captura no disponibles, valores neutrales, `RelayCommand.CanExecute`, notificaciones y error recuperable.
- **Composición**: construir el contenedor y resolver ShellWindow/ShellViewModel sin integraciones.
- **Arquitectura**: comprobar referencias y ausencia de Data en Views/ViewModels.
- **Manual WPF**: quickstart a 1280 × 720 y pantalla completa; mockup, foco, contraste, estados, controles deshabilitados y ausencia de landing.
- No se crean pruebas de funciones excluidas.

## Complexity Tracking

No aplica: el diseño cumple todas las puertas constitucionales sin excepciones.
