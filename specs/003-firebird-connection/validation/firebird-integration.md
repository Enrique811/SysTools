# Integracion Firebird real

Fecha: 2026-09-23

Estado operacional: PASS para conexion valida.

Se uso la configuracion heredada autorizada, decodificada en memoria con la
misma regla Base64 de `ConfigManager`. No se imprimieron ni persistieron host,
ruta, usuario, password o cadena de conexion.

Comando equivalente ejecutado:

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Release --filter 'Category=FirebirdIntegration'
```

Resultado:

- Conexion valida: 1 prueba passed, que verifica 10 aperturas consecutivas.
- Cada apertura termino como `Success` dentro del limite de 6 segundos.
- Fallos reales autorizados: 2 pruebas skipped.

Una primera ejecucion diagnostica uso accidental de los valores Base64 sin
decodificar y devolvio `ServerUnavailable`; no se trato como validacion. Tras
aplicar la decodificacion heredada, la ejecucion final fue PASS.

No se probaron deliberadamente password invalido ni base inexistente porque la
autorizacion recibida cubre la base indicada, no intentos destructivos o
credenciales incorrectas. Esos escenarios continuan cubiertos con dobles y
quedan disponibles como integracion opt-in separada.
