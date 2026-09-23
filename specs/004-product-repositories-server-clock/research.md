# Research: Repositorios de productos y fecha del servidor

## Decision 1: Conservar el SQL principal del flujo Java

**Decision**: Consultar `PRODUCTOS` con `LEFT JOIN INVENTARIO_BALANCES`, buscar codigo con igualdad exacta y descripcion mediante `UPPER(DESCRIPCION) LIKE UPPER(@prefix)` usando `{texto}%`.

**Rationale**: Es el flujo activo de `SQLArticulo.java`; el esquema confirma codigo unico y un balance unico por producto. Preserva resultados mientras elimina SQL concatenado y estado estatico.

**Alternatives considered**: Usar las tablas auxiliares `ARTICULO`/`LINTARIF` o agregar precios especiales. Se rechazan porque no son el flujo principal y pertenecen fuera del alcance actual.

## Decision 2: Contratos en Business e implementaciones en Data

**Decision**: Definir `IProductRepository` e `IServerClockRepository` en Business; `FirebirdProductRepository` y `FirebirdServerClockRepository` viven en Data y reutilizan `IFirebirdConnectionFactory`.

**Rationale**: Cumple la direccion de dependencias constitucional: Business conoce abstracciones y Entities; Data concentra Firebird. Presentation solo registra implementaciones.

**Alternatives considered**: Colocar interfaces en Data o permitir que Business cree comandos. Ambas opciones acoplan Business a infraestructura y se rechazan.

## Decision 3: Modelo tipado y tolerante a nulos

**Decision**: Usar `int Id`, `string Barcode`, `string Description`, `string? Presentation`, `decimal? PriceWithTax` y `string Stock`. Descripcion nula se vuelve vacia; stock nulo/vacio se vuelve `Sin registro`; presentacion y precio conservan ausencia.

**Rationale**: Evita el modelo Java completamente textual, mantiene ceros iniciales del codigo, permite reglas monetarias posteriores con `decimal` y cumple la exigencia de no asumir campos completos.

**Alternatives considered**: Mantener todos los campos como `string` replica deuda del legado; forzar cero o texto para precio nulo inventa un valor comercial.

## Decision 4: Fallo seguro diferente de ausencia

**Decision**: Una fila inexistente produce `null`; una busqueda sin filas produce una coleccion vacia; los fallos no cancelados producen `RepositoryAccessException` sin excepcion interna ni datos sensibles. La cancelacion conserva `OperationCanceledException`.

**Rationale**: Los consumidores pueden distinguir estados sin referenciar el proveedor. No encadenar la excepcion evita que un manejador superior registre accidentalmente mensajes con credenciales o cadena de conexion.

**Alternatives considered**: Devolver `null`/vacio tambien en fallos oculta problemas; propagar `FbException` viola el aislamiento; incluirla como `InnerException` crea riesgo de filtracion en logging global.

## Decision 5: ADO.NET asincrono y disposicion determinista

**Decision**: Crear una conexion independiente por operacion, abrirla asincronamente, crear comando y lector locales, pasar el token a apertura/ejecucion/lectura y disponer cada recurso con `await using`/`using`.

**Rationale**: Reutiliza la base de 003, permite pruebas con dobles de `DbConnection` y evita conexiones, statements y result sets globales del Java.

**Alternatives considered**: Conexion singleton o reutilizacion global; se rechazan por concurrencia, seguridad y constitucion. EF Core se rechaza por stack aprobado.

## Decision 6: Parametros y timeout

**Decision**: Enlazar `@barcode` y `@descriptionPrefix` como parametros, fijar timeout de comando en 5 segundos y usar `@descriptionPrefix = texto.Trim() + "%"`.

**Rationale**: Impide alteracion de SQL, conserva el prefijo Java y alinea la espera con la conexion de la feature 003.

**Alternatives considered**: Concatenacion, busqueda por contiene o timeout indefinido; se rechazan por seguridad, divergencia funcional y mala recuperacion.

## Decision 7: Timestamp Firebird sin conversion al reloj local

**Decision**: Consultar `SELECT CURRENT_TIMESTAMP AS HORAFECHA FROM RDB$DATABASE` y devolver `DateTime` exactamente desde el proveedor, sin usar `DateTime.Now`, `UtcNow` ni conversion de zona.

**Rationale**: La licencia heredada depende de la fecha del servidor. Firebird `TIMESTAMP` no incluye offset; inventar una zona cambiaria el instante observado.

**Alternatives considered**: `DateTimeOffset` o solo `DateOnly`; el primero requeriria una zona no suministrada y el segundo perderia precision necesaria para impresion y auditoria.

## Decision 8: Integracion opt-in y solo lectura

**Decision**: Agregar pruebas unitarias exhaustivas con dobles y pruebas reales opt-in mediante variables de sesion. Las pruebas reales solo ejecutan `SELECT` y se omiten explicitamente sin entorno autorizado.

**Rationale**: La constitucion exige validacion Firebird, pero no autoriza credenciales en Git ni acceso destructivo. Los dobles permiten validar lifecycle y casos limites siempre.

**Alternatives considered**: Depender siempre de una base real haria la suite no determinista; usar una base productiva o sembrar datos esta fuera de alcance y no es seguro.
