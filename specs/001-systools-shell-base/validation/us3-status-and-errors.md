# Validación US3 — estados y errores

- Conexión y licencia inician en `Unavailable` y se muestran como `No disponible`.
- El mensaje inicial es `Módulo en preparación` y expone una etiqueta accesible con severidad y contenido.
- El seam `IModuleInitializer` se probó con una excepción que contiene tipo, ruta y texto técnico. La shell conserva `price-verifier`; el mensaje visible solo indica que no fue posible preparar el módulo y no expone tipo, ruta ni stack trace.
- `ShellViewModel` registra el fallo con propiedades estructuradas `Stage=Initialization` y `ModuleId=price-verifier`.
- Serilog escribió el arranque real en `%LOCALAPPDATA%/SysTools/Logs/systools-20260920.log`; se observaron Startup, Initialization, `price-verifier` y Shutdown. La retención configurada es de 14 archivos diarios.

Resultado: aprobado.
