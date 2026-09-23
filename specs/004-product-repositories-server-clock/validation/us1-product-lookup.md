# Validacion US1: consulta exacta de producto

Fecha: 2026-09-23

Comandos:

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~ProductDataReaderMapperTests|FullyQualifiedName~FirebirdProductRepository"
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~RepositoryDependencyInjectionTests"
```

Resultado: PASS; 13 pruebas de repositorio y mapper, mas 1 prueba de
composicion, sin fallos ni omisiones.

Se verificaron SQL de solo lectura, parametro `@barcode`, timeout 5, ceros
iniciales, acentos, descripcion de 255 caracteres, decimal, nulos, `Sin
registro`, ausencia como `null`, cancelacion en apertura/ejecucion/lectura,
excepcion segura, disposicion unica y resolucion sin abrir red.
