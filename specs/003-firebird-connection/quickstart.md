# Quickstart: Validación de conexión Firebird

## Purpose

Validar construcción, apertura, clasificación, cancelación, seguridad y cierre de conexiones sin modificar configuración real ni consultar datos funcionales.

## Prerequisites

- Windows 11 x64.
- SDK .NET 10 seleccionado por `global.json`.
- Repositorio en la raíz de SysTools.
- Para integración: servidor Firebird no productivo accesible por puerto 3050, base autorizada y credenciales de prueba.
- No colocar secretos en archivos, argumentos, capturas, logs ni Git.

## 1. Restore and build

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln -c Debug --no-restore
```

Expected:

- Todos los proyectos compilan sin warnings.
- `FirebirdSql.Data.FirebirdClient` se resuelve únicamente desde Data y la suite de pruebas que valida Data.
- Entities y Business siguen sin referencia al paquete Firebird o WPF.

## 2. Run the feature suite

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Debug --no-build
```

Expected groups:

- Entidades e invariantes descritas en [data-model.md](data-model.md).
- Validación conjunta y orden estable de los cuatro campos.
- Mapeo del builder: host, base, usuario, password, puerto, charset, dialecto, timeout y pooling.
- Apertura/disposición exactamente una vez en éxito, fallo y cancelación.
- Prioridad entre cancelación del consumidor y timeout.
- Clasificación por códigos y fallback desconocido.
- Dos intentos concurrentes aislados.
- Logs/resultados sin secretos, configuración ni cadena completa.

## 3. Run all regression tests

```powershell
dotnet test SysTools.sln -c Debug --no-build
```

Expected:

- Pasan las suites de Presentation, Configuration y Firebird.
- La shell puede resolverse sin abrir red ni requerir un servidor.
- Las pruebas arquitectónicas rechazan referencias Firebird desde Entities, Business, Views y ViewModels.

## 4. Static architecture and scope audit

```powershell
rg -n "FirebirdSql|FbConnection|FbException|FbConnectionStringBuilder" src --glob '*.cs' --glob '*.csproj'
rg -n "SELECT|INSERT|UPDATE|DELETE|EXECUTE PROCEDURE|LicenseValidation|PrintDialog|ZXing|FastReport" src/Data/Connection src/Business/Connection src/Entities/Connection
```

Expected:

- Los tipos/paquete Firebird aparecen solo en Data; `App.xaml.cs` puede nombrar implementaciones Data para DI, pero no tipos del proveedor.
- No existen SQL, consultas de productos/fecha, licencia funcional, impresión, barcode, reportes ni UI nueva.

## 5. Run controlled integration

Definir variables solo en la sesión actual. Los valores siguientes son marcadores y deben reemplazarse localmente sin registrarlos:

```powershell
$env:SYSTOOLS_FIREBIRD_RUN_INTEGRATION = '1'
$env:SYSTOOLS_FIREBIRD_TEST_HOST = '<host-no-productivo>'
$env:SYSTOOLS_FIREBIRD_TEST_DATABASE = '<ruta-o-alias>'
$env:SYSTOOLS_FIREBIRD_TEST_USER = '<usuario-prueba>'
$env:SYSTOOLS_FIREBIRD_TEST_PASSWORD = '<password-prueba>'
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Release --filter 'Category=FirebirdIntegration'
```

Expected:

- 10 de 10 intentos válidos retornan `Success`.
- Cada intento completa en menos de 6 segundos.
- Cada objeto de conexión está cerrado/desechado antes del resultado.
- La salida no imprime las variables ni la cadena.

Si `SYSTOOLS_FIREBIRD_RUN_INTEGRATION` o un dato requerido falta, las pruebas reales se omiten explícitamente. Una omisión no valida operativamente la feature.

## 6. Authorized real failure scenarios

Solo si el entorno autoriza intentos inválidos, agregar valores dedicados:

```powershell
$env:SYSTOOLS_FIREBIRD_TEST_BAD_PASSWORD = '<password-invalido-autorizado>'
$env:SYSTOOLS_FIREBIRD_TEST_MISSING_DATABASE = '<ruta-o-alias-inexistente-autorizado>'
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Release --filter 'Category=FirebirdFailureIntegration'
```

Expected:

- Password inválido retorna `AuthenticationFailed` cuando Firebird entrega un código reconocido.
- Base inexistente retorna `DatabaseUnavailable` cuando el attach entrega una señal confiable.
- Un fallo ambiguo retorna `UnexpectedFailure`, no una causa inventada.
- Ningún intento altera configuración o cierra la shell.

No provocar bloqueos de cuenta, apagar servidores ni probar contra producción.

## 7. Timeout, cancellation and concurrency

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Release --filter 'Category=Lifecycle'
```

Expected:

- 100 casos automatizados disponen exactamente una conexión por intento.
- El timeout retorna `Timeout` y la cancelación externa retorna `Canceled`.
- Cuando coinciden, retorna `Canceled`.
- Cancelar/fallar una solicitud no cambia otra solicitud concurrente.

## 8. Security audit

Ejecutar la suite con tokens centinela únicos y revisar el directorio de resultados/logs generado por pruebas:

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Release --filter 'Category=Security'
rg -n "Systools-Fb-Secret-|Systools-Fb-License-|DataSource=.*Password=" TestResults specs/003-firebird-connection/validation
```

Expected:

- La prueba automática confirma cero coincidencias en resultados, mensajes y eventos capturados.
- La búsqueda manual devuelve cero coincidencias de tokens y cadenas.
- Los eventos conservan estado, duración, tipo y códigos numéricos permitidos.

## Completion evidence

Registrar después de implementar en `specs/003-firebird-connection/validation/`:

- Comandos y resultados de restore/build/test Debug y Release.
- Resultado de los 100 casos de ciclo de vida y concurrencia.
- Auditoría de arquitectura, alcance y secretos.
- Fecha, entorno no productivo y resultado de 10 conexiones reales consecutivas, sin copiar host, ruta, usuario o password.
- Fallos reales controlados disponibles y su categoría; si no fueron autorizados, indicarlo explícitamente.
- Confirmación de que `ISO8859_1`, puerto 3050 y timeout 5 quedaron presentes en la configuración construida.

Después de validar, limpiar las variables sensibles de la sesión:

```powershell
Remove-Item Env:SYSTOOLS_FIREBIRD_TEST_PASSWORD -ErrorAction SilentlyContinue
Remove-Item Env:SYSTOOLS_FIREBIRD_TEST_BAD_PASSWORD -ErrorAction SilentlyContinue
```
