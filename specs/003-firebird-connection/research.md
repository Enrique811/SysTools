# Research: Conexión y prueba Firebird

## Decision 1: Proveedor Firebird estable y aislado en Data

**Decision**: Usar `FirebirdSql.Data.FirebirdClient` 10.3.4 y agregar la referencia únicamente a `src/Data/SysTools.Data.csproj`.

**Rationale**: Es la versión estable publicada del proveedor oficial al momento de planificar y es compatible con los targets modernos usados por la solución. La constitución ya seleccionó este proveedor. Mantener el paquete en Data evita contaminar Entities, Business, Views o ViewModels.

**Alternatives considered**:

- Versión prerelease posterior: descartada para no introducir cambios no estabilizados en una dependencia crítica.
- Cliente nativo directo u ODBC: descartados porque contradicen el stack aprobado y agregan despliegue/interop innecesarios.
- EF Core: descartado por constitución y porque esta feature solo necesita conectividad.

**Sources**:

- [NuGet: FirebirdSql.Data.FirebirdClient 10.3.4](https://www.nuget.org/packages/FirebirdSql.Data.FirebirdClient/10.3.4)
- [Repositorio oficial Firebird .NET Provider](https://github.com/FirebirdSQL/NETProvider)

## Decision 2: Connection string tipada y reutilizable

**Decision**: Construir siempre con `FbConnectionStringBuilder`, usando `DataSource`, `Database`, `UserID`, `Password`, `Port=3050`, `Charset=ISO8859_1`, `Dialect=3`, `ConnectionTimeout=5` y `Pooling=true`.

**Rationale**: El builder oficial escapa valores y expone de forma explícita los parámetros relevantes. Conserva la equivalencia de la URL Java heredada y deja una fábrica utilizable por repositorios futuros. El pooling es interno al proveedor: cada llamada aún devuelve un objeto de conexión distinto que debe cerrarse.

**Alternatives considered**:

- Concatenar texto manualmente: descartado por errores de escape y riesgo de registrar accidentalmente la cadena completa.
- Charset por defecto o UTF8: descartado porque cambia el contrato con la base heredada `ISO8859_1`.
- Deshabilitar pooling globalmente: descartado porque penalizaría repositorios futuros; las pruebas verifican propiedad del objeto, no la destrucción física del pool.

**Sources**:

- [Documentación ADO.NET del proveedor](https://github.com/FirebirdSQL/NETProvider/blob/master/docs/ado-net.md)
- [Propiedades oficiales de FbConnectionStringBuilder](https://github.com/FirebirdSQL/NETProvider/blob/master/src/FirebirdSql.Data.FirebirdClient/FirebirdClient/FbConnectionStringBuilder.cs)
- [Comportamiento Java heredado](../../docs/documentacion-tecnica-verificador-precios.md#6-conexion-a-base-de-datos)

## Decision 3: Dos límites coordinados para timeout y cancelación

**Decision**: Configurar `ConnectionTimeout=5` y además llamar `OpenAsync` con un token enlazado que se cancela a los 5 segundos. Clasificar primero la cancelación explícita del consumidor y después el vencimiento interno.

**Rationale**: El proveedor acepta `CancellationToken` en `OpenAsync` y distingue una cancelación solicitada del timeout interno. El límite en la cadena conserva el comportamiento esperado por el driver, mientras el token enlazado da al caso de uso un límite controlable y permite cancelar desde una UI futura.

**Alternatives considered**:

- Solo `ConnectionTimeout`: descartado porque no satisface cancelación del consumidor y ha tenido comportamiento dependiente de la red en versiones anteriores.
- `Task.WhenAny` sin pasar token al proveedor: descartado porque el método podría retornar dejando una apertura viva en segundo plano.
- `Task.Run` sobre apertura síncrona: descartado porque consume un hilo y no cancela la operación de red.

**Sources**:

- [Implementación oficial de FbConnection.OpenAsync](https://github.com/FirebirdSQL/NETProvider/blob/master/src/FirebirdSql.Data.FirebirdClient/FirebirdClient/FbConnection.cs#L583)
- [Configuración oficial del timeout de conexión Firebird](https://github.com/FirebirdSQL/firebird/blob/master/builds/install/misc/firebird.conf)

## Decision 4: Fábrica Data basada en DbConnection

**Decision**: Definir `IFirebirdConnectionFactory` dentro de Data con retorno `DbConnection`; `FirebirdConnectionFactory` crea un `FbConnection` nuevo por llamada. Business consume otra abstracción, `IFirebirdConnectionProbe`, que no expone conexiones.

**Rationale**: `DbConnection` ofrece apertura, cierre y comandos suficientes para repositorios posteriores sin filtrar el tipo del proveedor fuera de Data. Un probe Business separado permite probar coordinación y mensajes con dobles deterministas y mantiene la inversión de dependencias exigida por la constitución.

**Alternatives considered**:

- Retornar `FbConnection` desde Business: descartado porque acopla el caso de uso al proveedor.
- Hacer que Business construya la cadena: descartado porque la infraestructura y credenciales técnicas pertenecen a Data.
- Mantener una conexión singleton: descartado por aislamiento, concurrencia y recuperación de fallos.

## Decision 5: Clasificación por códigos y fallback seguro

**Decision**: Clasificar `FbException` por sus códigos numéricos y tipos internos conocidos; nunca por texto. Mantener listas explícitas para autenticación, red y disponibilidad de base. Cualquier ambigüedad termina en `UnexpectedFailure`.

**Rationale**: Los mensajes pueden estar localizados y el mismo síntoma puede envolver varios códigos. `FbException.Errors` permite conservar señales estables. La política evita inventar diagnósticos y cumple el fallback exigido por FR-012.

Familias iniciales que deben quedar cubiertas por pruebas:

- Autenticación: `335544472` (`isc_login`) y `335545106` (`isc_login_error`).
- Servidor/red: `335544421` (`isc_connect_reject`), `335544721` (`isc_network_error`), `335544726` (`isc_net_read_err`) y su equivalente de escritura cuando esté presente.
- Base/attach: `335544323` (`isc_bad_db_format`), `335544344` (`isc_io_error`), `335544375` (`isc_unavailable`) y `335544379` (`isc_wrong_ods`).
- Timeout/cancelación: tipos `TimeoutException`/`OperationCanceledException` y estado de los tokens, no códigos de mensaje.

**Alternatives considered**:

- Buscar frases como “password” o “network”: descartado por localización y cambios de versión.
- Devolver siempre `UnexpectedFailure`: descartado porque pierde diagnósticos confiables disponibles.
- Exponer `FbException` al consumidor: descartado por acoplamiento y riesgo de filtrar información.

**Sources**:

- [API oficial FbException.Errors](https://github.com/FirebirdSQL/NETProvider/blob/master/src/FirebirdSql.Data.FirebirdClient/FirebirdClient/FbException.cs)
- [Códigos del proveedor oficial](https://github.com/FirebirdSQL/NETProvider/blob/master/src/FirebirdSql.Data.FirebirdClient/Common/IscCodes.cs)
- [Códigos Firebird mantenidos por el proyecto oficial](https://github.com/FirebirdSQL/jaybird/blob/master/src/main/org/firebirdsql/gds/ISCConstants.java)

## Decision 6: Logs estructurados sin excepción cruda

**Decision**: Registrar eventos con nombre estable y propiedades escalares seguras: operación, resultado, duración, issue count, tipo de excepción y códigos numéricos. No pasar el objeto excepción, su mensaje, configuración ni cadena al logger en el flujo de conexión.

**Rationale**: Un mensaje técnico de attach puede incluir host o ruta y una excepción futura podría incorporar más datos. Los códigos y tipos permiten soporte y métricas sin arriesgar password, licencia, configuración serializada ni cadena completa.

**Alternatives considered**:

- `LogError(exception, ...)`: descartado en este flujo por el contenido no controlado de `Message` y `Data`.
- No registrar fallos: descartado porque impediría soporte y violaría la constitución.
- Redactar con expresiones regulares después de serializar: descartado porque es frágil y procesa secretos que nunca deberían llegar al sink.

## Decision 7: Validación específica de conectividad

**Decision**: Crear `ConnectionConfigurationValidator` para evaluar solo `IpEmpresa`, `RutaEmpresa`, `Usuario` y `Password` en orden estable y devolver todos los faltantes. No reutilizar `ConfigurationValidator.Validate` como gate completo.

**Rationale**: Una prueba de configuración proporcionada debe depender únicamente de los campos Firebird. Bloquearla por `Ambiente`, `FormatoPrecio` o `Columnas` mezclaría reglas de persistencia con conectividad y contradiría FR-005/FR-006. La validación no modifica ni recorta los valores.

**Alternatives considered**:

- Exigir `IsConnectionReady` de la feature 002: descartado porque también depende de catálogos ajenos a la conexión.
- Validar un campo por vez: descartado porque impide mostrar todos los faltantes en una sola respuesta.

## Decision 8: Integración real opt-in, obligatoria para cierre operativo

**Decision**: Crear pruebas de integración omitibles cuando faltan variables de entorno, sin secretos en archivos ni salida. Para aceptar operativamente la feature deben ejecutarse contra una base no productiva y documentarse 10 éxitos consecutivos, autenticación inválida y base inexistente cuando el entorno lo permita.

**Rationale**: Los dobles prueban ciclo de vida y clasificación, pero no verifican protocolo, autenticación, puerto o compatibilidad real del charset. Las variables de entorno evitan comprometer credenciales y permiten CI opcional.

**Alternatives considered**:

- Usar credenciales embebidas: descartado por seguridad.
- Descargar o iniciar Firebird automáticamente: descartado porque altera el entorno y no valida la base heredada real.
- Considerar suficientes los unit tests: descartado por el riesgo explícito y la constitución.
