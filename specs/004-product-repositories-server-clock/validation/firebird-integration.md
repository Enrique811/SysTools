# Integracion Firebird real

Fecha: 2026-09-23

Estado operacional: PASS.

Se uso la configuracion heredada autorizada, decodificada exclusivamente en
memoria. Codigo y prefijo se seleccionaron mediante `SELECT FIRST 1`; no se
imprimieron ni persistieron configuracion, credenciales o datos seleccionados.

Comando equivalente ejecutado:

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj -c Release --filter 'Category=RepositoryIntegration'
```

Resultado: 3 passed, 0 failed, 0 skipped.

- Diez consultas exactas consecutivas coincidieron campo por campo con una
  consulta de referencia independiente.
- La busqueda por descripcion coincidio con los IDs ordenados de referencia.
- Diez timestamps coincidieron con lecturas directas del servidor dentro de la
  tolerancia definida.
- Todas las operaciones fueron `SELECT`; no se modifico la base.

Una primera ejecucion diagnostica uso accidental de valores Base64 sin
decodificar y fallo de resolucion; la evidencia aceptada es la ejecucion final
con la misma decodificacion que usa `ConfigManager` en Java.
