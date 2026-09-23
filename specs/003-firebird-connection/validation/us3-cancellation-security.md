# Validación US3: cancelación y seguridad

Fecha: 2026-09-23

Comando:

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~FirebirdConnectionCancellationTests|FullyQualifiedName~FirebirdConnectionConcurrencyTests|FullyQualifiedName~FirebirdConnectionLifecycleTests|FullyQualifiedName~FirebirdConnectionLoggingTests"
```

Resultado: PASS. 7 pruebas superadas, 0 fallidas y 0 omitidas.

Evidencia:

- Cancelación previa y durante `OpenAsync` retorna `Canceled` y dispone una vez.
- La cancelación del consumidor gana si coincide con deadline o con
  `TimeoutException` del proveedor.
- Dos solicitudes concurrentes obtienen resultados y conexiones independientes.
- 100 intentos mixtos (éxito, fallo inesperado y cancelación) crean 100 conexiones;
  las 100 se disponen exactamente una vez.
- Los logs Data no reciben el objeto excepción ni su mensaje; contienen solo
  operación, outcome, nombre de tipo y códigos numéricos.
- Los logs Business contienen únicamente operación, outcome, duración y campos/
  cantidad de issues.
- La búsqueda automatizada no encontró los seis valores centinela de password,
  licencia, host, ruta, usuario y cadena en resultados, mensajes, excepciones del
  logger o propiedades estructuradas.
