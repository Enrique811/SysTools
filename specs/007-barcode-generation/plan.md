# Implementation Plan: Generacion de codigos de barras

**Branch**: `[007-barcode-generation]` | **Date**: 2026-09-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/007-barcode-generation/spec.md`

**Note**: This template is filled in by the `$speckit-plan` command; its definition describes the execution workflow.

## Summary

Migrar la deteccion y generacion Java de EAN-8, EAN-13, UPC-A y CODE128 a un servicio de Business que normaliza la entrada, valida checksums, calcula dimensiones y devuelve un resultado inmutable. La codificacion y serializacion PNG se aislan detras de una abstraccion implementada en Data con ZXing.Net para Windows; Presentation solo registra dependencias. Se limita la entrada normalizada a 50 caracteres, se acepta el repertorio ASCII que CODE128 puede reproducir exactamente y se convierten fallos recuperables en estados seguros sin exponer el codigo.

## Technical Context

**Language/Version**: C# 14 sobre .NET 10 LTS

**Primary Dependencies**: `ZXing.Net.Bindings.Windows.Compatibility` 0.16.16 en Data y pruebas de decodificacion (incluye ZXing.Net y System.Drawing.Common); `Microsoft.Extensions.Logging.Abstractions` 10.0.12 y DI ya presentes

**Storage**: N/A; la imagen PNG vive solo en memoria y no se escribe en archivos ni base de datos

**Testing**: xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1 y `dotnet test`; matriz de checksums, round-trip con lector ZXing independiente, fallos inyectados, cancelacion, inmutabilidad, logging y rendimiento

**Target Platform**: Windows Desktop x64/x86/Any CPU; `net10.0` en Entities/Business/Data y `net10.0-windows` en pruebas de barcode/Presentation

**Project Type**: Aplicacion WPF en solucion multiproyecto de 3 capas + Entities

**Performance Goals**: Al menos 95% de entradas operativas en menos de 100 ms; 1,000 generaciones consecutivas sin salida corrupta, dimensiones incorrectas ni referencias mutables compartidas

**Constraints**: Normalizacion Java exacta que recorta `U+0000..U+0020` solo en extremos; entrada normalizada maxima de 50 caracteres; CODE128 solo ASCII `U+0000` a `U+007F`, excluyendo marcadores FNC reservados; ancho maximo 900 px, alto maximo 60 px, margen cero y PNG estructuralmente completo de maximo 1 MiB; mensajes seguros; sin UI, Firebird, archivos, reportes ni impresoras; cancelacion propagada; logs sin contenido ni bytes

**Scale/Scope**: Una imagen por llamada, cuatro simbologias, cinco estados finales, dimensiones fijas/deterministas y un proyecto de pruebas dedicado; sin cache, lotes, persistencia, vista previa o impresion

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **3 capas + Entities**: PASS. Entities contiene tipos y resultado; Business clasifica, valida y coordina; Data encapsula ZXing/System.Drawing; Presentation solo compone.
- **WPF + MVVM**: PASS. No se agregan Views ni ViewModels y ninguna capa visual genera imagenes directamente.
- **Migracion incremental Spec Kit**: PASS. Es la feature 007 posterior a licenciamiento y anterior a UI/reportes, conforme al orden constitucional.
- **Compatibilidad funcional y mejora**: PASS. Conserva checksums, fallback, dimensiones, margen y PNG del Java; mejora inmutabilidad, limites, cancelacion, estados tipados y privacidad de logs.
- **Stack tecnico**: PASS. Usa C#/.NET 10, DI existente y ZXing.Net, la dependencia de barcode exigida por la constitucion.
- **Seguridad y logging**: PASS. Los fallos incluyen mensajes amigables estables; los logs solo incluyen etapa, tipo, categoria y longitud, nunca contenido o bytes.
- **Validacion minima**: PASS condicionado a matriz de al menos 100 casos, round-trip de los cuatro formatos, pruebas de fallos/cancelacion/inmutabilidad, 1,000 generaciones, auditoria de logs y regresion completa.
- **Post-design**: PASS. `data-model.md` mantiene Entities simples, `contracts/barcode-contract.md` fija las fronteras y `quickstart.md` valida la feature sin UI ni recursos externos.

## Project Structure

### Documentation (this feature)

```text
specs/007-barcode-generation/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- tasks.md
|-- validation/
|-- contracts/
|   `-- barcode-contract.md
`-- checklists/
    `-- requirements.md
```

### Source Code (repository root)

```text
src/
|-- Entities/
|   `-- Barcodes/
|       |-- BarcodeType.cs
|       |-- BarcodeGenerationStatus.cs
|       `-- BarcodeGenerationResult.cs
|-- Business/
|   `-- Barcodes/
|       |-- IBarcodeService.cs
|       |-- IBarcodeImageEncoder.cs
|       |-- BarcodeEncodingException.cs
|       |-- PngOutputValidator.cs
|       |-- BarcodeClassifier.cs
|       `-- BarcodeService.cs
|-- Data/
|   `-- Barcodes/
|       `-- ZxingBarcodeImageEncoder.cs
`-- Presentation/
    `-- App.xaml.cs

tests/
|-- SysTools.Barcodes.Tests/
|   |-- Compatibility/
|   |-- Business/
|   |-- Data/
|   |-- Entities/
|   |-- Performance/
|   |-- Security/
|   `-- TestDoubles/
`-- SysTools.Presentation.Tests/
    |-- Architecture/
    `-- Composition/
```

**Structure Decision**: Conservar los cuatro proyectos de produccion. El resultado publico y sus enums viven en Entities sin ZXing ni tipos graficos. Business conserva las reglas puras y define el puerto de codificacion. Data implementa ese puerto con ZXing.Net y serializa PNG para Windows. Presentation se limita a registrar el encoder y el servicio. Un proyecto de pruebas dedicado puede referenciar las tres capas y usar el lector ZXing para verificar round-trip sin debilitar la regla que prohibe ZXing dentro de Business.

## Complexity Tracking

No hay violaciones constitucionales que requieran excepcion.
