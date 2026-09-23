# Repositories

Implementaciones Firebird de solo lectura para los contratos definidos en
`SysTools.Business.Repositories`.

- `FirebirdProductRepository` consulta un producto por codigo exacto y busca
  productos por prefijo de descripcion.
- `FirebirdServerClockRepository` obtiene `CURRENT_TIMESTAMP` desde Firebird.
- `ProductDataReaderMapper` convierte filas tolerando campos opcionales y
  normaliza stock ausente como `Sin registro`.
- `FirebirdRepositoryDiagnostics` traduce fallos a excepciones seguras y logs
  sin configuracion, entradas, filas o mensajes del proveedor.

Cada llamada crea y dispone su propia conexion, comando y lector. Los
repositorios no mantienen estado global, no escriben en la base y no contienen
reglas de precio, licencia, UI, etiquetas o impresion. Presentation solo conoce
las implementaciones en el composition root; Views y ViewModels consumiran
casos de uso de Business, nunca este directorio directamente.
