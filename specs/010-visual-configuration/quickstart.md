# Quickstart validation: Configuración visual

## Automatizado

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
dotnet test SysTools.sln --no-build
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj --no-build --filter Category=FirebirdIntegration
```

Esperado: restore/build sin warnings; deterministas sin fallos; externas PASS o SKIPPED explícito.

## Primera configuración

1. Perfil aislado sin configuración: shell visible y un diálogo.
2. Password visual vacío y Save rechazado antes de prueba.
3. Capturar conexión/probar; cambiar servidor e invalidar.
4. Reprobar/guardar; verificar una re-preparación.

## Edición segura

1. Abrir desde administración y verificador.
2. Ver no sensibles y “credencial configurada”; password vacío.
3. Cambiar preferencias y guardar sin recapturar secreto.
4. Reabrir/inspeccionar JSON/logs con sentinel: cero secreto.
5. Cerrar dirty y probar Permanecer/Descartar.

## Licencia, UUID y opciones

1. Copiar UUID y simular fallo recuperable.
2. Importar licencia válida/inválida/vencida/otro equipo.
3. Confirmar inválida no reemplaza.
4. Seleccionar impresora/plantilla y conservar ausente como “No disponible”.
5. Confirmar cero preview/impresión.

## Recuperación/carreras

1. JSON malformado/secreto irrecuperable exige confirmación/password y respaldo.
2. Cancelar conserva bytes; confirmar produce documento válido.
3. Cerrar durante no cooperativa; respuesta antigua no publica.
4. Ejecutar matrices 20/50/100.

## Accesibilidad/manual

Solo teclado a 1280x720, maximizado y 125%: foco, Tab/Shift+Tab, access keys, Ctrl+S, Escape, scroll, errores, live status y barra fija. No capturar evidencia con UUID, paths, licencia o password.
