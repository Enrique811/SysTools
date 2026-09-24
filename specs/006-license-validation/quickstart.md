# Quickstart: Validacion de licencias heredadas

## Prerequisites

- Windows con .NET SDK fijado por `global.json`.
- Repositorio en la raiz de `SysTools`.
- No se requiere Firebird real, licencia productiva, llave privada ni ejecutar la aplicacion WPF.

## 1. Build and full regression

```powershell
dotnet build SysTools.sln --no-restore
dotnet test SysTools.sln --no-build
```

Expected: compilacion limpia y todas las suites existentes mas `SysTools.Licensing.Tests` en verde.

## 2. Compatibility and cryptography

```powershell
dotnet test tests/SysTools.Licensing.Tests/SysTools.Licensing.Tests.csproj --filter "FullyQualifiedName~Compatibility"
```

Expected:

- Los vectores validos reconocen distribuidor o desarrollador.
- Una alteracion en UUID, inicio, fin o firma produce `InvalidSignature`.
- El payload usa UTF-8, `LF`, UUID en mayusculas y horas de frontera.
- Ningun archivo de llave privada existe bajo `src/`, `tests/` o `specs/`.

## 3. Source and field failures

```powershell
dotnet test tests/SysTools.Licensing.Tests/SysTools.Licensing.Tests.csproj --filter "FullyQualifiedName~Source|FullyQualifiedName~Parsing"
```

Expected: JSON directo y archivo funcionan; entradas ausentes, archivos no disponibles/ grandes, JSON malformado, duplicados, tipos incorrectos, UUID/fechas/firma invalidos reciben el estado controlado del contrato.

## 4. Hardware and server time

```powershell
dotnet test tests/SysTools.Licensing.Tests/SysTools.Licensing.Tests.csproj --filter "FullyQualifiedName~Hardware|FullyQualifiedName~Validity|FullyQualifiedName~Cancellation"
```

Expected: UUID se normaliza, mismatch falla antes de Firebird, bordes son inclusivos, reloj local no participa, error de servidor se traduce y cancelacion se propaga.

## 5. Security and architecture audit

```powershell
dotnet test tests/SysTools.Licensing.Tests/SysTools.Licensing.Tests.csproj --filter "FullyQualifiedName~Security"
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --filter "FullyQualifiedName~Architecture|FullyQualifiedName~Composition"
rg -n --glob "*.cs" "DateTime\\.(Now|UtcNow)|private\\.key|BEGIN PRIVATE KEY" src/Business/Licensing src/Data/Licensing
```

Expected: cero centinelas en resultados/logs, dependencias registradas, ningun acceso desde View/ViewModel y la busqueda final no encuentra reloj local ni material privado en licenciamiento.

## 6. Scope review

Confirmar que no se agregaron controles WPF, generador de licencias, llave privada, barcode, reportes, impresion, cache, renovacion ni revocacion. La configuracion sigue almacenando la licencia como texto opaco.
