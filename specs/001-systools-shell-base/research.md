# Research: Base de shell modular de SysTools

## 1. Plataforma y SDK

**Decision**: .NET 10 LTS con `global.json` desde `10.0.100`, `rollForward: latestFeature` y `allowPrerelease: false`.

**Rationale**: Es el stack obligatorio y está en soporte activo. La política permite parches/feature bands 10.0 compatibles. El equipo actual solo tiene SDK 8.0.204, así que instalar .NET 10 es prerrequisito, no motivo para degradar la feature.

**Alternatives considered**: .NET 8 (contradice constitución); patch exacto (dificulta parches de seguridad); sin `global.json` (permitiría seleccionar otro major).

**Sources**: [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy), [`global.json` overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)

## 2. Proyectos y fronteras

**Decision**: Cuatro proyectos: Presentation, Business, Data y Entities. Presentation es ejecutable/composition root; Business alberga casos de uso y contratos futuros; Data los implementa; Entities conserva modelos simples. Presentation conoce Data solo en `App.xaml.cs`.

**Rationale**: Los ensamblados hacen verificables los límites. Un composition root evita un quinto proyecto sin permitir Data en Views/ViewModels. No se crean servicios, repositorios o entidades ficticios.

**Alternatives considered**: proyecto único (límites débiles); quinto Bootstrapper (complejidad prematura); Data en ViewModels (prohibido).

## 3. Ciclo de vida, DI y navegación

**Decision**: `Host.CreateApplicationBuilder`, Microsoft.Extensions.DependencyInjection y lifecycle desde `App.OnStartup`/`OnExit`. Navegación con catálogo `UtilityModuleItem`, `ActiveModuleContent`, `ContentControl` y DataTemplates ViewModel→View.

**Rationale**: El host centraliza servicios, logging y disposición. DataTemplates mantienen navegación declarativa y evitan service locator. La documentación oficial define `IServiceCollection` para registro y binding como separación UI/estado.

**Alternatives considered**: `ServiceCollection` sin host (más lifecycle manual); service locator (dependencias ocultas); cambio de Views en code-behind (rompe MVVM); framework MVVM adicional (innecesario ahora).

**Sources**: [Service registration](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-registration), [WPF data binding](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/)

## 4. Logging estructurado

**Decision**: Serilog integrado al host, sink diario en `%LOCALAPPDATA%/SysTools/Logs`, Information, retención de 14 archivos y configuración en código.

**Rationale**: Un sink persistente permite soporte offline; rotación/retención limitan crecimiento. Configuración programática evita implementar prematuramente configuración local. El logger se crea antes de la ventana y se vacía al salir.

**Alternatives considered**: consola/Debug (no persistente); Event Log (permisos/operación); JSON en AppData (feature futura); archivo ilimitado (crecimiento no acotado).

**Sources**: [Serilog configuration basics](https://github.com/serilog/serilog/wiki/configuration-basics), [Serilog file sink](https://github.com/serilog/serilog-sinks-file/blob/dev/README.md)

## 5. Estado del placeholder

**Decision**: Estado en ViewModels de Presentation. Valores iniciales: descripción/presentación/existencia/precio `—`, conexión/licencia `No disponible`, mensaje `Módulo en preparación`; código de barras, información adicional y acciones futuras visibles pero deshabilitados.

**Rationale**: Es estado visual, no dominio. El guion evita interpretar `$0` como precio consultado. Deshabilitar toda captura evita aparentar una consulta o persistencia inexistente.

**Alternatives considered**: `$0` (resultado engañoso); estados exitosos/invalidación del SVG (sin validación); ocultar controles (contradice aclaración).

## 6. Pruebas

**Decision**: xUnit para ViewModels, comandos, catálogo y composición, más quickstart manual WPF. Sin UI Automation end-to-end aún.

**Rationale**: Las reglas puras se protegen con `dotnet test`; layout, DPI y foco requieren inspección real. UI end-to-end sería coste/fragilidad antes de flujos operativos.

**Alternatives considered**: solo manual (no protege estados); UI Automation completa (prematura); pruebas Data (sin implementaciones).

**Sources**: [.NET testing](https://learn.microsoft.com/en-us/dotnet/core/testing/), [xUnit tutorial](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-xunit)

## 7. Layout y estilos

**Decision**: Grid, tamaños relativos, mínimo 1280 × 720 y tokens en ResourceDictionaries; sin Canvas ni coordenadas del SVG.

**Rationale**: WPF soporta layout independiente de resolución. El SVG define intención, no reproducción píxel por píxel.

**Alternatives considered**: coordenadas absolutas (frágiles ante resize/DPI); scroll global permanente (oculta mala adaptación).

**Sources**: [WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/), [WPF data binding](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/)
