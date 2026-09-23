# Validacion US2: busqueda por descripcion

Fecha: 2026-09-23

Comando:

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~FirebirdProductRepositorySearch"
```

Resultado: PASS; 7 pruebas superadas, 0 fallidas y 0 omitidas.

Se verificaron prefijo trimmeado con `%`, comparacion `UPPER LIKE UPPER`, orden
por descripcion/ID, parametro separado del SQL, coleccion de solo lectura,
entrada vacia sin conexion, cero filas, cancelacion, fallo seguro y disposicion.
