# Feature Specification: Integracion operativa del verificador de precios

**Feature Branch**: `[008-price-verifier-ui]`

**Created**: 2026-09-23

**Status**: Complete

**Input**: User description: "Continuar la integracion en el cambio 008."

## Clarifications

### Session 2026-09-23

- Q: ¿Cuando debe volver a comprobarse la conexion y la licencia despues de que el modulo quedo disponible? → A: Al iniciar o reactivar el modulo; tras un fallo operativo se bloquea y ofrece Reintentar.
- Q: ¿Despues de mostrar un producto correctamente, como debe quedar el campo del codigo para la siguiente captura? → A: Conserva el codigo visible, seleccionado por completo y con foco.
- Q: ¿Que tipos de fallo deben bloquear el modulo y exigir Reintentar antes de aceptar otro codigo? → A: Solo fallos de configuracion, conexion, licencia o acceso al catalogo.
- Q: ¿Que debe ocurrir si el lector envia otro codigo mientras una consulta todavia esta en curso? → A: La captura permanece deshabilitada y las nuevas confirmaciones se ignoran hasta terminar.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Preparar el verificador para operar (Priority: P1)

Como operador, quiero que el Verificador de precios compruebe automaticamente su configuracion, conexion y licencia para saber si puedo capturar codigos sin interpretar estados tecnicos ni realizar preparativos manuales.

**Why this priority**: Ninguna consulta debe comenzar con configuracion inutilizable, conexion no comprobada o licencia invalida; este estado de disponibilidad protege el flujo principal y evita resultados ambiguos.

**Independent Test**: Se inicia el modulo con combinaciones controladas de configuracion, conexion y licencia validas o invalidas, y se comprueba que la captura solo se habilite cuando las tres condiciones requeridas sean satisfactorias.

**Acceptance Scenarios**:

1. **Given** configuracion valida, conexion disponible y licencia vigente, **When** el modulo termina de prepararse, **Then** los indicadores muestran disponibilidad, el campo de codigo queda habilitado y recibe el foco.
2. **Given** que la configuracion no existe, es invalida o no puede leerse, **When** inicia el modulo, **Then** la captura permanece deshabilitada y se muestra un mensaje amigable que orienta al operador sin revelar datos sensibles.
3. **Given** que la conexion no esta disponible, **When** se comprueba el estado operativo, **Then** el indicador de conexion refleja el fallo, la captura permanece deshabilitada y la shell continua abierta.
4. **Given** una licencia ausente, invalida, vencida o no correspondiente al equipo, **When** se valida la disponibilidad, **Then** el indicador de licencia muestra que no esta disponible, la captura permanece deshabilitada y se presenta el mensaje controlado correspondiente.
5. **Given** que la preparacion esta en curso, **When** el operador observa el modulo, **Then** puede distinguir el estado ocupado y no puede iniciar consultas prematuramente.
6. **Given** que un fallo operativo bloqueo el modulo, **When** el operador confirma Reintentar, **Then** se comprueban nuevamente configuracion, conexion y licencia antes de volver a habilitar la captura.

---

### User Story 2 - Consultar un producto por codigo (Priority: P1)

Como operador, quiero capturar o escanear un codigo de barras y ver de inmediato la descripcion, presentacion, existencia y precio final del producto para atender una consulta sin navegar a otra pantalla.

**Why this priority**: Es la operacion central del verificador y conecta en una experiencia utilizable las capacidades de configuracion, productos y formato de precio ya disponibles.

**Independent Test**: Con el modulo disponible y un catalogo controlado, se envian codigos existentes mediante teclado o lector y se verifica que cada resultado visible corresponda exactamente al producto consultado y a la configuracion de precio vigente.

**Acceptance Scenarios**:

1. **Given** el modulo disponible y un codigo existente, **When** el operador confirma la captura, **Then** se muestran el codigo, descripcion, presentacion, existencia y precio final del producto correspondiente.
2. **Given** un producto con precio, **When** se presenta el resultado, **Then** el precio usa el formato configurado y no un formato regional inferido del equipo.
3. **Given** que la configuracion contiene informacion adicional, **When** se muestra un producto, **Then** esa informacion aparece en su zona prevista sin mezclarse con la descripcion comercial.
4. **Given** un lector que envia el codigo seguido de confirmacion, **When** la captura llega al campo activo, **Then** se ejecuta una sola consulta sin requerir el mouse.
5. **Given** una consulta exitosa terminada, **When** el resultado ya es visible, **Then** el codigo consultado permanece visible y seleccionado por completo, y el campo conserva el foco para que la siguiente captura lo sustituya.
6. **Given** un producto con presentacion o precio no registrado, **When** se muestra el resultado, **Then** la interfaz usa un valor neutral y no inventa informacion comercial.
7. **Given** una consulta en curso, **When** el lector envia otra confirmacion, **Then** no se cancela la consulta vigente, no se crea una cola y no se inicia otra consulta.

---

### User Story 3 - Recuperarse de consultas sin resultado o con fallo (Priority: P2)

Como operador, quiero recibir mensajes claros cuando un codigo no existe o la consulta falla, conservando el modulo listo para reintentar y sin confundir datos anteriores con el intento actual.

**Why this priority**: Los codigos inexistentes y los fallos temporales son situaciones operativas normales que no deben cerrar la aplicacion ni dejar resultados obsoletos en pantalla.

**Independent Test**: Se consultan entradas vacias, codigos inexistentes y fallos controlados; en cada caso se verifican mensaje, limpieza de datos, disponibilidad posterior y ausencia de detalles tecnicos.

**Acceptance Scenarios**:

1. **Given** una captura vacia o compuesta solo por espacios, **When** se intenta confirmar, **Then** no se consulta el catalogo y el operador recibe una indicacion breve para ingresar un codigo.
2. **Given** un codigo que no corresponde a ningun producto, **When** termina la consulta, **Then** los detalles y el precio quedan neutrales, se informa que no hubo coincidencia y el operador puede intentar otro codigo sin ejecutar Reintentar.
3. **Given** un fallo recuperable de configuracion, conexion, licencia o acceso al catalogo, **When** termina el intento, **Then** no se muestran datos parciales o anteriores, la shell permanece abierta, el modulo se bloquea y se ofrece un mensaje seguro con la accion Reintentar.
4. **Given** una consulta en curso, **When** se cierra o desactiva el modulo, **Then** el trabajo cancelado no puede publicar posteriormente datos ni mensajes en pantalla.
5. **Given** cualquier resultado o fallo, **When** se registra diagnostico, **Then** no se incluye el codigo capturado, informacion completa del producto, credenciales, licencia ni detalles tecnicos visibles para el operador.

### Edge Cases

- Una confirmacion repetida del lector mientras el modulo esta ocupado se ignora y no debe cancelar, encolar ni iniciar otra consulta.
- Los espacios exteriores del codigo se retiran, pero los ceros iniciales y el contenido interior se conservan como texto.
- Un codigo de mas de 50 caracteres no llega al catalogo y produce un mensaje controlado.
- Un producto puede tener descripcion vacia, presentacion nula, precio nulo o existencia sin registro; la interfaz permanece estable y usa valores neutrales.
- Si la disponibilidad cambia mientras existe un resultado visible, la captura se deshabilita y los datos anteriores no se presentan como vigentes.
- Una respuesta antigua que finaliza despues de una consulta mas reciente no puede sobrescribir el estado actual.
- El cambio rapido de foco o el uso exclusivo de teclado no debe perder la confirmacion del lector.
- Los mensajes extensos deben ajustarse sin ocultar el campo de captura ni los datos principales en el tamano minimo soportado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST cargar y validar la configuracion existente al preparar el Verificador de precios.
- **FR-002**: El sistema MUST comprobar la configuracion, la conexion y la licencia al iniciar o reactivar el modulo y antes de volver a habilitarlo mediante Reintentar; MUST NOT repetir estas comprobaciones antes de cada consulta mientras el modulo conserve su disponibilidad.
- **FR-003**: El sistema MUST mostrar estados distinguibles de preparacion, disponible y no disponible para conexion y licencia, sin afirmar exito antes de comprobarlo.
- **FR-004**: El campo de codigo MUST permanecer deshabilitado mientras la preparacion esta pendiente o cualquiera de las condiciones operativas requeridas no sea satisfactoria.
- **FR-005**: Cuando el modulo este disponible, el campo de codigo MUST habilitarse, recibir el foco y permitir captura por teclado o lector que actua como teclado.
- **FR-006**: El operador MUST poder confirmar una captura sin usar el mouse.
- **FR-007**: El sistema MUST retirar espacios exteriores del codigo, conservar ceros iniciales y contenido interior, y tratarlo siempre como texto.
- **FR-008**: Una captura vacia, solo espacios o mayor de 50 caracteres MUST producir un mensaje controlado y MUST NOT consultar el catalogo.
- **FR-009**: Cada captura aceptada MUST iniciar como maximo una consulta exacta de producto.
- **FR-010**: Mientras una consulta esta en curso, el sistema MUST mostrar un estado ocupado, deshabilitar la captura e ignorar nuevas confirmaciones; MUST NOT cancelar la consulta vigente, encolar capturas ni iniciar otra consulta.
- **FR-011**: Un producto encontrado MUST mostrar codigo, descripcion, presentacion, existencia y precio final en las zonas ya reservadas del modulo.
- **FR-012**: El precio MUST usar el formato definido en la configuracion vigente.
- **FR-013**: La informacion adicional configurada MUST mostrarse en su zona propia y MUST NOT reemplazar ni modificar los datos comerciales del producto.
- **FR-014**: Valores comerciales ausentes MUST representarse mediante valores neutrales claramente distinguibles de datos reales.
- **FR-015**: Cuando no exista el producto, el sistema MUST limpiar cualquier resultado anterior, mostrar un mensaje de ausencia y permitir una nueva captura.
- **FR-016**: Ante un fallo recuperable de configuracion, conexion, licencia o acceso al catalogo, el sistema MUST limpiar datos parciales u obsoletos, conservar la shell abierta, bloquear el modulo y presentar un mensaje amigable con Reintentar; entradas invalidas y productos inexistentes MUST permitir continuar sin bloqueo.
- **FR-017**: Una consulta cancelada por cierre o desactivacion del modulo MUST NOT publicar resultados ni mensajes obsoletos; las capturas recibidas mientras existe una consulta vigente MUST ignorarse y no pueden superarla.
- **FR-018**: Al finalizar una consulta exitosa, el campo MUST conservar visible el codigo consultado, seleccionarlo por completo y mantener el foco para que la siguiente captura lo reemplace sin concatenarse; tras cualquier otro intento, MUST quedar igualmente preparado para una nueva captura cuando el modulo siga disponible.
- **FR-019**: Si un fallo de configuracion, conexion, licencia o acceso al catalogo hace perder la disponibilidad, el sistema MUST deshabilitar nuevas capturas, MUST NOT presentar el ultimo resultado como vigente y MUST ofrecer una accion Reintentar que repita todas las comprobaciones de preparacion.
- **FR-020**: Los estados visibles MUST permanecer coherentes entre la shell y el modulo; conexion o licencia no pueden mostrarse simultaneamente como disponible y bloqueante.
- **FR-021**: Los registros de diagnostico MUST identificar etapa y categoria de resultado sin incluir codigo capturado, datos completos del producto, credenciales, contenido de licencia ni configuracion sensible.
- **FR-022**: La interfaz MUST coordinar las capacidades existentes sin consultar directamente fuentes de datos, leer archivos, validar firmas, generar imagenes o aplicar reglas comerciales por su cuenta.
- **FR-023**: Todas las interacciones MUST permanecer utilizables mediante teclado y los cambios de estado esenciales MUST ser perceptibles sin depender unicamente del color.
- **FR-024**: La interfaz MUST permanecer legible y sin superposiciones desde 1280 por 720 hasta pantalla completa.
- **FR-025**: Esta feature MUST NOT implementar busqueda por descripcion, edicion de configuracion, captura de etiquetas, imagen visible de codigo de barras, reportes, vista previa, seleccion de impresora ni impresion.

### Key Entities

- **Disponibilidad operativa**: Estado combinado de configuracion, conexion, licencia y acceso al catalogo que determina si se permiten consultas y proporciona un mensaje seguro con Reintentar cuando existe un bloqueo.
- **Captura de codigo**: Texto introducido por teclado o lector, conservado como texto y asociado a un unico intento vigente.
- **Resultado visible de producto**: Proyeccion temporal del producto consultado con codigo, descripcion, presentacion, existencia, precio formateado e informacion adicional.
- **Estado de consulta**: Distingue reposo, consulta en curso, producto encontrado, sin coincidencia, cancelacion y fallo recuperable, evitando que respuestas anteriores sustituyan el estado vigente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En el 100% de los escenarios de preparacion definidos, la captura solo queda habilitada cuando configuracion, conexion y licencia han sido comprobadas satisfactoriamente.
- **SC-002**: En una matriz de al menos 50 productos representativos, el 100% de las consultas existentes muestra el producto correcto y conserva exactamente el codigo textual, incluidos ceros iniciales.
- **SC-003**: El 100% de los precios con formatos soportados coincide con el resultado esperado para la configuracion vigente.
- **SC-004**: Un operador puede completar una consulta existente usando solo lector o teclado en una unica confirmacion y sin cambiar de pantalla.
- **SC-005**: En 100 consultas consecutivas, se producen cero envios duplicados, resultados cruzados o respuestas antiguas mostradas como actuales.
- **SC-006**: El 100% de entradas invalidas, productos ausentes, cancelaciones y fallos recuperables deja la shell utilizable, sin datos obsoletos ni detalles tecnicos visibles.
- **SC-007**: Durante operaciones de hasta 6 segundos, la interfaz permanece receptiva y comunica claramente que existe trabajo en curso.
- **SC-008**: Una auditoria automatizada encuentra cero codigos capturados, datos completos de producto, credenciales, licencias o configuraciones sensibles en los mensajes y registros propios del flujo.
- **SC-009**: Todas las funciones del flujo principal pueden completarse mediante teclado y los estados esenciales siguen siendo distinguibles sin depender solo del color.
- **SC-010**: Una auditoria de alcance encuentra cero implementaciones de busqueda por descripcion, edicion de configuracion, etiquetas, reportes, vista previa o impresion.

## Assumptions

- La Feature 008 corresponde a la etapa de UI del roadmap constitucional y activa el flujo exacto del Verificador de precios; la busqueda por descripcion se mantiene para la feature siguiente.
- El lector de codigos se comporta como teclado y envia una confirmacion al final; no requiere un controlador especializado.
- Una licencia valida y una conexion comprobada son condiciones necesarias para consultar productos.
- La configuracion, conexion, repositorio de productos, formato de precio y licenciamiento ya existen y se reutilizan sin cambiar sus reglas funcionales.
- La informacion adicional procede de la configuracion vigente y es de solo lectura en esta feature.
- Solo existe una consulta vigente por modulo; cualquier captura posterior recibida antes de terminar se ignora, sin cancelacion ni cola.
- No se realiza comprobacion periodica ni previa a cada captura; un fallo operativo bloquea el modulo hasta que Reintentar complete nuevamente la preparacion.
- Los controles Buscar, Imprimir y Configuracion permanecen visibles y deshabilitados.

## Dependencies

- Configuracion local y proteccion de secretos de Feature 002.
- Comprobacion de conexion de Feature 003.
- Consulta exacta de productos de Feature 004.
- Formato de precio y reglas de presentacion de Feature 005.
- Validacion de licencia de Feature 006.
- Shell modular y zonas visuales creadas en Feature 001.

## Out of Scope

- Busqueda y seleccion de productos por descripcion.
- Creacion o edicion visual de configuracion.
- Generacion visible de imagenes de codigo de barras.
- Captura o agrupacion de etiquetas.
- Plantillas, reportes, vista previa e impresion.
- Administracion de licencias o seleccion manual de archivos.
- Nuevos formatos de precio, reglas comerciales o consultas de datos.
