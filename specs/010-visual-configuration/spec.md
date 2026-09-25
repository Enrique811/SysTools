# Feature Specification: Configuración visual del sistema

**Feature Branch**: `[010-visual-configuration]`

**Created**: 2026-09-24

**Status**: Implemented

**Input**: User description: "Implementar la Feature 010 de configuración visual para conexión Firebird y preferencias operativas de SysTools."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Completar la configuración inicial (Priority: P1)

Como operador que abre SysTools sin una conexión configurada, quiero capturar y comprobar los datos indispensables para poder comenzar a utilizar el verificador sin editar archivos manualmente.

**Why this priority**: La primera ejecución no puede completar el flujo operativo hasta disponer de una configuración de conexión válida y comprobada.

**Independent Test**: Con un perfil sin datos de conexión, se inicia la aplicación, se presenta automáticamente la configuración inicial, se capturan servidor, base, usuario y contraseña, se prueba la conexión y se guarda; al volver a abrir, los valores no sensibles están disponibles y el sistema puede preparar el verificador.

**Acceptance Scenarios**:

1. **Given** no existe una configuración lista para conexión, **When** inicia SysTools, **Then** se abre la configuración inicial y se explica qué datos faltan sin cerrar la shell.
2. **Given** datos completos y una conexión disponible, **When** el operador selecciona Probar conexión, **Then** recibe confirmación textual y puede guardar.
3. **Given** datos incompletos o una conexión fallida, **When** intenta probar o guardar, **Then** ve errores asociados a los campos o al resultado operativo y conserva la captura para corregirla.
4. **Given** una configuración inicial guardada correctamente, **When** cierra la configuración, **Then** el verificador vuelve a prepararse con los nuevos valores.

---

### User Story 2 - Modificar preferencias operativas con seguridad (Priority: P1)

Como operador, quiero abrir la configuración desde la shell y actualizar conexión, ambiente, formato, columnas e información complementaria para adaptar SysTools sin exponer credenciales ni perder la configuración vigente.

**Why this priority**: Los cambios operativos son parte del uso normal y deben utilizar la misma fuente segura consumida por el resto del sistema.

**Independent Test**: Partiendo de una configuración válida, se abre la pantalla, se modifican preferencias, se guarda y se vuelve a abrir; cada valor funcional coincide con lo elegido, la contraseña existente nunca se muestra y un guardado fallido no altera la versión anterior.

**Acceptance Scenarios**:

1. **Given** una configuración guardada, **When** el operador abre Configuración, **Then** ve los valores no sensibles actuales y una indicación de credencial conservada, pero nunca el password recuperado.
2. **Given** el operador no captura una nueva contraseña, **When** guarda otros cambios, **Then** se conserva la credencial vigente.
3. **Given** el operador captura una contraseña nueva, **When** prueba y guarda correctamente, **Then** la nueva credencial sustituye a la anterior sin mostrarse posteriormente.
4. **Given** ambiente, formato o columnas fuera de las opciones admitidas, **When** intenta guardar, **Then** el sistema rechaza el cambio y señala todos los problemas detectados.
5. **Given** ocurre un fallo de persistencia, **When** termina el intento, **Then** la última configuración válida sigue disponible y el operador recibe un mensaje controlado.

---

### User Story 3 - Preparar licencia e impresión futura (Priority: P2)

Como operador o técnico de soporte, quiero consultar el identificador del equipo, importar una licencia y elegir impresora y plantilla para dejar preparado el entorno para las capacidades posteriores.

**Why this priority**: Licencia, impresora y plantilla condicionan la operación futura, aunque esta feature todavía no genera ni imprime etiquetas.

**Independent Test**: Se obtiene el identificador del equipo, se copia, se selecciona un archivo de licencia y se valida, se eligen impresora y plantilla entre opciones disponibles y se guardan; al reabrir, las selecciones persisten y no se ejecuta ninguna impresión.

**Acceptance Scenarios**:

1. **Given** el identificador del equipo está disponible, **When** se abre la configuración, **Then** se muestra en formato legible y puede copiarse sin editarse.
2. **Given** un archivo de licencia válido y correspondiente al equipo, **When** el operador lo importa, **Then** se muestra su estado y vigencia antes de guardar su referencia.
3. **Given** una licencia ilegible, inválida, vencida o de otro equipo, **When** intenta importarla, **Then** se informa el estado sin guardar datos engañosos ni revelar contenido interno.
4. **Given** impresoras o plantillas disponibles, **When** el operador elige una, **Then** la selección queda preparada y persistida sin imprimir ni generar vista previa.
5. **Given** una impresora o plantilla previamente guardada que ya no está disponible, **When** abre la configuración, **Then** el valor se conserva y aparece claramente como no disponible para que pueda corregirlo.

### Edge Cases

- La configuración no existe, está malformada o contiene un secreto que ya no puede recuperarse.
- El operador cierra la configuración inicial sin guardar; la shell permanece estable, pero el módulo dependiente continúa no disponible.
- Se intenta guardar dos veces mientras existe una prueba, validación o guardado en curso.
- La prueba de conexión tarda, se cancela o devuelve autenticación, servidor, base o timeout no disponibles.
- El password contiene espacios, acentos o caracteres especiales; nunca se copia a mensajes, historial visible ni logs.
- El archivo de base o licencia se selecciona desde una ruta larga, con espacios o caracteres especiales.
- El archivo de licencia desaparece después de seleccionarlo o supera los límites admitidos.
- No se puede obtener el UUID, acceder al portapapeles, enumerar impresoras o descubrir plantillas.
- No hay impresoras o plantillas disponibles.
- Se cambia un campo después de una prueba exitosa; la confirmación anterior deja de considerarse vigente.
- La configuración cambia externamente mientras la pantalla está abierta.
- La ventana se cierra durante una operación cooperativa o no cooperativa y llega una respuesta tardía.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: SysTools MUST ofrecer una configuración inicial y una configuración general accesibles desde la shell mediante acciones visibles y teclado.
- **FR-002**: Cuando la configuración cargada no esté lista para conexión, la configuración inicial MUST abrirse automáticamente sin cerrar ni sustituir la shell.
- **FR-003**: La configuración visual MUST permitir capturar servidor, ruta de base, usuario y una nueva contraseña de conexión.
- **FR-004**: La configuración visual MUST permitir seleccionar un archivo de base y conservar también la captura manual de su ruta.
- **FR-005**: La contraseña almacenada MUST NOT mostrarse, copiarse ni devolverse a un campo visual; la pantalla solo puede indicar que existe una credencial conservada.
- **FR-006**: Dejar vacía la captura de nueva contraseña durante una edición MUST conservar la credencial vigente; en configuración inicial sin credencial, una contraseña no vacía MUST ser requerida.
- **FR-007**: El operador MUST poder probar la conexión con los valores actualmente capturados antes de guardarlos.
- **FR-008**: Una prueba de conexión MUST distinguir éxito, configuración inválida, autenticación, servidor, base, timeout y fallo inesperado mediante mensajes controlados.
- **FR-009**: Cambiar cualquier dato de conexión después de una prueba exitosa MUST invalidar esa confirmación.
- **FR-010**: Durante carga, prueba, validación o guardado, la pantalla MUST comunicar estado ocupado e impedir operaciones duplicadas, cierres contradictorios y publicaciones tardías.
- **FR-011**: El guardado inicial MUST requerir una configuración persistible y una prueba de conexión exitosa correspondiente exactamente a los datos vigentes.
- **FR-012**: Al editar una configuración ya funcional, todo cambio de conexión MUST requerir una nueva prueba exitosa; cambios exclusivamente operativos MAY conservar la disponibilidad previa.
- **FR-013**: La configuración visual MUST permitir elegir ambiente únicamente entre pruebas y producción, formato de precio únicamente entre Colombia y México, y columnas únicamente entre 1, 2 y 3.
- **FR-014**: La configuración visual MUST permitir editar impresora, plantilla de etiqueta e información adicional.
- **FR-015**: Impresora y plantilla MUST presentarse desde opciones disponibles; un valor guardado que ya no esté disponible MUST conservarse y marcarse como tal hasta que el operador lo cambie.
- **FR-016**: El sistema MUST permitir obtener y mostrar el UUID del equipo como dato de solo lectura y MUST ofrecer una acción para copiarlo.
- **FR-017**: Si el UUID no está disponible o no puede copiarse, el sistema MUST mostrar un resultado recuperable sin cerrar la pantalla.
- **FR-018**: El operador MUST poder seleccionar un archivo `.lic`, validar su compatibilidad, firma, equipo y vigencia antes de guardar su referencia.
- **FR-019**: Una licencia no válida MUST mostrar su categoría segura y MUST NOT reemplazar una referencia válida ya guardada salvo decisión explícita posterior del operador.
- **FR-020**: La pantalla MUST mostrar únicamente metadatos seguros de una licencia válida, incluidos emisor y vigencia, y MUST NOT mostrar firma, contenido completo ni datos técnicos internos.
- **FR-021**: Guardar MUST validar todos los campos en una sola evaluación y presentar todos los problemas detectados, asociados a su sección o campo.
- **FR-022**: Un guardado exitoso MUST persistir el conjunto completo, cerrar la ventana cuando corresponda y provocar que los módulos dependientes vuelvan a evaluar su disponibilidad.
- **FR-023**: Cancelar o cerrar sin guardar MUST descartar únicamente los cambios de la sesión visual y MUST NOT alterar la configuración persistida.
- **FR-024**: Un fallo de carga o guardado MUST mantener la pantalla operable, conservar la última configuración válida y ofrecer reintento cuando sea aplicable.
- **FR-025**: La pantalla MUST advertir antes de descartar cambios no guardados.
- **FR-026**: Los mensajes y registros MUST identificar etapa, estado y duración sin incluir password, contenido de licencia, configuración completa, rutas privadas ni UUID completo.
- **FR-027**: Todas las acciones principales MUST poder completarse con teclado, los campos MUST tener etiquetas accesibles y los estados MUST distinguirse mediante texto además de color.
- **FR-028**: La configuración MUST ser legible y operable desde 1280 por 720 hasta pantalla completa, incluido 125% de escala, con desplazamiento cuando el contenido lo requiera.
- **FR-029**: La interfaz MUST coordinar las capacidades existentes de configuración, conexión, hardware y licencia sin acceder directamente a archivos, protección de secretos, procesos del sistema o Firebird.
- **FR-030**: Esta feature MUST NOT generar etiquetas, mostrar vista previa, imprimir, editar plantillas, administrar impresoras ni modificar archivos de licencia.

### Key Entities

- **Borrador de configuración**: Copia editable de los valores no sensibles y de los secretos recién capturados; permite validar sin modificar la configuración persistida.
- **Confirmación de conexión**: Resultado temporal asociado a la versión exacta de los datos de conexión probados; pierde vigencia al cambiar cualquiera de ellos.
- **Opción operativa**: Impresora o plantilla disponible para selección, con capacidad de representar un valor persistido actualmente no disponible.
- **Resumen de licencia**: Estado seguro de la licencia elegida, con emisor y vigencia cuando sea válida, sin transportar su contenido o firma a la vista.
- **Resultado de configuración**: Estado de carga, prueba, guardado, cancelación o fallo que permite a la shell decidir si debe reintentar la preparación de sus módulos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En 20 recorridos de primera configuración, el 100% impide guardar antes de una prueba exitosa y permite completar el proceso después de ella sin editar archivos manualmente.
- **SC-002**: En 50 ediciones que no cambian la contraseña, el 100% conserva la credencial existente; en ninguna inspección visual, mensaje o registro aparece su valor.
- **SC-003**: En una matriz de resultados de conexión, el 100% de las categorías previstas produce un mensaje distinguible y deja la pantalla utilizable.
- **SC-004**: En 100 operaciones consecutivas de prueba/guardado no se producen duplicados, resultados cruzados ni respuestas antiguas publicadas como vigentes.
- **SC-005**: El 100% de combinaciones inválidas de ambiente, formato, columnas y campos requeridos muestra todos los problemas esperados en un solo intento y no altera la configuración persistida.
- **SC-006**: En pruebas con licencia válida, inválida, vencida, de otro equipo, ausente e ilegible, el 100% informa la categoría correcta sin revelar contenido sensible.
- **SC-007**: Un operador puede completar configuración inicial, copia de UUID, importación de licencia y edición general utilizando únicamente teclado.
- **SC-008**: En 20 cancelaciones o fallos de guardado, el 100% conserva byte a byte la última configuración válida y mantiene estable la shell.
- **SC-009**: La configuración permanece completamente operable en el tamaño mínimo soportado y a 125% de escala, sin ocultar las acciones de probar, guardar o cancelar.
- **SC-010**: Una auditoría automatizada encuentra cero accesos directos desde la interfaz a archivos, secretos, procesos, base de datos o impresión, y cero implementaciones nuevas de generación o impresión de etiquetas.

## Assumptions

- La pantalla se presenta como diálogo modal sobre la shell; solo puede existir una instancia activa.
- La primera ejecución se determina con el resultado de configuración existente, no por una bandera visual separada.
- La licencia se conserva como referencia al archivo `.lic`; la validación utiliza la capacidad de licenciamiento ya implementada.
- Las opciones de impresora y plantilla son preparatorias: elegirlas no comprueba calidad de impresión ni compatibilidad visual.
- Las plantillas reconocidas son archivos disponibles en la ubicación administrada por la aplicación para la futura feature de reportes.
- La selección de producción requiere una decisión explícita del operador, pero no incorpora roles ni autenticación adicional.
- Los cambios externos mientras la pantalla está abierta se resuelven recargando o reabriendo; no se fusionan automáticamente.

## Dependencies

- Configuración local y protección de secretos de Feature 002.
- Prueba de conexión Firebird de Feature 003.
- Licenciamiento y UUID de Feature 006.
- Shell, estados, accesibilidad y ciclo de vida entregados por Features 001 y 008.
- Las preferencias de impresora, plantilla y columnas ya representadas en la configuración local.

## Out of Scope

- Generación, vista previa o impresión de etiquetas.
- Creación, descarga, renovación o firma de licencias.
- Edición del contenido de archivos `.lic` o plantillas.
- Instalación, eliminación o configuración avanzada de impresoras.
- Migración automática desde la configuración Java.
- Administración remota, sincronización entre equipos, roles o autenticación de operadores.
- Cambios al esquema o consultas comerciales de Firebird.
