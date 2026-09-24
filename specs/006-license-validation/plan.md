# Implementation Plan: Validacion de licencias heredadas

**Branch**: `[006-license-validation]` | **Date**: 2026-09-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/006-license-validation/spec.md`

**Note**: This template is filled in by the `$speckit-plan` command; its definition describes the execution workflow.

## Summary

Migrar el validador Java de licencias a un servicio asincrono de Business que carga JSON directo o mediante una abstraccion de Data, reproduce el payload canonico y verifica RSA SHA-256/PKCS#1 con las dos llaves publicas heredadas. El servicio consulta un proveedor sustituible de UUID Windows y el `IServerClockRepository` existente, devuelve un resultado inmutable y seguro de Entities y propaga cancelacion. Presentation solo registra las dependencias; la UI y el bloqueo de operaciones quedan fuera de esta feature.

## Technical Context

**Language/Version**: C# 14 sobre .NET 10 LTS

**Primary Dependencies**: BCL (`System.Text.Json`, `System.Security.Cryptography`, `System.Diagnostics`, `System.Globalization`), `Microsoft.Extensions.Logging.Abstractions` 10.0.12 y DI ya presente; sin nuevos paquetes de produccion

**Storage**: JSON directo o archivo `.lic` UTF-8 de solo lectura; Firebird remoto existente se usa unicamente mediante `IServerClockRepository`; sin migraciones ni escrituras

**Testing**: xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1 y `dotnet test`; vectores criptograficos fijos y dobles de archivo, UUID, reloj y logging

**Target Platform**: Windows Desktop x64/x86/Any CPU; `net10.0` en Entities/Business/Data y `net10.0-windows` en pruebas/Presentation

**Project Type**: Aplicacion WPF en solucion multiproyecto de 3 capas + Entities

**Performance Goals**: Rechazos previos a Firebird por debajo de 50 ms p95; validacion local completa por debajo de 250 ms p95 con dobles; maximo una lectura de UUID y una consulta de servidor por validacion elegible

**Constraints**: Compatibilidad binaria con dos llaves X.509 heredadas; SHA-256 con RSA PKCS#1 v1.5; UTF-8 y `LF`; formato estricto `yyyy-MM-dd HH:mm:ss`; limites inclusivos; sin reloj local; sin llave privada; sin payload, firma, UUID completo, ruta, credenciales o excepcion en resultados/logs; cancelacion propagada

**Scale/Scope**: Una licencia por operacion, cuatro campos firmados, dos emisores, diez estados de rechazo y cuatro historias; no hay UI, cache, renovacion, revocacion, activacion en linea ni generacion

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **3 capas + Entities**: PASS. Entities solo modela estados/resultados; Business coordina reglas y contratos; Data implementa archivo y plataforma; Presentation solo compone.
- **WPF + MVVM**: PASS. No se modifica ninguna View ni ViewModel y no se agrega logica de licencia a UI.
- **Migracion incremental Spec Kit**: PASS. Es la feature 006 posterior a repositorios y reglas, y anterior a barcode/UI/reportes conforme al roadmap.
- **Compatibilidad funcional y mejora**: PASS. Reproduce campos, canonicalizacion, algoritmo, llaves, UUID y reloj del Java; mejora parsing, asincronia, cancelacion, tipado y seguridad diagnostica.
- **Seguridad, logging y configuracion**: PASS. Solo se distribuyen llaves publicas; el valor opaco de configuracion no se registra; todos los fallos visibles usan codigos/mensajes controlados.
- **Firebird y tiempo**: PASS. Reutiliza el repositorio de reloj ya probado, no abre conexiones desde Business y no usa `DateTime.Now`/`UtcNow`.
- **Validacion minima**: PASS condicionado a pruebas de vectores criptograficos, estados, bordes temporales, comandos UUID, cancelacion, DI, arquitectura y auditoria de centinelas.
- **Post-design**: PASS. `data-model.md` mantiene valores simples; `contracts/licensing-contract.md` separa las tres capas; `quickstart.md` valida sin UI, equipo real o Firebird real.

## Project Structure

### Documentation (this feature)

```text
specs/006-license-validation/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- licensing-contract.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md
```

### Source Code (repository root)

```text
src/
|-- Entities/
|   `-- Licensing/
|       |-- LicenseIssuer.cs
|       |-- LicenseValidationStatus.cs
|       `-- LicenseValidationResult.cs
|-- Business/
|   `-- Licensing/
|       |-- IHardwareIdProvider.cs
|       |-- ILicenseSourceReader.cs
|       |-- ILicenseService.cs
|       |-- LicenseDocumentParser.cs
|       |-- LegacyLicenseSignatureVerifier.cs
|       `-- LicenseService.cs
|-- Data/
|   `-- Licensing/
|       |-- FileLicenseSourceReader.cs
|       `-- WindowsHardwareIdProvider.cs
`-- Presentation/
    `-- App.xaml.cs

tests/
|-- SysTools.Licensing.Tests/
|   |-- Compatibility/
|   |-- Business/
|   |-- Data/
|   |-- Entities/
|   |-- Security/
|   `-- TestDoubles/
`-- SysTools.Presentation.Tests/
    |-- Architecture/
    `-- Composition/
```

**Structure Decision**: Conservar los cuatro proyectos de produccion. El resultado publico vive en Entities sin criptografia ni serializacion. Business define interfaces para toda entrada externa, contiene parsing/canonicalizacion/verificacion y coordina el reloj ya abstraido. Data implementa lectura de archivo y ejecucion controlada de comandos Windows. Se agrega un proyecto de pruebas dedicado y solo se toca el composition root de Presentation.

## Complexity Tracking

No hay violaciones constitucionales que requieran excepcion.
