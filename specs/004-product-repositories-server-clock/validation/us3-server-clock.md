# Validacion US3: fecha y hora del servidor

Fecha: 2026-09-23

Comandos:

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~FirebirdServerClockRepository"
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~RepositoryDependencyInjectionTests"
```

Resultado: PASS; 5 pruebas de reloj y 1 de composicion, sin fallos ni
omisiones.

Se verificaron `CURRENT_TIMESTAMP`, valor y `Kind` exactos del servidor,
timeout 5, cero parametros/escrituras, valor o fila nulos como fallo seguro,
cancelacion, disposicion y resolucion DI sin apertura de red. La implementacion
no usa `DateTime.Now`, `DateTime.UtcNow` ni conversion de zona.
