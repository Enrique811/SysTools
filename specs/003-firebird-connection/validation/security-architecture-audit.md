# Auditoría de seguridad y arquitectura

Fecha: 2026-09-23

Resultado: PASS.

## Dependencias

- `FirebirdSql.Data.FirebirdClient` 10.3.4 aparece como `PackageReference`
  únicamente en `src/Data/SysTools.Data.csproj`.
- Los usos productivos de `FbConnectionStringBuilder`, `FbConnection` y
  `FbException` están únicamente en `src/Data/Connection`.
- Entities y Business no referencian el paquete ni tipos `FbConnection`.
- Views y ViewModels no referencian Data Connection ni el proveedor.
- `App.xaml.cs` nombra implementaciones Data solo como composition root.
- Las 6 pruebas de `LayerDependencyTests` pasaron.

## Logging y secretos

- Cero llamadas de logging pasan configuración, connection string, password,
  host, ruta, usuario, `Exception`, `Exception.Message` o destructuring.
- Business registra solo operación, outcome, duración, campos y cantidad de
  issues.
- Data registra solo operación, outcome, nombre de tipo y códigos numéricos.
- La prueba de seguridad pasó con centinelas de password, licencia, host, ruta,
  usuario y cadena; ningún valor apareció en resultado o eventos capturados.
- La búsqueda en `TestResults` (cuando existe) y `validation/` devolvió cero
  coincidencias de secretos centinela o patrón de cadena con password.

No se encontraron excepciones crudas, payload de configuración ni credenciales
persistidas por esta feature.
