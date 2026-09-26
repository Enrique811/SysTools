# Quickstart validation: Correos, soporte y logs

## Automatizado

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
dotnet test SysTools.sln --no-build --no-restore
```

Esperado: cero warnings, errors o failures; integraciones externas controladas pueden permanecer SKIPPED.

## Solicitud de licencia

1. Abrir “Soporte y logs”.
2. Confirmar UUID disponible y copiarlo explícitamente.
3. Preparar solicitud y abrir Default, Gmail y Outlook con dobles o control manual.
4. Confirmar un borrador por activación, destinatario editable y cero envío automático.

## Reporte de error

1. Capturar resumen y detalle con acentos, saltos y símbolos.
2. Preparar y abrir cada canal.
3. Confirmar versión y código opaco, y ausencia de secretos, rutas, UUID y adjuntos.

## Logs

1. Probar directorio ausente, vacío, 1, 20 y 25 archivos.
2. Confirmar orden y límite, y seleccionar un log grande.
3. Confirmar tail de máximo 500 líneas y 256 KiB.
4. Probar traversal, subdirectorio, extensión inválida y reparse point.
5. Abrir carpeta y confirmar root administrado exacto.

## UI y seguridad

- Operar con teclado a 1280×720 y 125%.
- Ejecutar 100 aperturas por canal y dobles clics.
- Auditar logs con sentinels; ningún payload, ruta, UUID o excepción adjunta.

