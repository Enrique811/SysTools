# Feature Specification: Correos, soporte y logs

**Feature Branch**: `[012-support-mail-logs]`

**Created**: 2026-09-25

**Status**: Complete

**Input**: User description: "Implementar la fase 012 de correos, soporte y logs"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Solicitar licencia (Priority: P1)

Como operador, quiero copiar el identificador del equipo y preparar una solicitud de licencia en mi cliente de correo preferido para pedir activación sin transcribir datos técnicos.

**Why this priority**: Sin una licencia válida el verificador no puede operar, por lo que soporte debe recibir una solicitud reproducible y controlada.

**Independent Test**: Con un identificador disponible se abre el módulo, se copia el valor y se genera un borrador por Gmail, Outlook o el cliente predeterminado; ningún canal envía el correo automáticamente.

**Acceptance Scenarios**:

1. **Given** un identificador disponible, **When** el operador elige solicitar licencia, **Then** se prepara un asunto y cuerpo con el identificador y la versión de la aplicación.
2. **Given** un borrador válido, **When** se elige Gmail, Outlook o cliente predeterminado, **Then** se abre exactamente un destino con campos codificados y queda al operador revisar y enviar.
3. **Given** que el identificador no está disponible, **When** se solicita licencia o copiar UUID, **Then** la acción se bloquea con un mensaje controlado y no se abre ningún destino.

---

### User Story 2 - Reportar un error (Priority: P1)

Como operador, quiero describir un problema y preparar un correo de soporte con diagnóstico seguro para solicitar ayuda sin revelar configuración sensible.

**Why this priority**: Un reporte consistente reduce el tiempo de diagnóstico y evita que credenciales, rutas privadas o información comercial se filtren por accidente.

**Independent Test**: Se captura un resumen y detalle, se genera un borrador con versión y código de diagnóstico no sensible, y se abre por cualquiera de los tres canales sin envío automático ni adjuntos.

**Acceptance Scenarios**:

1. **Given** un resumen y detalle válidos, **When** se prepara el reporte, **Then** el borrador contiene esos textos, versión y un código opaco de diagnóstico.
2. **Given** contenido vacío o fuera de límites, **When** se intenta preparar, **Then** se muestran validaciones y no se abre correo.
3. **Given** datos con caracteres especiales, **When** se abre un canal, **Then** asunto y cuerpo se conservan después de codificarse y se abre una sola vez.

---

### User Story 3 - Consultar y localizar logs (Priority: P2)

Como operador o persona de soporte, quiero ver los logs recientes y abrir su carpeta para localizar evidencia técnica sin navegar por rutas internas manualmente.

**Why this priority**: Los logs ayudan a soporte después de recibir el reporte, pero la solicitud y el reporte pueden funcionar aun si no existen archivos.

**Independent Test**: Con cero, uno y varios archivos se muestra un catálogo ordenado; seleccionar uno carga una vista previa acotada y la acción de localizar abre únicamente la carpeta administrada.

**Acceptance Scenarios**:

1. **Given** varios logs administrados, **When** se abre el módulo, **Then** se listan los 20 más recientes con nombre simple, fecha y tamaño, sin exponer la ruta completa.
2. **Given** un log válido, **When** se selecciona, **Then** se muestran como máximo las últimas 500 líneas y 256 KiB con lectura controlada.
3. **Given** un archivo ausente, reemplazado, ilegible o fuera del directorio, **When** se intenta leer, **Then** se devuelve un estado seguro sin cerrar la aplicación.
4. **Given** que la carpeta administrada existe, **When** se elige localizar logs, **Then** se abre exactamente esa carpeta; nunca se acepta una ruta proporcionada por el usuario.

### Edge Cases

- Identificador de hardware no disponible, vacío o excesivamente largo.
- Cliente predeterminado no configurado, navegador bloqueado o inicio de proceso rechazado.
- Caracteres Unicode, saltos de línea y símbolos reservados en asunto y cuerpo.
- Doble activación, respuestas tardías, cancelación y cierre del módulo durante una operación.
- Directorio de logs ausente, vacío, con enlaces simbólicos, archivos rotados o archivo que cambia durante la lectura.
- Logs mayores al límite, líneas extremadamente largas o texto con codificación inválida.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST ofrecer un módulo habilitado de “Soporte y logs” desde la administración de la shell.
- **FR-002**: El módulo MUST obtener el identificador de hardware mediante el servicio existente y MUST mostrar un estado explícito cuando no esté disponible.
- **FR-003**: El operador MUST poder copiar el identificador únicamente mediante una acción explícita.
- **FR-004**: El sistema MUST preparar un borrador de solicitud de licencia con asunto, identificador y versión de aplicación.
- **FR-005**: El operador MUST poder preparar un reporte de error con resumen requerido de 1..120 caracteres y detalle requerido de 1..4000 caracteres.
- **FR-006**: El reporte MUST incluir versión y un código de diagnóstico opaco; MUST NOT incluir automáticamente credenciales, configuración, rutas privadas, licencia, UUID completo en reportes de error, producto, barcode, precio ni documentos.
- **FR-007**: Los borradores MUST admitir cliente predeterminado, Gmail web y Outlook web.
- **FR-008**: Abrir un canal MUST crear exactamente una solicitud de apertura y MUST NOT enviar el mensaje automáticamente.
- **FR-009**: Los campos del borrador MUST codificarse de forma que Unicode, espacios, saltos y símbolos reservados se preserven.
- **FR-010**: Un fallo al abrir el canal MUST producir un resultado tipado y un mensaje recuperable.
- **FR-011**: El sistema MUST impedir operaciones de apertura concurrentes y MUST ignorar resultados tardíos después de cerrar o invalidar el módulo.
- **FR-012**: El sistema MUST registrar solo canal, tipo de solicitud, estado, duración y código opaco; MUST NOT registrar asunto, cuerpo, UUID, contenido del log, ruta o excepción adjunta.
- **FR-013**: El sistema MUST localizar los logs únicamente bajo el directorio administrado de SysTools del perfil local.
- **FR-014**: La lista MUST incluir como máximo los 20 archivos `.log` más recientes, ordenados por fecha descendente, y exponer solo nombre simple, fecha UTC y tamaño.
- **FR-015**: La vista previa MUST limitarse a las últimas 500 líneas y 256 KiB por archivo.
- **FR-016**: El lector MUST rechazar traversal, subdirectorios, extensiones distintas, enlaces que escapen del directorio y archivos mayores a 50 MiB.
- **FR-017**: La vista MUST distinguir directorio ausente, vacío, archivo ausente, acceso denegado, contenido inválido y lectura exitosa.
- **FR-018**: La acción “Abrir carpeta” MUST usar solamente el directorio administrado y MUST crearla si no existe.
- **FR-019**: Ninguna función de esta feature MUST modificar, eliminar, comprimir, adjuntar ni transmitir archivos de log.
- **FR-020**: El módulo MUST permitir refrescar la lista sin bloquear la interfaz y conservar selección solo si el archivo continúa disponible.
- **FR-021**: El módulo MUST ser operable por teclado, exponer nombres accesibles y mostrar estados mediante texto además de color.
- **FR-022**: El contenido MUST seguir siendo utilizable a 1280×720 con escala Windows de 125% mediante scroll y áreas acotadas.
- **FR-023**: Presentation MUST limitarse a bindings y coordinación visual; composición de borradores, catálogo y lectura MUST residir detrás de contratos de Business.
- **FR-024**: Data MUST ser la única capa que acceda al sistema de archivos o inicie navegador, correo o explorador.
- **FR-025**: Los resultados y colecciones MUST ser inmutables o de solo lectura y MUST validar sus invariantes.
- **FR-026**: Cerrar el módulo MUST cancelar lecturas pendientes y MUST NOT cerrar procesos externos ya iniciados.
- **FR-027**: El módulo MUST reutilizar el servicio de hardware existente y la infraestructura de logging vigente.
- **FR-028**: El destinatario de soporte MUST permanecer vacío cuando no exista una dirección aprobada, permitiendo que el operador la capture en el cliente.
- **FR-029**: La feature MUST NOT implementar SMTP, OAuth, credenciales de correo, envío silencioso ni carga remota de logs.
- **FR-030**: Todos los fallos recuperables MUST conservar la aplicación abierta y ofrecer reintento explícito cuando aplique.

### Key Entities

- **SupportMailDraft**: Borrador validado con tipo, destinatario opcional, asunto, cuerpo y código opaco.
- **SupportOperationResult**: Resultado tipado de preparación o apertura sin incluir contenido sensible.
- **LogFileSummary**: Metadatos seguros de un archivo administrado, sin ruta completa.
- **LogCatalogResult**: Estado y colección de archivos recientes de solo lectura.
- **LogPreviewResult**: Estado, nombre simple, contenido acotado y señal de truncamiento.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un operador puede preparar una solicitud de licencia y abrir cualquiera de los tres canales en menos de 30 segundos.
- **SC-002**: Los tres canales conservan correctamente 20 casos con Unicode, espacios, saltos de línea y símbolos reservados.
- **SC-003**: En 100 operaciones por canal se produce como máximo una apertura por activación y ninguna apertura automática adicional.
- **SC-004**: 100 reportes con valores centinela no exponen credenciales, rutas, licencia, UUID completo ni datos comerciales en logs internos.
- **SC-005**: Catálogos de 0, 1, 20 y más de 20 archivos devuelven resultados correctos en menos de 500 ms en el entorno objetivo.
- **SC-006**: Una vista previa de log nunca supera 500 líneas ni 256 KiB y no bloquea la interfaz por más de 2 segundos.
- **SC-007**: 50 intentos con traversal, subdirectorios, enlaces o extensiones inválidas producen cero lecturas fuera del directorio administrado.
- **SC-008**: Todos los estados de ausencia, vacío, acceso denegado, archivo cambiado, apertura fallida y cancelación muestran mensajes controlados y cero cierres abruptos.
- **SC-009**: El flujo completo puede operarse por teclado y permanece legible a 1280×720 con escala de 125%.
- **SC-010**: Compilación y regresión completa terminan con cero errores, cero advertencias y cero fallos deterministas.

## Assumptions

- No existe todavía una dirección de soporte aprobada; el destinatario queda vacío hasta una decisión operativa posterior.
- Gmail y Outlook se abren mediante sus experiencias web; el cliente predeterminado usa la asociación del sistema.
- El usuario revisa y envía el correo; SysTools nunca autentica ni envía mensajes.
- Los logs actuales ya aplican políticas de logging seguro, pero la vista sigue acotando rutas y volumen.
- La carpeta administrada continúa siendo `%LocalAppData%\SysTools\Logs`.
- Esta feature depende de las etapas 001, 002, 006 y de la infraestructura de logging existente.
