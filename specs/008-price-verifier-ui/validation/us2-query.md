# US2 exact query validation

**Result**: PASS — 2026-09-24

- Espacios exteriores se retiran una vez; ceros iniciales y contenido interior se conservan.
- Vacío y longitud 51 se rechazan sin catálogo; longitud 50 se acepta.
- Cada confirmación aceptada produce una consulta exacta; busy ignora reentrada sin cola ni cancelación.
- La matriz automatizada recorre 50 productos con acentos, nulls, stock neutral y precio opcional.
- La prueba de confiabilidad completa 100 consultas con 100 llamadas y estado final correspondiente a la consulta 100.
- El TextBox usa binding TwoWay inmediato, Enter ejecuta el comando y FocusRequestVersion se traduce a Focus+SelectAll por Dispatcher.

La validación forma parte de los filtros Business 45 PASS y Presentation 27 PASS.
