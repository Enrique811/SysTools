# Feature Specification: Búsqueda de productos por descripción

**Feature Branch**: `[009-product-search]`

**Created**: 2026-09-24

**Status**: Complete

**Input**: User description: "Continuar con la Feature 009: búsqueda modal de productos por descripción, selección y carga en el verificador."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Encontrar productos sin conocer el código (Priority: P1)

Como operador, quiero buscar productos por el inicio de su descripción para localizar el artículo correcto cuando no conozco o no puedo escanear su código de barras.

**Why this priority**: Es el objetivo central de la feature y completa la alternativa operativa prevista en el sistema anterior sin alterar la consulta rápida por código.

**Independent Test**: Con el verificador disponible y un catálogo controlado, se abre la búsqueda, se introduce un prefijo y se comprueba que la lista contiene únicamente las coincidencias esperadas, ordenadas por descripción.

**Acceptance Scenarios**:

1. **Given** el verificador disponible, **When** el operador activa Buscar o usa Ctrl+F7, **Then** se abre una búsqueda sobre el módulo actual y el campo de descripción recibe el foco.
2. **Given** productos cuyas descripciones comienzan con el texto capturado, **When** el operador confirma la búsqueda, **Then** se muestran las coincidencias ordenadas ascendentemente por descripción.
3. **Given** diferencias de mayúsculas y minúsculas, **When** se busca el mismo prefijo, **Then** se obtienen las mismas coincidencias.
4. **Given** una búsqueda en curso, **When** el operador observa la ventana, **Then** distingue el estado ocupado y no puede iniciar búsquedas duplicadas.
5. **Given** un texto vacío o compuesto solo por espacios, **When** el operador intenta buscar, **Then** no se consulta el catálogo y se muestra una indicación breve.

---

### User Story 2 - Seleccionar y cargar un producto (Priority: P1)

Como operador, quiero seleccionar un resultado con teclado o doble clic para cargarlo en el verificador y consultar sus datos sin copiar manualmente el código.

**Why this priority**: Encontrar productos no aporta el valor completo si el operador no puede continuar inmediatamente con el flujo principal.

**Independent Test**: Con una lista de resultados controlada, se selecciona un producto mediante Enter y doble clic, y se verifica que el diálogo cierre y el verificador muestre el producto correspondiente usando su código exacto.

**Acceptance Scenarios**:

1. **Given** una fila seleccionada, **When** el operador confirma con Enter, **Then** el producto se carga en el verificador, la búsqueda se cierra y el código queda visible, enfocado y seleccionado.
2. **Given** una fila seleccionada, **When** el operador hace doble clic, **Then** ocurre la misma selección que con Enter y se ejecuta una sola consulta del producto.
3. **Given** resultados visibles sin una fila válida seleccionada, **When** el operador intenta confirmar, **Then** no se cierra la búsqueda ni se consulta un producto.
4. **Given** que el producto dejó de existir entre la búsqueda y la selección, **When** se intenta cargarlo, **Then** el verificador presenta el estado controlado de producto no encontrado y queda listo para otra captura.

---

### User Story 3 - Cancelar o recuperarse sin perder el flujo (Priority: P2)

Como operador, quiero cerrar la búsqueda o recuperarme de resultados vacíos y fallos sin dejar información contradictoria ni perder innecesariamente el estado del verificador.

**Why this priority**: Cancelar, no encontrar coincidencias y sufrir fallos temporales son situaciones normales que deben conservar una experiencia segura y predecible.

**Independent Test**: Se abre la búsqueda con un resultado previo en el verificador; se prueba cancelar, buscar sin coincidencias, cerrar durante una operación y provocar un fallo controlado, verificando el estado final en cada caso.

**Acceptance Scenarios**:

1. **Given** un resultado previo en el verificador, **When** el operador cancela con Escape o cierra la búsqueda, **Then** el resultado previo permanece sin cambios y el foco vuelve al campo de código.
2. **Given** un prefijo sin coincidencias, **When** termina la búsqueda, **Then** se muestra un mensaje de ausencia, la ventana permanece abierta y el operador puede cambiar el texto.
3. **Given** una búsqueda en curso, **When** la ventana se cierra, **Then** cualquier respuesta posterior queda descartada y no altera el verificador.
4. **Given** un fallo recuperable de acceso al catálogo, **When** termina la búsqueda, **Then** se cierra la búsqueda, se limpian datos que ya no sean vigentes, el módulo se bloquea y ofrece Reintentar.

### Edge Cases

- Los espacios exteriores se retiran; el contenido interior, acentos y caracteres especiales se conservan.
- Un texto mayor de 255 caracteres no llega al catálogo y produce un mensaje controlado.
- Confirmaciones repetidas mientras una búsqueda está en curso se ignoran sin cancelar, encolar ni iniciar otra búsqueda.
- Una respuesta de búsqueda antigua no puede sustituir resultados de una búsqueda posterior.
- Productos con descripción, precio, presentación o existencia ausentes se muestran con valores neutrales sin inventar información.
- Descripciones largas y mensajes extensos se ajustan sin ocultar el campo de búsqueda ni la tabla en el tamaño mínimo soportado.
- Si el módulo pierde disponibilidad mientras la búsqueda está abierta, la búsqueda deja de aceptar acciones y se cierra de forma segura.
- Si una selección no contiene un código utilizable, no se inicia la consulta exacta y se informa el problema sin cerrar abruptamente la aplicación.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El operador MUST poder abrir la búsqueda mediante el control Buscar y mediante Ctrl+F7.
- **FR-002**: Buscar MUST estar disponible únicamente cuando el verificador esté preparado y no exista otra operación en curso.
- **FR-003**: Al abrir la búsqueda, el campo de descripción MUST recibir el foco y permitir operación completa mediante teclado.
- **FR-004**: La búsqueda MUST usar coincidencia por inicio de descripción y MUST NOT convertirla en búsqueda por contenido, aproximada o por palabras independientes.
- **FR-005**: La comparación MUST ser insensible a diferencias de mayúsculas y minúsculas.
- **FR-006**: El sistema MUST retirar espacios exteriores del texto y conservar su contenido interior, acentos y caracteres especiales.
- **FR-007**: Un texto vacío, compuesto solo por espacios o mayor de 255 caracteres MUST producir un mensaje controlado y MUST NOT consultar el catálogo.
- **FR-008**: El operador MUST poder confirmar una búsqueda mediante Enter y mediante una acción visible.
- **FR-009**: Cada confirmación aceptada MUST iniciar como máximo una búsqueda.
- **FR-010**: Mientras una búsqueda esté en curso, el sistema MUST mostrar estado ocupado, deshabilitar nuevas confirmaciones e ignorar reentradas sin cola ni cancelación de la operación vigente.
- **FR-011**: Los resultados MUST mostrarse ordenados ascendentemente por descripción.
- **FR-012**: Cada resultado MUST mostrar código de barras, descripción, precio final y existencia.
- **FR-013**: Los precios MUST usar el formato vigente del verificador y los valores ausentes MUST mostrarse mediante valores neutrales.
- **FR-014**: Una búsqueda sin coincidencias MUST devolver una lista vacía, mostrar un mensaje claro y permitir una nueva búsqueda sin cerrar la ventana.
- **FR-015**: El operador MUST poder recorrer la lista y seleccionar una fila utilizando únicamente el teclado.
- **FR-016**: El operador MUST poder confirmar una selección mediante Enter o doble clic; ambas acciones MUST producir el mismo resultado y como máximo una consulta exacta.
- **FR-017**: La selección MUST cargar el producto en el verificador usando su código exacto y MUST reutilizar las mismas reglas de presentación, precio y disponibilidad de la consulta por código.
- **FR-018**: Tras una selección válida, la búsqueda MUST cerrarse y el campo de código MUST conservar el código visible, recibir el foco y quedar seleccionado por completo.
- **FR-019**: Confirmar sin una selección válida MUST mantener abierta la búsqueda y MUST NOT consultar un producto.
- **FR-020**: Cancelar con Escape o cerrar la búsqueda sin seleccionar MUST conservar el resultado previo del verificador y devolver el foco al campo de código.
- **FR-021**: Cerrar o reemplazar una búsqueda en curso MUST impedir que respuestas antiguas publiquen resultados o mensajes.
- **FR-022**: Un fallo recuperable del catálogo MUST cerrar la búsqueda, limpiar resultados no vigentes, bloquear el módulo y ofrecer Reintentar; una entrada inválida o una lista vacía MUST NOT bloquearlo.
- **FR-023**: Los estados de búsqueda y del verificador MUST permanecer coherentes; la ventana no puede aceptar búsquedas o selección cuando el módulo no está disponible.
- **FR-024**: Los mensajes y registros propios del flujo MUST identificar etapa y categoría sin incluir el texto buscado, productos completos, códigos seleccionados, credenciales, licencia ni detalles técnicos visibles.
- **FR-025**: Los cambios de estado esenciales MUST ser perceptibles mediante texto y no depender únicamente del color.
- **FR-026**: La búsqueda MUST permanecer legible y operable desde 1280 por 720 hasta pantalla completa, incluido 125% de escala.
- **FR-027**: La interfaz MUST coordinar capacidades existentes sin acceder directamente a fuentes de datos, archivos, validación de licencia, generación de códigos, reportes o impresión.
- **FR-028**: Esta feature MUST NOT implementar edición de configuración, captura de etiquetas, generación visible de códigos de barras, reportes, vista previa, selección de impresora ni impresión.

### Key Entities

- **Criterio de búsqueda**: Texto capturado por el operador, normalizado únicamente en sus extremos y usado como prefijo de descripción.
- **Resultado de búsqueda**: Producto candidato mostrado con código, descripción, precio final y existencia, sin transportar configuración sensible.
- **Selección de producto**: Elección temporal de un resultado cuyo código exacto se entrega al flujo principal del verificador.
- **Estado de búsqueda**: Distingue reposo, búsqueda en curso, resultados, sin coincidencias, cancelación y fallo operativo, evitando publicaciones obsoletas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En una matriz de al menos 50 prefijos representativos, el 100% de los resultados corresponde al inicio de la descripción, sin coincidencias por contenido, y aparece en orden ascendente.
- **SC-002**: El 100% de las búsquedas con diferencias únicamente de mayúsculas y minúsculas produce el mismo conjunto de resultados.
- **SC-003**: Un operador puede abrir la búsqueda, localizar y cargar un producto usando solo teclado en una confirmación de búsqueda y una confirmación de selección.
- **SC-004**: En 100 búsquedas consecutivas se producen cero consultas duplicadas, resultados cruzados o respuestas antiguas mostradas como vigentes.
- **SC-005**: El 100% de selecciones válidas carga en el verificador el producto correspondiente y conserva exactamente su código de barras.
- **SC-006**: El 100% de entradas inválidas, listas vacías, cancelaciones y cierres deja la aplicación utilizable y sin datos contradictorios.
- **SC-007**: Durante operaciones de hasta 6 segundos, la interfaz permanece receptiva y comunica claramente el trabajo en curso.
- **SC-008**: Una auditoría automatizada encuentra cero términos buscados, productos completos, códigos seleccionados, credenciales, licencias o configuraciones sensibles en los mensajes y registros propios del flujo.
- **SC-009**: Todas las acciones del flujo principal pueden completarse mediante teclado y los estados siguen siendo distinguibles sin depender solo del color.
- **SC-010**: Una auditoría de alcance encuentra cero implementaciones nuevas de configuración visual, etiquetas, reportes, vista previa o impresión.

## Assumptions

- La Feature 009 corresponde a la etapa de búsqueda de productos posterior a la UI operativa de Feature 008.
- El catálogo ya ofrece búsqueda por prefijo y orden estable; esta feature expone esa capacidad al operador sin cambiar reglas de datos.
- La búsqueda se abre como una interacción modal sobre el verificador y solo existe una instancia activa.
- La búsqueda se ejecuta explícitamente con Enter o con una acción visible; no se consulta en cada pulsación.
- La selección entrega el código al flujo exacto del verificador para evitar duplicar reglas comerciales y de formato.
- Escape y el cierre de ventana equivalen a cancelar sin selección.
- Los controles Imprimir y Configuración continúan visibles y deshabilitados.

## Dependencies

- Repositorio y servicio de búsqueda por descripción de Feature 004.
- Reglas de presentación y formato de precio de Feature 005.
- Disponibilidad operativa, consulta exacta, Retry y foco de Feature 008.
- Shell modular y estilos accesibles existentes.

## Out of Scope

- Búsqueda por contenido, coincidencia aproximada, palabras independientes o múltiples filtros.
- Edición o alta de productos.
- Configuración visual, administración de licencias o conexión.
- Captura de etiquetas, generación visible de códigos de barras, reportes, vista previa e impresión.
- Cambios al esquema, consultas comerciales o reglas de precio existentes.
