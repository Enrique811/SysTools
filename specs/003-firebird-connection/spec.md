# Feature Specification: Conexión y prueba Firebird

**Feature Branch**: `003-firebird-connection`

**Created**: 2026-09-23

**Status**: Implemented — operational validation pending

**Input**: User description: "Implementar conexión Firebird usando FirebirdSql.Data.FirebirdClient, con fábrica de conexiones, prueba de conectividad, timeout, logging de errores y compatibilidad con charset ISO8859_1 usado por el sistema Java. La conexión debe construirse desde la configuración local protegida y no debe exponer credenciales."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Confirmar una configuración válida (Priority: P1)

Como operador que necesita preparar SysTools, quiero comprobar que los datos configurados permiten abrir la base de datos de la empresa, para saber que las funciones posteriores podrán trabajar con Firebird.

**Why this priority**: Una conexión real y verificable es el prerrequisito para consultar productos, obtener la fecha del servidor y validar licencias.

**Independent Test**: Con un servidor Firebird controlado y una configuración válida, se solicita una prueba de conectividad y se comprueba que devuelve éxito, identifica la duración del intento y deja cerrados los recursos utilizados.

**Acceptance Scenarios**:

1. **Given** host, base, usuario y password válidos, **When** un consumidor solicita probar la conexión, **Then** obtiene un resultado exitoso dentro del tiempo permitido y la conexión de prueba queda cerrada.
2. **Given** una configuración guardada cuyo password puede recuperarse para el usuario actual, **When** se prepara una conexión, **Then** se emplean todos los datos funcionales requeridos sin volver a solicitar la credencial.
3. **Given** varias pruebas consecutivas con la misma configuración válida, **When** finalizan, **Then** cada prueba es independiente y ninguna conserva una conexión abierta global.

---

### User Story 2 - Entender por qué no fue posible conectar (Priority: P2)

Como operador, quiero recibir un resultado claro cuando la conexión falla, para distinguir datos incompletos, credenciales rechazadas, servidor inaccesible, base no disponible o espera agotada sin ver detalles técnicos confusos.

**Why this priority**: Una respuesta controlada reduce soporte y evita que un problema recuperable cierre la aplicación o parezca un éxito.

**Independent Test**: Se ejecutan intentos con configuración incompleta, credencial inválida, host inaccesible y base inexistente; cada caso retorna la categoría y mensaje esperados, registra diagnóstico y mantiene operativa la shell.

**Acceptance Scenarios**:

1. **Given** faltan uno o más datos requeridos, **When** se solicita probar la conexión, **Then** el sistema rechaza el intento antes de contactar al servidor y enumera todos los campos faltantes.
2. **Given** el servidor responde pero rechaza las credenciales, **When** termina el intento, **Then** el resultado indica autenticación fallida sin revelar usuario o password.
3. **Given** el host no responde dentro del límite, **When** expira la espera, **Then** el resultado indica timeout y la aplicación continúa disponible.
4. **Given** el servidor está disponible pero la base indicada no puede abrirse, **When** termina el intento, **Then** el resultado diferencia la base no disponible de una autenticación fallida cuando Firebird proporciona información suficiente.
5. **Given** ocurre un fallo no reconocido, **When** se traduce el resultado, **Then** el operador recibe un mensaje genérico controlado y soporte conserva diagnóstico técnico seguro.

---

### User Story 3 - Cancelar y diagnosticar sin exponer secretos (Priority: P3)

Como consumidor de la capacidad de conexión, quiero cancelar un intento innecesario y consultar un resultado seguro, para no mantener esperas o recursos después de abandonar la operación.

**Why this priority**: La cancelación, el cierre determinista y el logging seguro permiten integrar esta capacidad posteriormente en una UI responsiva.

**Independent Test**: Se inicia una conexión controlada que permanece pendiente, se cancela y se verifica que el resultado sea `Canceled`, que el recurso se cierre y que mensajes y logs no contengan secretos ni la cadena completa.

**Acceptance Scenarios**:

1. **Given** un intento aún no completado, **When** el consumidor lo cancela, **Then** finaliza como cancelado y no deja una conexión abierta.
2. **Given** cualquier resultado de conexión, **When** se revisan mensajes y logs, **Then** no aparecen password, cadena completa, licencia ni configuración serializada.
3. **Given** dos solicitudes cercanas o concurrentes, **When** una falla o se cancela, **Then** no modifica el resultado ni el ciclo de vida de la otra.

### Edge Cases

- La configuración no existe, está corrupta o el password protegido no puede recuperarse; estos fallos pertenecen al flujo de carga segura de la feature 002 y se resuelven antes de invocar `IConnectionTestService`.
- Host, ruta, usuario o password son vacíos o contienen solo espacios.
- El host es un nombre DNS, IPv4 o IPv6 y no puede resolverse.
- La ruta de base contiene espacios, acentos, separadores Windows o un alias de Firebird.
- El servidor responde, pero la base está apagada, bloqueada, movida o requiere una versión incompatible.
- Firebird rechaza usuario/password o limita conexiones.
- La red cae antes, durante o inmediatamente después de abrir la conexión.
- La cancelación y el timeout ocurren casi simultáneamente.
- El driver devuelve un código conocido con un mensaje localizado diferente.
- El logging recibe una excepción cuyo texto incluye accidentalmente parte de la cadena de conexión.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: SysTools MUST ofrecer una capacidad reutilizable para crear conexiones Firebird a partir de una configuración proporcionada, sin que Presentation construya cadenas ni use el cliente de datos directamente.
- **FR-002**: La conexión MUST usar `ipEmpresa`, `rutaEmpresa`, `usuario` y `password` como host, base, usuario y credencial respectivamente.
- **FR-003**: La conexión MUST usar el juego de caracteres `ISO8859_1` requerido por la base heredada y MUST mantenerlo como parte explícita de su configuración.
- **FR-004**: El puerto inicial MUST ser el puerto Firebird estándar `3050`; agregar configuración visual o persistida de puerto queda fuera de esta feature.
- **FR-005**: Antes de contactar Firebird, el sistema MUST validar en una sola evaluación que host, base, usuario y password sean no vacíos después de retirar espacios exteriores.
- **FR-006**: El sistema MUST permitir probar una configuración proporcionada sin exigir que primero se guarde, para soportar una pantalla de configuración posterior.
- **FR-007**: Una prueba de conectividad MUST abrir una conexión y cerrarla al terminar, tanto en éxito como en fallo o cancelación.
- **FR-008**: Cada conexión solicitada MUST ser una instancia independiente; esta feature MUST NOT mantener una conexión global estática ni compartir una instancia abierta entre consumidores.
- **FR-009**: El intento MUST aplicar un timeout de conexión de 5 segundos y MUST aceptar cancelación del consumidor.
- **FR-010**: El resultado MUST distinguir al menos `Success`, `InvalidConfiguration`, `AuthenticationFailed`, `ServerUnavailable`, `DatabaseUnavailable`, `Timeout`, `Canceled` y `UnexpectedFailure`.
- **FR-011**: Cada resultado MUST incluir una categoría estable, un mensaje seguro para el operador y la duración del intento; MUST NOT entregar stack trace ni excepción técnica como mensaje.
- **FR-012**: Cuando Firebird no permita diferenciar de forma confiable autenticación, servidor o base, el sistema MUST usar una categoría más general en lugar de afirmar una causa incorrecta.
- **FR-013**: Inicio, éxito, timeout, cancelación y fallo MUST producir eventos estructurados suficientes para soporte, con categoría, duración y códigos técnicos seguros cuando existan.
- **FR-014**: Mensajes, resultados y logs MUST NOT incluir password, cadena de conexión completa, licencia, configuración serializada ni valores protegidos.
- **FR-015**: La capacidad MUST consumir el password solamente en memoria y MUST NOT crear otra copia persistida de las credenciales.
- **FR-016**: Un fallo o cancelación de conexión MUST ser recuperable y MUST NOT cerrar la shell ni alterar la configuración guardada.
- **FR-017**: La fábrica de conexiones MUST quedar disponible para repositorios posteriores sin exponer el cliente Firebird a Business, Entities, Views o ViewModels.
- **FR-018**: Esta feature MUST NOT consultar productos, obtener la fecha del servidor, validar licencias, habilitar controles de conexión ni modificar estados visuales de la shell.

### Technical and Organizational Constraints

- El cliente aprobado por la constitución es `FirebirdSql.Data.FirebirdClient`; no se introducirá ORM.
- Business dependerá de abstracciones y Data implementará la construcción/apertura Firebird y la traducción técnica de errores.
- Entities contendrá resultados y categorías sin referenciar tipos del driver.
- Presentation se limitará al composition root; no se agregará UI en esta feature.
- Las pruebas unitarias usarán dobles controlados; la validación de integración usará una base Firebird dedicada y credenciales suministradas fuera del repositorio.

### Key Entities

- **ConnectionTestResult**: Resultado seguro de un intento. Contiene estado, mensaje para el operador y duración; no contiene la conexión, la configuración ni una excepción destinada a UI.
- **ConnectionTestStatus**: Catálogo estable de resultados: éxito, configuración inválida, autenticación fallida, servidor no disponible, base no disponible, timeout, cancelación o fallo inesperado.
- **ConnectionConfigurationIssue**: Campo requerido ausente o inválido antes de intentar la conexión; identifica el campo sin copiar su valor.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Con una base controlada disponible y datos válidos, 10 de 10 pruebas consecutivas terminan como exitosas, cada una dentro de 6 segundos y sin recursos abiertos después del resultado.
- **SC-002**: El 100% de los escenarios definidos de configuración incompleta, credenciales rechazadas, servidor inaccesible, base no disponible, timeout y cancelación devuelve un resultado controlado y no cierra la shell.
- **SC-003**: En 100 intentos automatizados de éxito, fallo y cancelación mediante dobles controlados, el 100% de las conexiones creadas queda cerrado o desechado exactamente una vez.
- **SC-004**: Ninguna búsqueda de los valores centinela usados como password, licencia o cadena completa encuentra coincidencias en mensajes, resultados o logs de todos los escenarios automatizados.
- **SC-005**: En el 100% de las configuraciones inválidas de prueba, se informan conjuntamente todos los campos faltantes y se crean cero conexiones.
- **SC-006**: Una revisión arquitectónica confirma que el 100% de los tipos del cliente Firebird queda en Data o en el composition root autorizado, sin referencias desde Entities, Views o ViewModels.
- **SC-007**: Una auditoría de alcance confirma que no se agregaron consultas de productos/fecha, licencia funcional ni controles visuales de configuración.

## Assumptions

- Firebird escucha inicialmente en el puerto estándar 3050; soporte para otro puerto requerirá ampliar la configuración en una feature posterior.
- `rutaEmpresa` contiene una ruta o alias aceptado por el servidor Firebird; SysTools no comprueba su existencia en el sistema de archivos local.
- El timeout de 5 segundos conserva el comportamiento conocido del sistema Java y es suficiente para una prueba interactiva inicial.
- Las categorías específicas de error solo se usarán cuando el driver entregue códigos confiables; mensajes localizados no serán la única base de clasificación.
- El pooling interno del cliente puede permanecer habilitado, pero cada objeto de conexión creado por SysTools se cierra/desecha; no existe una conexión global.
- La aplicación objetivo es Windows y la configuración local segura de la feature 002 ya está disponible.
- Las credenciales y la dirección del entorno Firebird real se proporcionarán fuera del código y de Git. La suite puede omitir esa integración cuando el entorno no exista, pero la feature no se considerará validada operativamente hasta documentar al menos una ejecución real exitosa y los fallos controlados disponibles.

## Dependencies

- Feature `002-local-configuration-secrets` para `AppConfiguration`, validación de readiness y recuperación segura del password.
- Acceso de red a un servidor Firebird compatible para validación de integración.
- Una base de datos y credenciales no productivas autorizadas para las pruebas reales.
- Constitución vigente de SysTools, logging estructurado y composition root existentes.

## Out of Scope

- Pantallas, botones o comandos WPF para editar o probar conexión.
- Persistir un puerto distinto, SSL/TLS adicional, túneles SSH o configuraciones avanzadas del servidor.
- Mantener una conexión global abierta o implementar reintentos automáticos.
- Consultar productos, existencias, precios o fecha/hora del servidor.
- Validar licencias o usar la conexión para decisiones de licenciamiento.
- Crear, reparar, respaldar o migrar bases Firebird.
- Importar configuración Java o cambiar el formato JSON de la feature 002.
