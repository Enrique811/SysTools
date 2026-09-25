# Quickstart validation: Reportes, etiquetas e impresión

## Automatizado

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
dotnet test SysTools.sln --no-build
```

Esperado: 0 warnings/errors/failures; hardware solo SKIPPED si opt-in incompleto.

## Preview

1. Configurar ambiente `a`, template compatible y columnas 1/2/3.
2. Consultar productos ficticios y activar `Ctrl+F8` hasta completar.
3. Confirmar progreso, una preview y cero delta de spool.
4. Cerrar con Escape y verificar foco listo.

## Producción controlada

1. Usar impresora no productiva allowlisted, papel correcto y `TEST / NO VENDER`.
2. Habilitar opt-in físico.
3. Ejecutar una fila/capacidad; confirmar tres jobs, cero diálogos/fallback.
4. Medir, revisar campos y escanear barcodes.
5. Registrar PASS/FAIL; entorno ausente es SKIPPED.

## Fallos/retry

- Template ausente/incompatible/no aprobado: cero render/spool.
- Printer ausente: cero fallback y fila reintentable.
- Submit indeterminado: no auto-retry.
- Doble `Ctrl+F8`: máximo un output.
- Cierre/cancelación tardía: cero publicación/job adicional.

## Seguridad/arquitectura

Auditar logs/mensajes con sentinels. Presentation no contiene filesystem, FastReport, ZXing, `System.Drawing.Printing`, `PrintDialog`, `PrintQueue` ni parsing.

## Cierre

La automatización puede quedar verde sin hardware, pero producción solo se aprueba cuando manifest, goldens, comparación heredada e impresión física estén PASS.
