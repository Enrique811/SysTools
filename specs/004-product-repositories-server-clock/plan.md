# Implementation Plan: Repositorios de productos y fecha del servidor

**Branch**: `004-product-repositories-server-clock` | **Date**: 2026-09-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-product-repositories-server-clock/spec.md`

## Summary

Agregar contratos de repositorio en Business, un modelo de producto libre de dependencias en Entities e implementaciones Firebird de solo lectura en Data. Las consultas reutilizaran la fabrica de conexiones de la feature 003, parametros enlazados, operaciones asincronas cancelables y disposicion determinista. Los errores de infraestructura se traduciran a una excepcion segura y estable para que ausencia de producto, busqueda vacia y fallo tecnico nunca sean equivalentes.

## Technical Context

**Language/Version**: C# 14 sobre .NET 10.0.401; proyectos `net10.0` y pruebas `net10.0-windows`

**Primary Dependencies**: `FirebirdSql.Data.FirebirdClient` 10.3.4, `Microsoft.Extensions.Logging.Abstractions` 10.0.12, DI existente de `Microsoft.Extensions.DependencyInjection`

**Storage**: Firebird remoto existente, tablas `PRODUCTOS` e `INVENTARIO_BALANCES`; `RDB$DATABASE` para `CURRENT_TIMESTAMP`; solo lectura y sin migraciones

**Testing**: xUnit 2.9.3, `Microsoft.NET.Test.Sdk` 17.14.1, dobles ADO.NET controlados e integracion Firebird opt-in

**Target Platform**: Windows 11 x64, aplicacion WPF; los proyectos Entities, Business y Data permanecen independientes de WPF

**Project Type**: Aplicacion desktop modular con arquitectura Presentation, Business, Data y Entities

**Performance Goals**: Las operaciones con dobles deben completar sin espera artificial; 10 consultas integradas consecutivas deben concluir dentro del timeout configurado de 5 segundos cada una

**Constraints**: Charset `ISO8859_1`; consultas parametrizadas; prefijo `{texto}%`; codigo maximo 50 caracteres; descripcion maxima 255; sin estado global; sin logs de configuracion, entradas o filas; no EF Core

**Scale/Scope**: Tres operaciones publicas de lectura, una entidad, una excepcion segura, dos implementaciones Data, un proyecto de pruebas y tres escenarios de integracion opt-in

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / restriction | Pre-design | Post-design | Evidence |
|---|---:|---:|---|
| I. 3 capas + Entities | PASS | PASS | `Product` y resultados simples en Entities; contratos y excepcion segura en Business; SQL y ADO.NET en Data; Presentation solo registra DI. |
| II. WPF + MVVM | PASS | PASS | No se agregan vistas, ViewModels ni logica de UI. |
| III. Migracion incremental | PASS | PASS | Es la feature 004 posterior a conexion Firebird y excluye reglas, licencia y UI. |
| IV. Compatibilidad y mejora | PASS | PASS | Conserva SQL principal, prefijo y fecha del servidor; elimina estado estatico, mensajes Swing y recursos globales. |
| V. Seguridad y logging | PASS | PASS | Parametros enlazados, logs por operacion/outcome/tipo/codigos y excepcion publica sin payload ni excepcion original. |
| Stack aprobado | PASS | PASS | .NET 10, Repository, FirebirdSql, DI y Serilog por `ILogger`; sin ORM. |
| Validacion Firebird obligatoria | PASS | PASS | Dobles cubren nulos, acentos, longitud, decimales, cancelacion y lifecycle; integracion opt-in documentada. |

No existen excepciones constitucionales ni complejidad que requiera justificacion.

## Project Structure

### Documentation (this feature)

```text
specs/004-product-repositories-server-clock/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- repository-contracts.md
|-- checklists/
|   `-- requirements.md
|-- validation/
`-- tasks.md
```

### Source Code (repository root)

```text
src/
|-- Entities/
|   `-- Products/Product.cs
|-- Business/
|   `-- Repositories/
|       |-- IProductRepository.cs
|       |-- IServerClockRepository.cs
|       |-- RepositoryAccessException.cs
|       `-- RepositoryOperation.cs
|-- Data/
|   `-- Repositories/
|       |-- FirebirdProductRepository.cs
|       |-- FirebirdServerClockRepository.cs
|       |-- FirebirdRepositoryDiagnostics.cs
|       `-- ProductDataReaderMapper.cs
`-- Presentation/
    `-- App.xaml.cs

tests/
`-- SysTools.Repositories.Tests/
    |-- Entities/
    |-- Contracts/
    |-- Data/
    |-- Integration/
    |-- Security/
    `-- TestDoubles/
```

**Structure Decision**: Mantener los cuatro proyectos existentes y agregar un proyecto de pruebas de feature. Business define contratos que reciben `AppConfiguration`; Data implementa consultas mediante `IFirebirdConnectionFactory`; Presentation actua solo como composition root. No se agrega servicio de negocio porque las reglas del verificador pertenecen a la feature 005.

## Design Decisions

- El modelo colapsa los alias heredados duplicados `CODIGO` e `IDENTIFICACION` en un unico `Id` numerico y conserva `CODIGO_BARRAS` como texto.
- `PriceWithTax` es `decimal?`; Data convierte desde el tipo numerico entregado por Firebird y no aplica formato ni escala comercial adicional.
- `Stock` permanece textual para conservar `Sin registro` y la representacion heredada de valores negativos o fraccionarios sin introducir una regla de presentacion posterior.
- Los metodos reciben una configuracion ya cargada. Esta feature no abre archivos ni recupera secretos por si misma.
- `RepositoryAccessException` contiene solamente operacion y mensaje seguro; la excepcion del proveedor se registra mediante tipo y codigos numericos y no se encadena hacia capas superiores.
- Un codigo vacio se rechaza como argumento invalido; una descripcion vacia devuelve una lista vacia sin crear conexion.
- Las consultas no agregan limite ni paginacion para conservar equivalencia; esa decision se revisara junto con la UI de busqueda.

## Phase 0: Research

Las decisiones y alternativas estan registradas en [research.md](./research.md). No quedan marcadores `NEEDS CLARIFICATION`.

## Phase 1: Design and Contracts

- [data-model.md](./data-model.md) define `Product`, `RepositoryOperation` y las reglas de normalizacion.
- [contracts/repository-contracts.md](./contracts/repository-contracts.md) fija firmas, resultados, cancelacion y semantica de fallos.
- [quickstart.md](./quickstart.md) define build, pruebas, auditorias e integracion Firebird de solo lectura.

El chequeo constitucional posterior al diseno permanece en PASS para todos los gates.
