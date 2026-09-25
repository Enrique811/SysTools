# Quickstart: Validacion de integracion operativa del verificador

Esta guia valida Feature 008 sin habilitar busqueda, configuracion visual, etiquetas, reportes o impresion. Los escenarios automatizados no requieren Firebird ni licencia reales; el recorrido visual real usa solamente un entorno no productivo autorizado.

## Prerequisites

- Windows con .NET SDK indicado por `global.json`.
- Directorio de trabajo en la raiz de `SysTools`.
- Para recorrido real opcional: configuracion local autorizada, Firebird no productivo y licencia de prueba valida para el equipo.

## 1. Restore and build

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
```

Expected: cero errores/warnings y ningun paquete nuevo para Feature 008.

## 2. Validate Business workflow

```powershell
dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --no-build --filter "FullyQualifiedName~PriceVerifier"
```

Expected:

- matriz configuracion/conexion/licencia respeta short-circuit;
- solo las tres comprobaciones validas crean sesion Ready;
- input vacio o mayor de 50 no consulta repositorio;
- 50 productos conservan codigo, acentos, nulls, stock y formato MX/CO;
- fallo de catalogo invalida sesion y exige Prepare/Retry;
- logs no contienen centinelas ni configuracion.

## 3. Validate ViewModel, lifecycle and shell

```powershell
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --no-build --filter "FullyQualifiedName~PriceVerifier|FullyQualifiedName~ShellStatus|FullyQualifiedName~Mvvm|FullyQualifiedName~DependencyInjection"
```

Expected:

- Prepare/Retry muestran Pending y habilitan solo al quedar Ready;
- Enter ejecuta una llamada; confirmaciones durante busy se ignoran;
- 100 consultas consecutivas no duplican ni cruzan resultados;
- deactivate/cierre cancela y una respuesta tardia no publica;
- exito conserva codigo, solicita foco/select-all y muestra precio correcto;
- shell refleja exactamente los estados del modulo;
- DI resuelve workflow y ViewModel singleton.

## 4. Validate architecture, security and scope

```powershell
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --no-build --filter "FullyQualifiedName~Architecture|FullyQualifiedName~Security|FullyQualifiedName~Accessibility"
rg -n "SysTools.Data|FirebirdSql|FbConnection|System.IO|Cryptography|ZXing|FastReport|PrintDialog|SearchByDescriptionAsync|ILabelQueueService" src/Presentation/Modules/PriceVerifier
rg -n "Barcode|Password|Licencia|IpEmpresa|RutaEmpresa" src/Business/PriceVerifier src/Presentation/Modules/PriceVerifier
```

Expected:

- primera busqueda: cero infraestructura, search, barcode grafico, etiquetas, reportes o impresion en View/ViewModel;
- segunda busqueda: solo nombres permitidos de captura/estado, cero valores registrados o configuracion completa;
- pruebas con centinelas encuentran cero codigo, producto, password, ruta, host o licencia en estado/logs.

## 5. Controlled integration prerequisites

Antes del recorrido visual, ejecutar las validaciones controladas existentes con variables autorizadas y sin copiar secretos a archivos o salida:

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj --no-build --filter "Category=FirebirdIntegration"
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj --no-build --filter "Category=RepositoryIntegration"
```

Si el entorno no esta habilitado, se omiten; la omision no invalida pruebas UI con dobles, pero el recorrido real queda registrado como SKIPPED.

## 6. Manual WPF walkthrough

```powershell
dotnet run --project src/Presentation/SysTools.Presentation.csproj --no-build
```

Con entorno autorizado:

1. Abrir en 1280x720. La shell aparece antes de terminar I/O y muestra Preparando.
2. Confirmar Conexion/Licencia disponibles; el campo recibe foco.
3. Ejecutar una consulta controlada con demora de 6 segundos; durante la espera la ventana se puede mover, se repinta y muestra el estado ocupado. Al terminar se muestra producto/precio y el codigo queda visible y seleccionado.
4. Escanear otro codigo. Sustituye al anterior sin mouse ni concatenacion.
5. Probar codigo inexistente: campos neutrales, mensaje claro, captura aun disponible.
6. Provocar de forma controlada una perdida de acceso: campos neutrales, captura bloqueada y Retry visible.
7. Restaurar entorno y pulsar Retry: repite tres comprobaciones y devuelve foco.
8. Recorrer con Tab/Shift+Tab. Buscar, Imprimir y Configuracion siguen deshabilitados; estados se entienden sin color.
9. Repetir maximizado y a 125% DPI: sin solapamientos, mensajes envueltos y contenido esencial visible.

No registrar capturas, productos, rutas, licencia ni credenciales en evidencia.

## 7. Full regression

```powershell
dotnet test SysTools.sln --no-build
```

Expected: todas las suites deterministas pasan; solo integraciones externas no habilitadas pueden quedar skipped.

## Evidence to retain

- Comandos y conteos restore/build/test.
- Matrices de preparacion, 50 productos y 100 consultas.
- Auditorias de privacidad, capas y alcance.
- PASS/FAIL/SKIPPED del recorrido manual a 1280x720, maximizado y 125% DPI.
- Integraciones controladas o motivo explicito de omision, sin secretos.

## References

- [Specification](spec.md)
- [Data model](data-model.md)
- [Workflow contract](contracts/price-verifier-workflow-contract.md)
- [Research decisions](research.md)
