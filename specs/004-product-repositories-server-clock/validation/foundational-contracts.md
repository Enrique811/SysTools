# Contratos fundacionales

Fecha: 2026-09-23

Comando:

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj -c Debug --no-restore
```

Resultado: PASS; 15 pruebas superadas, 0 fallidas y 0 omitidas.

Se observo primero el fallo de compilacion esperado por ausencia de `Product`,
`RepositoryOperation` y `RepositoryAccessException`. Despues se verificaron
invariantes, normalizacion de nulos, ceros iniciales, longitudes, mensajes
catalogados, ausencia de inner exception y `ToString` sin payload.
