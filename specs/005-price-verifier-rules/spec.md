# Feature Specification: Reglas de negocio del verificador

**Feature Branch**: `[005-price-verifier-rules]`

**Created**: 2026-09-23

**Status**: Draft

**Input**: User description: "Implementar servicios de negocio para consulta de productos, formateo de precios, manejo de informacion adicional y cola de etiquetas de 1, 2 o 3 columnas."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar productos sin depender de la pantalla (Priority: P1)

Un operador consulta un articulo por su codigo de barras o busca productos por el inicio de su descripcion y recibe resultados de negocio utilizables por cualquier interfaz, sin que la pantalla conozca detalles de acceso a datos.

**Why this priority**: La consulta del articulo es el punto de entrada del verificador y permite reutilizar los repositorios existentes sin acoplar las siguientes pantallas a Firebird.

**Independent Test**: Se puede probar con un catalogo controlado consultando un codigo existente, uno inexistente, una entrada vacia y un prefijo de descripcion; los resultados y las llamadas al origen son observables sin iniciar la interfaz grafica.

**Acceptance Scenarios**:

1. **Given** un codigo valido asociado a un producto, **When** se consulta el codigo, **Then** se devuelve ese producto sin modificar su codigo, descripcion, presentacion, precio o existencia.
2. **Given** un codigo valido sin coincidencia, **When** se consulta el codigo, **Then** se informa que no existe producto sin fabricar un articulo ni lanzar un error tecnico.
3. **Given** una entrada vacia o compuesta solo por espacios, **When** se intenta consultar o buscar, **Then** se devuelve un resultado vacio y no se contacta el origen de productos.
4. **Given** un prefijo de descripcion con espacios exteriores, **When** se busca, **Then** se usa el prefijo normalizado y se conserva el orden recibido del catalogo.
5. **Given** una cancelacion solicitada o un fallo controlado del origen, **When** la consulta esta en curso, **Then** la cancelacion o el fallo seguro se conserva para que el consumidor decida como informarlo.

---

### User Story 2 - Mostrar precios compatibles con cada pais (Priority: P1)

Un operador ve el precio final con impuestos usando el formato configurado para Mexico o Colombia, de manera consistente e independiente de la configuracion regional de Windows.

**Why this priority**: Un precio ambiguo o redondeado de forma inconsistente afecta directamente la operacion del verificador y las etiquetas posteriores.

**Independent Test**: Se puede proporcionar una tabla de importes positivos, cero, negativos y con mas decimales para comprobar que `MX` siempre produce simbolo de moneda y dos decimales, mientras `CO` produce moneda colombiana sin decimales.

**Acceptance Scenarios**:

1. **Given** el importe `123.45` y formato `MX`, **When** se formatea, **Then** el resultado es `$123.45` independientemente de la cultura activa del equipo.
2. **Given** un importe con decimales y formato `CO`, **When** se formatea, **Then** el resultado usa moneda colombiana y cero decimales.
3. **Given** un formato distinto de `MX`, **When** se formatea un importe, **Then** se aplica el comportamiento legado predeterminado `CO`.
4. **Given** un producto sin precio final, **When** se intenta preparar para etiqueta, **Then** la operacion se rechaza de forma controlada y no representa el precio ausente como cero.

---

### User Story 3 - Preparar etiquetas y completar filas (Priority: P2)

Un operador prepara los datos de una etiqueta con la informacion adicional capturada y, segun una configuracion de una, dos o tres columnas, obtiene una fila completa lista para una futura vista previa o impresion.

**Why this priority**: La cola reproduce una regla central del sistema existente y aisla la preparacion de etiquetas del motor de reportes, que se migrara en una etapa posterior.

**Independent Test**: Se puede capturar etiquetas en colas configuradas con una, dos y tres columnas y verificar el estado pendiente, la posicion, el orden y el lote completado sin usar WPF ni un motor de impresion.

**Acceptance Scenarios**:

1. **Given** una configuracion de una columna, **When** se captura una etiqueta valida, **Then** se devuelve inmediatamente una fila completa con esa unica etiqueta y no quedan pendientes.
2. **Given** una configuracion de dos columnas sin pendientes, **When** se captura la primera etiqueta, **Then** queda una etiqueta pendiente en la primera posicion y aun no existe fila completa.
3. **Given** una configuracion de dos columnas con una etiqueta pendiente, **When** se captura la segunda, **Then** se devuelve una fila completa en orden de captura y la cola queda vacia.
4. **Given** una configuracion de tres columnas, **When** se capturan tres etiquetas, **Then** solo la tercera captura completa la fila, conserva las tres posiciones y reinicia la cola.
5. **Given** textos con acentos, caracteres especiales o espacios intencionales en la informacion adicional, **When** se prepara y encola la etiqueta, **Then** el texto se conserva exactamente y pertenece solo a esa captura.

---

### User Story 4 - Cancelar etiquetas pendientes (Priority: P3)

Un operador cancela una fila incompleta para evitar que articulos capturados por error se mezclen con la siguiente fila de etiquetas.

**Why this priority**: Es una recuperacion necesaria para la operacion diaria, aunque depende de que ya exista una cola funcional.

**Independent Test**: Se puede agregar una o dos etiquetas a una cola incompleta, cancelarla y comprobar que la siguiente captura inicia nuevamente en la primera posicion.

**Acceptance Scenarios**:

1. **Given** una cola con etiquetas pendientes, **When** se cancela, **Then** se eliminan todos los pendientes y se informa cuantos fueron descartados.
2. **Given** una cola vacia, **When** se cancela, **Then** la operacion es idempotente, informa cero descartados y la cola permanece disponible.

### Edge Cases

- Los codigos de barras conservan ceros iniciales y no se transforman en numeros.
- Una busqueda o consulta vacia no debe abrir conexion ni invocar el repositorio.
- La cancelacion solicitada por el consumidor no se convierte en un resultado de producto inexistente.
- El formateo debe ser estable aunque la cultura activa use coma decimal u otro simbolo monetario.
- Un precio ausente impide crear datos de etiqueta para no imprimir informacion economica falsa.
- Solo se aceptan capacidades de una, dos o tres columnas; otro valor se rechaza antes de modificar la cola.
- La cola no expone una coleccion mutable que permita reordenar o alterar capturas ya realizadas.
- Una etiqueta no puede pertenecer parcialmente a dos filas y una fila completada no permanece como pendiente.
- Cancelar despues de completar una fila no modifica el lote ya devuelto al consumidor.
- Cambiar la cantidad de columnas mientras existe una fila incompleta se rechaza sin perder ni mezclar pendientes; el operador debe completar o cancelar la fila actual.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST ofrecer consultas de productos mediante una capa de negocio que no exponga detalles de Firebird ni requiera una interfaz grafica.
- **FR-002**: La consulta exacta MUST conservar el codigo de barras como texto, incluidos los ceros iniciales, y MUST devolver el producto sin alterar los datos entregados por el origen.
- **FR-003**: Una consulta exacta sin coincidencia MUST devolver ausencia de producto como resultado normal.
- **FR-004**: Las entradas nulas, vacias o compuestas solo por espacios MUST producir resultados vacios sin contactar el origen de productos.
- **FR-005**: La busqueda por descripcion MUST retirar unicamente espacios exteriores antes de delegar y MUST conservar el orden de resultados recibido.
- **FR-006**: La capa de negocio MUST conservar la cancelacion solicitada y los fallos seguros catalogados del origen, sin convertirlos en ausencia de producto ni exponer datos tecnicos adicionales.
- **FR-007**: El sistema MUST formatear `MX` con simbolo `$`, exactamente dos decimales y separador decimal `.` independientemente de la cultura activa.
- **FR-008**: El sistema MUST formatear `CO` como moneda colombiana sin decimales y MUST usar este comportamiento para cualquier codigo de formato distinto de `MX`, preservando el valor predeterminado legado.
- **FR-009**: El formateo MUST aplicar una regla de redondeo determinista y verificable para importes que excedan los decimales visibles.
- **FR-010**: El sistema MUST rechazar de forma controlada la preparacion de una etiqueta cuyo producto no tenga precio final, sin sustituirlo por cero.
- **FR-011**: Los datos preparados para etiqueta MUST capturar como una instantanea inmutable el producto, el precio ya formateado y la informacion adicional correspondiente a esa captura.
- **FR-012**: La informacion adicional MUST aceptar texto vacio y MUST conservar exactamente acentos, caracteres especiales y espacios proporcionados; un valor nulo se normaliza a texto vacio.
- **FR-013**: La cola MUST admitir exclusivamente capacidades de `1`, `2` o `3` columnas y MUST rechazar otro valor antes de alterar su estado.
- **FR-014**: Para una columna, cada captura MUST producir inmediatamente una fila completa de una etiqueta y dejar cero pendientes.
- **FR-015**: Para dos o tres columnas, cada captura MUST ocupar la siguiente posicion disponible y solo la captura que alcance la capacidad MUST producir una fila completa.
- **FR-016**: Una fila completa MUST conservar el orden de captura y MUST vaciar atomica e inmediatamente los pendientes para iniciar una nueva fila.
- **FR-017**: Mientras una fila este incompleta, el sistema MUST informar las etiquetas pendientes, sus posiciones, la capacidad total y el numero restante sin exponer colecciones mutables.
- **FR-018**: Cancelar la cola MUST eliminar todas las etiquetas pendientes, informar la cantidad descartada y ser seguro cuando la cola ya este vacia.
- **FR-019**: Cambiar la capacidad mientras existan etiquetas pendientes MUST rechazarse sin alterar la fila actual; despues de completar o cancelar, la siguiente captura MAY usar otra capacidad valida.
- **FR-020**: La logica de consulta, formato, preparacion y cola MUST ser consumible y comprobable sin WPF, impresion, generacion de codigos de barras o validacion de licencia.
- **FR-021**: Los servicios de negocio MUST estar disponibles mediante la composicion de dependencias de la aplicacion sin permitir que Presentation consulte directamente el origen de datos.

### Key Entities

- **Product**: Articulo de solo lectura ya recuperado del catalogo; conserva identificador, codigo de barras, descripcion, presentacion, precio final con impuestos y existencia.
- **LabelData**: Instantanea inmutable de los datos que una futura plantilla necesita para una etiqueta: producto, precio presentado e informacion adicional.
- **PendingLabel**: Captura de etiqueta pendiente asociada a una posicion de columna dentro de la fila actual.
- **Completed label row**: Conjunto inmutable y ordenado de una, dos o tres etiquetas que alcanzo la capacidad configurada y deja de formar parte de los pendientes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las consultas vacias de una bateria automatizada finaliza sin llamadas al origen de productos.
- **SC-002**: El 100% de una tabla de al menos 20 importes, incluyendo cero, negativos, mitades de redondeo e importes grandes, produce resultados `MX` y `CO` identicos bajo al menos tres culturas activas distintas.
- **SC-003**: En 100 secuencias consecutivas de captura para cada capacidad `1`, `2` y `3`, cada etiqueta aparece exactamente una vez, en orden, y no queda estado residual despues de completar la fila.
- **SC-004**: El 100% de las cancelaciones probadas deja cero pendientes y la captura siguiente ocupa la primera posicion.
- **SC-005**: Textos representativos con acentos, caracteres especiales y espacios se conservan sin cambios en todas las posiciones de una fila de tres etiquetas.
- **SC-006**: Todas las reglas de esta feature se validan sin iniciar la aplicacion grafica, acceder a una base Firebird real ni cargar un motor de reportes.
- **SC-007**: Una revision de dependencias confirma que el 100% del codigo nuevo de reglas pertenece a Business o Entities y no depende de WPF, Firebird ni bibliotecas de impresion.

## Assumptions

- Los repositorios de productos y sus errores seguros de la feature 004 son la unica fuente de datos de productos para esta etapa.
- El comportamiento legado considera `CO` como formato predeterminado cuando el codigo recibido no es exactamente `MX`.
- El redondeo monetario seguira el comportamiento decimal estandar del sistema legado y se fijara explicitamente durante el plan para evitar dependencia cultural.
- Un precio nulo representa informacion insuficiente y no debe convertirse en una etiqueta hasta que exista una regla operativa explicita.
- La cola vive en memoria durante la sesion; persistencia, recuperacion tras reinicio, concurrencia entre operadores, vista previa e impresion estan fuera de alcance.
- Licenciamiento, imagen de codigo de barras, fecha impresa, plantillas y decisiones entre vista previa o impresion directa pertenecen a features posteriores.
- Las filas completas se entregan al consumidor como valores inmutables; el consumidor sera responsable de imprimirlas o descartarlas.
