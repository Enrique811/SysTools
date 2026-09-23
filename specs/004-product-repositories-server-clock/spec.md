# Feature Specification: Repositorios de productos y fecha del servidor

**Feature Branch**: `004-product-repositories-server-clock`

**Created**: 2026-09-23

**Status**: Implemented - operationally validated

**Input**: User description: "Implementar repositorios Firebird para consultar productos por codigo de barras, buscar productos por descripcion y obtener fecha/hora del servidor Firebird, devolviendo entidades limpias para la capa Business."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar un producto por codigo de barras (Priority: P1)

Como consumidor de las reglas del verificador, quiero obtener un producto mediante su codigo de barras para mostrar sus datos y preparar operaciones posteriores sin conocer la estructura de la base de datos.

**Why this priority**: La consulta exacta es el flujo principal del verificador y habilita la siguiente etapa de reglas de negocio.

**Independent Test**: Con datos controlados, se consulta un codigo existente y otro inexistente; el primero devuelve todos los campos equivalentes al sistema heredado y el segundo devuelve ausencia controlada, sin confundirla con un fallo de acceso.

**Acceptance Scenarios**:

1. **Given** un producto existente con codigo exacto, **When** un consumidor lo consulta, **Then** recibe identificador, codigo de barras, descripcion, presentacion, precio final y existencia.
2. **Given** un codigo que no existe, **When** se realiza la consulta, **Then** se informa ausencia de producto sin fabricar valores ni tratarla como error tecnico.
3. **Given** un producto sin balance o con existencia nula, **When** se mapea el resultado, **Then** su existencia se representa como `Sin registro`.

---

### User Story 2 - Buscar productos por descripcion (Priority: P2)

Como consumidor de busqueda, quiero localizar productos por el inicio de su descripcion para que posteriormente un operador pueda elegir un articulo aun cuando no conozca su codigo.

**Why this priority**: Conserva la busqueda auxiliar del sistema Java y prepara la futura pantalla de seleccion, pero no bloquea la consulta principal por codigo.

**Independent Test**: Con un catalogo controlado, se busca un prefijo con diferencias de mayusculas y caracteres especiales; se obtienen solamente coincidencias por prefijo, ordenadas por descripcion y con los mismos campos que la consulta exacta.

**Acceptance Scenarios**:

1. **Given** varios productos cuyas descripciones comienzan con el texto solicitado, **When** se ejecuta la busqueda, **Then** todos los resultados coincidentes se devuelven ordenados ascendentemente por descripcion.
2. **Given** diferencias de mayusculas o minusculas, **When** se busca el mismo prefijo, **Then** la comparacion no depende de la capitalizacion.
3. **Given** un prefijo sin coincidencias, **When** termina la busqueda, **Then** se devuelve una coleccion vacia y no un valor nulo.
4. **Given** un texto vacio o compuesto solo por espacios, **When** se solicita buscar, **Then** se devuelve una coleccion vacia sin consultar el catalogo completo.

---

### User Story 3 - Obtener la fecha y hora confiable del servidor (Priority: P3)

Como consumidor de reglas sensibles al tiempo, quiero obtener la fecha y hora del servidor de datos para que etiquetas y licencias posteriores no dependan del reloj local del equipo.

**Why this priority**: El licenciamiento necesita una fuente temporal resistente a cambios del reloj local, aunque esta feature todavia no valida licencias.

**Independent Test**: Se solicita la hora a una fuente controlada, se verifica que el valor provenga de la respuesta del servidor y se comprueba que cambiar el reloj local no altere el resultado.

**Acceptance Scenarios**:

1. **Given** una conexion valida, **When** se solicita la fecha y hora, **Then** se devuelve el timestamp informado por el servidor.
2. **Given** una fecha de servidor conocida y un reloj local diferente, **When** se consulta la hora, **Then** el resultado conserva el valor del servidor.
3. **Given** una consulta cancelada o fallida, **When** no se puede obtener la fecha del servidor, **Then** el consumidor puede distinguir el fallo de un timestamp valido.

### Edge Cases

- Codigos de barras de hasta 50 caracteres, con ceros iniciales, deben conservarse como texto.
- Descripciones nulas se normalizan a texto vacio; descripciones de hasta 255 caracteres y caracteres compatibles con la codificacion heredada no deben truncarse ni corromperse.
- Presentacion o precio nulos deben conservar su ausencia para que una regla posterior decida como mostrarlos; no se inventa un valor comercial.
- Existencia nula, vacia o no disponible por ausencia de balance se representa como `Sin registro`; una existencia numerica, incluso negativa o fraccionaria, conserva su representacion recuperada.
- Una cancelacion puede ocurrir antes de abrir la conexion, durante la apertura, al ejecutar el comando o mientras se leen resultados.
- Un fallo de red o de la base no debe convertirse en "producto no encontrado" ni en una lista vacia.
- Ninguna operacion debe modificar productos, inventarios ni el reloj del servidor.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: SysTools MUST ofrecer una capacidad reutilizable para consultar un producto mediante coincidencia exacta de su codigo de barras.
- **FR-002**: Una consulta exacta MUST devolver ausencia controlada cuando no exista el producto y MUST distinguirla de un fallo de acceso a datos.
- **FR-003**: SysTools MUST ofrecer una busqueda de productos por prefijo de descripcion, sin distinguir mayusculas de minusculas y ordenada ascendentemente por descripcion.
- **FR-004**: Una busqueda sin coincidencias o con un prefijo vacio MUST devolver una coleccion vacia; un prefijo vacio MUST NOT provocar una lectura completa del catalogo.
- **FR-005**: Cada producto recuperado MUST incluir identificador, codigo de barras, descripcion, presentacion, precio final con impuestos y existencia.
- **FR-006**: Los codigos de barras MUST conservarse como texto, incluidos sus ceros iniciales.
- **FR-007**: La existencia nula, vacia o ausente MUST normalizarse como `Sin registro`; los demas valores MUST conservar la informacion entregada por la base.
- **FR-008**: Descripcion, presentacion y precio MUST tolerar valores nulos sin provocar el cierre de la aplicacion ni inventar datos comerciales.
- **FR-009**: Las consultas MUST conservar descripciones completas de hasta 255 caracteres y caracteres especiales compatibles con la base heredada.
- **FR-010**: SysTools MUST ofrecer una capacidad reutilizable para obtener fecha y hora actuales desde el servidor de datos y MUST NOT sustituirla por el reloj local.
- **FR-011**: La fecha y hora MUST conservar precision suficiente para que licenciamiento e impresion posteriores puedan usar el mismo instante recuperado.
- **FR-012**: Todas las entradas variables MUST tratarse como datos y MUST NOT alterar la estructura de las consultas.
- **FR-013**: Todas las operaciones MUST aceptar cancelacion del consumidor y MUST liberar conexion, comando y lector al finalizar con exito, ausencia, fallo o cancelacion.
- **FR-014**: Cada operacion MUST usar recursos independientes y MUST NOT introducir conexion, comando, lector, producto o resultado global compartido.
- **FR-015**: Los fallos recuperables MUST registrar diagnostico estructurado suficiente para soporte sin incluir credenciales, cadena de conexion, host, ruta de base, texto buscado ni datos completos del producto.
- **FR-016**: Las capacidades de esta feature MUST estar disponibles mediante abstracciones consumibles por Business, mientras el acceso a Firebird permanece aislado de Entities, Views y ViewModels.
- **FR-017**: La feature MUST ser de solo lectura y MUST NOT insertar, actualizar o eliminar datos.
- **FR-018**: La consulta principal MUST conservar la equivalencia funcional con `PRODUCTOS` y su balance opcional de inventario del sistema Java; la consulta temporal MUST usar la fecha y hora del servidor.
- **FR-019**: Esta feature MUST NOT implementar reglas de formato de precio, precios especiales, licencia, codigos de barras graficos, UI de busqueda, etiquetas ni impresion.

### Key Entities

- **Product**: Representa un articulo recuperado del catalogo. Incluye identificador, codigo de barras textual, descripcion opcional normalizada para consumo seguro, presentacion opcional, precio final opcional y existencia textual normalizada.
- **Server timestamp**: Instante informado por el servidor de datos, independiente del reloj local y destinado a reglas posteriores de licenciamiento e impresion.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los casos automatizados de producto existente, inexistente y sin balance devuelve respectivamente el producto esperado, ausencia controlada y `Sin registro`.
- **SC-002**: El 100% de los resultados de busqueda controlados coincide solo por prefijo, ignora diferencias de capitalizacion y mantiene orden ascendente por descripcion.
- **SC-003**: Una matriz automatizada de descripciones con acentos, caracteres especiales, valores nulos y longitudes de hasta 255 caracteres se recupera sin excepciones, truncamiento ni sustitucion de caracteres.
- **SC-004**: En 100 operaciones mixtas de exito, ausencia, fallo y cancelacion, cada recurso creado se libera exactamente una vez y ninguna operacion comparte estado mutable con otra.
- **SC-005**: En 10 consultas consecutivas contra un entorno autorizado, productos y fecha del servidor coinciden con consultas de referencia ejecutadas sobre la misma base.
- **SC-006**: Cambiar el reloj local durante una prueba controlada produce cero cambios en el timestamp devuelto para una respuesta de servidor fija.
- **SC-007**: Una auditoria de seguridad encuentra cero credenciales, cadenas de conexion, entradas de busqueda o datos completos de producto en resultados de error y logs.
- **SC-008**: Una auditoria de alcance encuentra cero operaciones de escritura y cero implementaciones de precio especial, licencia, UI, barcode, reportes o impresion.

## Assumptions

- La estructura vigente conserva `PRODUCTOS.ID` como identificador unico, `PRODUCTOS.CODIGO` como codigo de barras unico y un maximo de un registro de `INVENTARIO_BALANCES` por producto.
- La busqueda por descripcion conserva el comportamiento Java `{texto}%`; no se agrega busqueda por contenido ni paginacion en esta etapa.
- La coleccion completa de coincidencias se devuelve como en el sistema heredado; limites de resultados pertenecen a la futura experiencia de busqueda si fueran necesarios.
- La configuracion segura y la fabrica de conexiones independientes de las features 002 y 003 estan disponibles.
- Los precios se recuperan sin aplicar formato regional; el formato `CO` o `MX` pertenece a la siguiente feature de reglas de negocio.
- Los fallos de infraestructura se propagan como fallos controlables por capas posteriores y nunca se representan como ausencia de producto, lista vacia o timestamp valido.

## Dependencies

- Feature `002-local-configuration-secrets` para proporcionar configuracion y credenciales en memoria.
- Feature `003-firebird-connection` para crear conexiones independientes compatibles con `ISO8859_1`.
- Base Firebird no productiva autorizada con productos representativos para validacion integrada.
- Estructura heredada documentada de `PRODUCTOS`, `INVENTARIO_BALANCES` y `RDB$DATABASE`.

## Out of Scope

- Precios especiales y tablas auxiliares heredadas de articulos o tarifas.
- Formateo regional de precios y reglas de cola de etiquetas.
- Validacion de licencias o decisiones basadas en vigencia.
- Generacion de codigos de barras, vistas WPF y dialogos de busqueda.
- Escritura de productos, inventarios o cualquier otro dato.
- Reintentos automaticos, cache de productos, paginacion o conexion global.
