# Implementation Plan: Integracion operativa del verificador de precios

**Branch**: `[008-price-verifier-ui]` | **Date**: 2026-09-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/008-price-verifier-ui/spec.md`

**Note**: This template is filled in by the `$speckit-plan` command; its definition describes the execution workflow.

## Summary

Convertir el placeholder del Verificador de precios en el flujo operativo exacto por codigo. Un nuevo orquestador de Business carga y conserva privadamente la configuracion valida de la sesion, comprueba conexion y licencia, consulta el producto y aplica el formato de precio mediante servicios existentes. Presentation expone preparacion, consulta, Retry, cancelacion, estados accesibles y foco/select-all mediante MVVM, sin conocer credenciales, archivos, Firebird, criptografia ni reglas comerciales.

## Technical Context

**Language/Version**: C# 14 sobre .NET SDK 10.0.100 / .NET 10 LTS

**Primary Dependencies**: WPF; Microsoft.Extensions.DependencyInjection, Hosting y Logging 10.0.12; Serilog 4.4.0; servicios internos de configuracion, conexion, productos, formato de precio y licenciamiento ya implementados. No se agrega paquete nuevo.

**Storage**: Configuracion JSON protegida existente bajo AppData y Firebird existente, ambos consumidos exclusivamente mediante contratos Business/Data ya implementados; la sesion operativa solo conserva su configuracion valida en memoria y nunca la expone a Presentation.

**Testing**: xUnit 2.9.3 y Microsoft.NET.Test.Sdk 17.14.1 en `SysTools.BusinessRules.Tests` y `SysTools.Presentation.Tests`; dobles deterministas, pruebas WPF STA, matrices de estados/productos, single-flight, cancelacion, privacidad, arquitectura y recorrido manual guiado.

**Target Platform**: Windows Desktop x64/x86/Any CPU; `net10.0` en Entities/Business/Data y `net10.0-windows` con WPF en Presentation/pruebas visuales.

**Project Type**: Aplicacion WPF multiproyecto con 3 capas + Entities y shell modular.

**Performance Goals**: La UI permanece receptiva durante operaciones de hasta 6 segundos; exactamente una consulta por confirmacion aceptada; 100 consultas secuenciales sin duplicados ni resultados cruzados.

**Constraints**: Preparacion `configuracion -> conexion -> licencia`; solo `ConnectionTestStatus.Success` y licencia `IsValid` habilitan captura; no revalidar por codigo; Retry repite toda la preparacion; maximo 50 caracteres; una operacion vigente; confirmaciones durante busy se ignoran sin cola/cancelacion; continuaciones obsoletas no publican; mensajes/logs sin codigo, producto completo ni secretos; 1280x720 minimo; teclado y estados no dependientes solo de color.

**Scale/Scope**: Un modulo y una sesion operativa local, una captura vigente, cuatro familias de estado de preparacion, cinco resultados de consulta, matriz minima de 50 productos y 100 consultas; sin busqueda, edicion de configuracion, barcode visible, etiquetas, reportes o impresion.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **3 capas + Entities**: PASS. Entities agrega resultados inmutables; Business concentra la orquestacion y mantiene privada la configuracion; Data no cambia; Presentation consume un unico contrato de flujo.
- **WPF + MVVM**: PASS. El ViewModel expone estado/comandos y la View se limita a bindings, eventos de ciclo visual y foco/select-all. No existe acceso directo a datos, archivos, criptografia o reglas comerciales.
- **Migracion incremental Spec Kit**: PASS. Feature 008 corresponde a UI despues de configuracion, Firebird, repositorios, reglas, licencia y barcode, y antes de busqueda/configuracion visual/reportes.
- **Compatibilidad funcional y mejora**: PASS. Reutiliza consulta exacta y formato heredados; mejora asincronia, single-flight, cancelacion, accesibilidad, privacidad y recuperacion sin cambiar reglas comerciales.
- **Stack tecnico**: PASS. Mantiene C#/.NET 10, WPF, MVVM, Repository, DI y Serilog sin dependencia adicional.
- **Seguridad y logging**: PASS. `AppConfiguration` nunca cruza a Presentation; diagnostico estructurado usa solo etapa/categoria/estado/duracion; mensajes no incluyen secretos ni excepciones.
- **Validacion proporcional**: PASS condicionado a matrices de preparacion, 50 productos, 100 consultas, carreras/cancelacion, WPF STA, auditoria de logs/capas, quickstart visual y regresion completa.
- **Post-design**: PASS. El modelo conserva datos seguros, el contrato impide infraestructura en UI y el quickstart cubre validacion automatizada y manual sin ampliar alcance.

## Project Structure

### Documentation (this feature)

```text
specs/008-price-verifier-ui/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- price-verifier-workflow-contract.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md
```

### Source Code (repository root)

```text
src/
|-- Entities/
|   `-- PriceVerifier/
|       |-- PriceVerifierPreparationStatus.cs
|       |-- PriceVerifierPreparationResult.cs
|       |-- PriceVerifierLookupStatus.cs
|       `-- PriceVerifierLookupResult.cs
|-- Business/
|   `-- PriceVerifier/
|       |-- IPriceVerifierWorkflow.cs
|       `-- PriceVerifierWorkflow.cs
`-- Presentation/
    |-- App.xaml.cs
    |-- Commands/
    |   `-- AsyncRelayCommand.cs
    |-- Modules/PriceVerifier/
    |   |-- ViewModels/PriceVerifierViewModel.cs
    |   `-- Views/
    |       |-- PriceVerifierView.xaml
    |       `-- PriceVerifierView.xaml.cs
    |-- Shell/
    |   |-- Services/IAsyncModuleLifecycle.cs
    |   |-- ViewModels/ShellViewModel.cs
    |   `-- Views/ShellWindow.xaml.cs
    `-- Styles/Controls.xaml

tests/
|-- SysTools.BusinessRules.Tests/
|   `-- PriceVerifier/
|       |-- PriceVerifierPreparationTests.cs
|       |-- PriceVerifierLookupTests.cs
|       |-- PriceVerifierReliabilityTests.cs
|       `-- PriceVerifierSecurityTests.cs
`-- SysTools.Presentation.Tests/
    |-- PriceVerifier/
    |   |-- PriceVerifierViewModelTests.cs
    |   |-- PriceVerifierLifecycleTests.cs
    |   `-- PriceVerifierAccessibilityTests.cs
    |-- Mvvm/MvvmInfrastructureTests.cs
    |-- Shell/ShellStatusTests.cs
    |-- Composition/DependencyInjectionTests.cs
    `-- Architecture/LayerDependencyTests.cs
```

**Structure Decision**: Mantener los cuatro proyectos productivos y las suites existentes. El orquestador de Business evita que Presentation reciba `AppConfiguration` o coordine infraestructura. Los resultados seguros viven en Entities; Data no requiere codigo nuevo. Las pruebas de flujo puro se incorporan a BusinessRules y las de comandos, lifecycle, bindings, shell y layout a Presentation.

## Complexity Tracking

No hay violaciones constitucionales que requieran excepcion.
