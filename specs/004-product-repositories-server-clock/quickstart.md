# Quickstart: Validacion de repositorios de productos y fecha del servidor

## Purpose

Validar consultas exactas y por prefijo, mapeo tolerante a nulos, fecha del servidor, cancelacion, seguridad y disposicion sin escribir en Firebird.

## Prerequisites

- Windows 11 x64 y SDK .NET seleccionado por `global.json`.
- Repositorio en la raiz de SysTools.
- Para integracion: Firebird no productivo autorizado con estructura `PRODUCTOS`, `INVENTARIO_BALANCES` y acceso a `RDB$DATABASE`.
- No colocar host, ruta, usuario, password, codigo o descripciones reales en archivos, argumentos, capturas, logs o Git.

## 1. Restore and build

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln -c Debug --no-restore
```

Expected: cero warnings/errores; Firebird permanece confinado a Data y pruebas de integracion.

## 2. Feature tests

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj -c Debug --no-build
```

Expected:

- Producto exacto, inexistente, nulos, acentos, descripcion de 255 caracteres, decimal y stock sin registro.
- Prefijo parametrizado, capitalizacion, orden y coleccion vacia.
- Timestamp del servidor sin reloj local.
- Cancelacion y disposicion unica de conexion, comando y lector.
- Excepcion y logging sin secretos, entradas, filas ni mensajes crudos.

## 3. Regression tests

```powershell
dotnet test SysTools.sln -c Debug --no-build
dotnet test SysTools.sln -c Release
```

Expected: todas las suites pasan; las integraciones se omiten explicitamente si no hay entorno.

## 4. Architecture, SQL and scope audit

```powershell
rg -n "FirebirdSql|FbConnection|FbCommand|FbDataReader" src --glob '*.cs' --glob '*.csproj'
rg -n "INSERT|UPDATE|DELETE|MERGE|EXECUTE PROCEDURE" src/Data/Repositories
rg -n "DateTime\\.Now|DateTime\\.UtcNow|PriceFormatter|License|ZXing|FastReport|PrintDialog" src/Data/Repositories src/Business/Repositories src/Entities/Products
```

Expected:

- Tipos del proveedor solo en Data; composition root solo nombra implementaciones.
- Cero SQL de escritura o capacidades fuera de alcance.
- La unica fuente temporal es `CURRENT_TIMESTAMP`.

## 5. Controlled integration

Definir variables solo en la sesion actual, reemplazando marcadores localmente:

```powershell
$env:SYSTOOLS_FIREBIRD_RUN_REPOSITORY_INTEGRATION = '1'
$env:SYSTOOLS_FIREBIRD_TEST_HOST = '<host-no-productivo>'
$env:SYSTOOLS_FIREBIRD_TEST_DATABASE = '<ruta-o-alias>'
$env:SYSTOOLS_FIREBIRD_TEST_USER = '<usuario-prueba>'
$env:SYSTOOLS_FIREBIRD_TEST_PASSWORD = '<password-prueba>'
# Opcionales: si se omiten, la prueba selecciona una fila existente solo mediante SELECT.
$env:SYSTOOLS_FIREBIRD_TEST_PRODUCT_CODE = '<codigo-existente-opcional>'
$env:SYSTOOLS_FIREBIRD_TEST_PRODUCT_PREFIX = '<prefijo-existente-opcional>'
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj -c Release --filter 'Category=RepositoryIntegration'
```

Expected:

- Diez consultas exactas devuelven el mismo producto que una consulta de referencia.
- La busqueda coincide con referencia y queda ordenada.
- Diez timestamps son valores de servidor validos y no futuros fuera de tolerancia.
- Todas las conexiones quedan cerradas; no hay escrituras ni datos del entorno en salida.

Sin habilitacion, host, base, usuario o password, la integracion se marca
`SKIPPED`; eso no valida operacionalmente la feature. Codigo y prefijo son
opcionales y, si faltan, se seleccionan desde una fila existente con consultas
de solo lectura.

## 6. Security and lifecycle

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj -c Release --filter 'Category=RepositoryLifecycle|Category=RepositorySecurity'
rg -n "Systools-Repo-Secret-|Systools-Repo-Barcode-|Systools-Repo-Description-|DataSource=.*Password=" TestResults specs/004-product-repositories-server-clock/validation
```

Expected: 100 operaciones liberan recursos exactamente una vez y cero tokens centinela aparecen en resultados o logs.

## Completion evidence

Registrar en `specs/004-product-repositories-server-clock/validation/`:

- Restore, build y todas las pruebas Debug/Release.
- Casos de mapeo, prefijo, timestamp, lifecycle, concurrencia y seguridad.
- Auditoria de arquitectura, SQL de solo lectura y alcance.
- Integracion real o bloqueo explicito, sin copiar datos del entorno.

Limpiar secretos al terminar:

```powershell
Remove-Item Env:SYSTOOLS_FIREBIRD_TEST_PASSWORD -ErrorAction SilentlyContinue
```
