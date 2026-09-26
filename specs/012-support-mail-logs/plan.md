# Implementation Plan: Correos, soporte y logs

**Branch**: `[012-support-mail-logs]` | **Date**: 2026-09-25 | **Spec**: [spec.md](spec.md)

## Summary

Agregar un módulo administrativo WPF que componga borradores seguros de solicitud de licencia y reporte de error, abra Gmail, Outlook web o el cliente predeterminado mediante un puerto de infraestructura, y permita listar, previsualizar y localizar logs administrados. Business coordina hardware, composición y resultados tipados; Data limita filesystem y procesos; Presentation conserva MVVM y nunca recibe rutas privadas.

## Technical Context

**Language/Version**: C# / .NET 10 LTS

**Primary Dependencies**: WPF, Microsoft.Extensions.DependencyInjection/Logging, Serilog existente; `System.Diagnostics.Process` y filesystem del BCL; sin SMTP ni dependencia nueva

**Storage**: solo lectura de `%LocalAppData%\SysTools\Logs`; sin nueva persistencia

**Testing**: xUnit, pruebas de URI/codificación, dobles de hardware/launcher/store/clipboard, filesystem temporal, STA/contratos XAML, matrices de seguridad

**Target Platform**: Windows Desktop x64, 1280×720 o superior y escala 125%

**Project Type**: aplicación WPF en 3 capas + Entities

**Performance Goals**: catálogo <500 ms; preview acotada <2 s; composición/apertura <1 s sin contar aplicaciones externas; UI responsiva

**Constraints**: cero envío automático; destinatario vacío por defecto; 20 logs; 500 líneas/256 KiB; archivo <=50 MiB; top-level `.log`; cero rutas o payloads en logs internos; single-flight/cancelación

**Scale/Scope**: un módulo, tres canales de correo, dos tipos de borrador y un directorio local administrado

## Constitution Check

*GATE: PASS antes y después del diseño.*

- **3 capas + Entities**: PASS. Entities contiene resultados; Business contratos/workflow; Data filesystem/procesos; Presentation vista/VM.
- **WPF + MVVM**: PASS. La vista solo enlaza; el ViewModel no usa filesystem ni `Process`.
- **Migración incremental**: PASS. Corresponde a la etapa 11 posterior a reportes.
- **Compatibilidad y mejora**: PASS. Conserva soporte por correo y añade límites, estados tipados y privacidad.
- **Seguridad/logging**: PASS. No SMTP/credenciales, no adjuntos, rutas privadas ocultas y logging por metadatos.
- **Validación proporcional**: PASS. Matrices de canales, URI, traversal, límites, concurrencia, accesibilidad y regresión.

## Project Structure

```text
src/
├── Entities/Support/
├── Business/Support/
├── Data/Support/
└── Presentation/Modules/Support/

tests/
├── SysTools.BusinessRules.Tests/Support/
├── SysTools.Configuration.Tests/Support/
└── SysTools.Presentation.Tests/Support/
```

**Structure Decision**: Reutilizar los proyectos existentes. El acceso externo permanece en Data y el módulo se integra como contenido habilitado de la shell.

## Complexity Tracking

No existen violaciones constitucionales ni dependencias nuevas.

