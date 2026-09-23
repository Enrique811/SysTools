# Feature Specification: Configuración local y secretos

**Feature Branch**: `002-local-configuration-secrets`

**Created**: 2026-09-23

**Status**: Draft

**Input**: User description: "Implementar configuración local de SysUtilerias guardada como JSON en AppData, con entidad AppConfiguration, repositorio de configuración, servicio de configuración, valores por defecto, validación de campos requeridos y protección del password de Firebird usando DPAPI. La configuración debe incluir ipEmpresa, rutaEmpresa, usuario, password, ambiente, impresora, formatoPrecio, reporte, columnas, informacion y licencia."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Iniciar con una configuración local segura (Priority: P1)

Como operador que inicia SysTools por primera vez, necesito que la aplicación disponga de una configuración local base para que los módulos posteriores puedan reconocer qué datos faltan sin depender del sistema Java ni detener la shell.

**Why this priority**: La configuración es la base para conexión, productos, licenciamiento e impresión. Sin una lectura predecible y segura, ninguna de esas etapas puede construirse de forma confiable.

**Independent Test**: Se ejecuta la capacidad de carga con un perfil de Windows que no tenga configuración previa y se comprueba que se obtiene una configuración base completa, que el archivo local se crea en la ubicación esperada y que la ausencia de datos de conexión se informa como un estado controlado.

**Acceptance Scenarios**:

1. **Given** el usuario actual no tiene archivo de configuración, **When** SysTools solicita la configuración, **Then** se crea y devuelve una configuración base con todos los campos conocidos y sin credenciales en texto plano.
2. **Given** se creó la configuración base, **When** se valida su aptitud para una conexión futura, **Then** se identifican de forma conjunta los campos obligatorios aún vacíos sin intentar conectarse a Firebird.
3. **Given** ya existe una configuración legible del usuario actual, **When** SysTools la solicita, **Then** se devuelve su contenido en lugar de reemplazarlo por valores base.

---

### User Story 2 - Guardar y recuperar preferencias completas (Priority: P2)

Como operador o módulo autorizado de SysTools, necesito guardar la configuración de conexión y operación para recuperarla en los siguientes inicios sin volver a capturarla.

**Why this priority**: Persistir todos los valores heredados permite que las siguientes etapas consuman una única fuente de configuración y elimina la dependencia del archivo `configuracion.properties` de Java.

**Independent Test**: Se guarda una configuración válida con valores distintivos en los once campos, se inicia una nueva sesión de lectura y se verifica que todos los valores recuperados coincidan, incluido el secreto disponible para el consumidor autorizado pero ausente en texto plano del almacenamiento.

**Acceptance Scenarios**:

1. **Given** una configuración válida con datos de conexión y preferencias, **When** se guarda y vuelve a cargar, **Then** los once campos conservan exactamente sus valores funcionales.
2. **Given** una contraseña de Firebird, **When** se persiste la configuración, **Then** la contraseña no aparece en texto plano ni como simple codificación reversible en el archivo, temporales o registros de diagnóstico.
3. **Given** una configuración protegida por el usuario actual de Windows, **When** ese mismo usuario la vuelve a cargar en el mismo equipo, **Then** SysTools recupera el secreto para uso interno sin solicitarlo nuevamente.
4. **Given** una configuración existente válida, **When** se guarda una modificación válida, **Then** el nuevo conjunto completo sustituye al anterior sin producir un estado parcial.

---

### User Story 3 - Detectar errores sin perder una configuración válida (Priority: P3)

Como operador, necesito que SysTools detecte configuraciones incompletas, inválidas o ilegibles y las reporte de forma comprensible, para poder corregirlas en una etapa posterior sin perder datos ni exponer secretos.

**Why this priority**: Los fallos de archivo, validación o protección del secreto son recuperables y no deben provocar cierres abruptos, sobrescrituras destructivas ni diagnósticos inseguros.

**Independent Test**: Se prueban configuraciones con campos faltantes, opciones no permitidas, contenido malformado, secreto no descifrable y fallo de escritura; en cada caso se verifica el resultado controlado, la preservación del último archivo válido y la ausencia de valores sensibles en mensajes y logs.

**Acceptance Scenarios**:

1. **Given** una configuración con varios campos requeridos vacíos o valores fuera de catálogo, **When** se valida, **Then** se devuelven en una sola respuesta todos los problemas detectados y no se guarda como configuración válida.
2. **Given** un archivo con contenido malformado, **When** se intenta cargar, **Then** SysTools devuelve un fallo recuperable, conserva el archivo para diagnóstico y no lo reemplaza silenciosamente.
3. **Given** un secreto protegido que el usuario actual no puede recuperar, **When** se carga la configuración, **Then** se informa que la credencial debe capturarse nuevamente sin mostrar el secreto protegido ni detalles criptográficos al operador.
4. **Given** una configuración válida ya persistida, **When** falla una actualización por permisos, espacio o interrupción de escritura, **Then** la versión válida anterior sigue disponible y el error queda registrado sin secretos.

### Edge Cases

- El directorio o archivo no existe en el primer inicio.
- El directorio existe, pero el usuario no tiene permiso de lectura o escritura.
- El archivo está vacío, truncado, contiene JSON malformado o campos desconocidos de una versión posterior.
- Faltan campos conocidos o aparecen valores nulos, vacíos o compuestos solo por espacios.
- `ambiente`, `formatoPrecio` o `columnas` contienen valores fuera de los catálogos admitidos.
- La ruta de empresa, la impresora, el reporte o la información adicional contienen espacios, acentos, caracteres especiales o texto largo.
- La protección del secreto pertenece a otro usuario o equipo, o el contenido protegido fue alterado.
- La aplicación se interrumpe mientras actualiza el archivo.
- Dos solicitudes del mismo proceso intentan leer o guardar configuración de forma cercana.
- Un error o registro de diagnóstico intenta incluir el objeto completo de configuración.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: SysTools MUST mantener una configuración local independiente de los archivos del sistema Java anterior.
- **FR-002**: La configuración MUST representar `ipEmpresa`, `rutaEmpresa`, `usuario`, `password`, `ambiente`, `impresora`, `formatoPrecio`, `reporte`, `columnas`, `informacion` y `licencia`.
- **FR-003**: El sistema MUST cargar la configuración del usuario actual y MUST crear una configuración base cuando todavía no exista.
- **FR-004**: La configuración base MUST usar `SYSDBA` como usuario, `a` como ambiente seguro de pruebas, `MX` como formato de precio y `1` como número de columnas; los datos de servidor, ruta, password, impresora, reporte, información y licencia MUST iniciar vacíos.
- **FR-005**: El sistema MUST distinguir entre una configuración estructuralmente legible y una configuración apta para conexión; crear valores base MUST NOT comunicar que la conexión está lista.
- **FR-006**: Para considerar una configuración apta para conexión, `ipEmpresa`, `rutaEmpresa`, `usuario` y `password` MUST contener valores no vacíos después de retirar espacios exteriores.
- **FR-007**: `ambiente` MUST admitir únicamente `a` o `b`, `formatoPrecio` MUST admitir únicamente `CO` o `MX`, y `columnas` MUST admitir únicamente `1`, `2` o `3`.
- **FR-008**: La validación MUST devolver todos los incumplimientos detectados en una sola evaluación, identificando cada campo sin incluir su valor sensible.
- **FR-009**: El sistema MUST rechazar el guardado de una configuración inválida y MUST conservar la última configuración válida persistida.
- **FR-010**: El sistema MUST guardar y recuperar los once campos sin pérdida de contenido, incluidos espacios internos, acentos y caracteres especiales válidos.
- **FR-011**: El password de Firebird MUST persistirse protegido para el usuario actual de Windows y MUST NOT aparecer en texto plano ni como Base64 recuperable directamente.
- **FR-012**: Los consumidores autorizados MUST recibir el password recuperado en memoria solo cuando la protección pueda abrirse correctamente para el usuario actual.
- **FR-013**: Un fallo al recuperar el password MUST producir un resultado controlado que requiera recapturar la credencial; MUST NOT borrar, sustituir ni revelar el contenido protegido.
- **FR-014**: El guardado MUST evitar exponer una configuración parcial y, ante un fallo, MUST preservar la última versión válida disponible.
- **FR-015**: Un archivo ausente MUST tratarse como primer uso; un archivo presente pero malformado o ilegible MUST tratarse como error recuperable y MUST NOT reemplazarse silenciosamente por valores base.
- **FR-016**: Los campos desconocidos de una versión posterior MUST NOT impedir la lectura de los campos conocidos, siempre que estos sean legibles y válidos.
- **FR-017**: Las operaciones de creación, carga, guardado, validación fallida y error MUST producir diagnóstico estructurado suficiente para soporte.
- **FR-018**: Mensajes, excepciones traducidas y registros MUST NOT incluir el password, el valor de licencia, el contenido protegido ni una serialización completa de la configuración.
- **FR-019**: Un fallo recuperable de configuración MUST NOT cerrar abruptamente la shell ni presentarse como éxito.
- **FR-020**: Esta feature MUST ofrecer una única capacidad de negocio para consultar, validar y actualizar configuración, manteniendo el acceso a archivos y protección del secreto fuera de Presentation.
- **FR-021**: La configuración y sus resultados de validación MUST permanecer independientes de componentes visuales, conexiones de base de datos, impresoras y motores de reportes.
- **FR-022**: Esta feature MUST NOT abrir conexiones Firebird, probar credenciales, validar licencias, enumerar impresoras, cargar reportes ni habilitar las pantallas de configuración.

### Technical and Organizational Constraints

- La configuración se guardará como JSON bajo `%APPDATA%\SysUtilerias\configuracion.json`, de acuerdo con la constitución y el roadmap aprobados.
- La protección del password usará DPAPI con alcance del usuario actual de Windows; no se aceptará Base64 como mecanismo de protección.
- Presentation solo podrá consumir la capacidad coordinada por Business; Data concentrará archivos y protección del secreto, y Entities contendrá los datos y resultados compartidos sin depender de UI ni infraestructura.
- Los eventos de diagnóstico usarán el logging estructurado existente y aplicarán exclusión explícita de secretos.
- La feature se validará en Windows con perfiles aislados y ubicaciones temporales controladas para no modificar la configuración real del operador durante pruebas.

### Key Entities

- **AppConfiguration**: Conjunto completo de datos locales de SysTools. Contiene host y ruta de empresa, usuario y secreto de conexión, ambiente, impresora, formato de precio, reporte, columnas, información adicional y licencia opaca.
- **ConfigurationValidationResult**: Resultado de evaluar una configuración. Indica si es válida para persistirse y si está lista para una conexión futura, junto con la colección de problemas identificados por campo.
- **ConfigurationIssue**: Problema individual de validación o recuperación, con campo, código y mensaje seguro para el consumidor, sin copiar valores sensibles.
- **StoredConfiguration**: Representación persistida que conserva las preferencias legibles y el password protegido; su forma evita confundir el contenido protegido con el secreto utilizable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En el 100% de 20 ciclos de guardado y recarga con combinaciones válidas, los once campos recuperados conservan sus valores funcionales, incluidos acentos y caracteres especiales.
- **SC-002**: En el 100% de los casos de prueba con password no vacío, ninguna búsqueda del valor original lo encuentra en el archivo de configuración, archivos temporales producidos por la operación, mensajes al operador ni logs.
- **SC-003**: Con un perfil sin configuración previa, SysTools obtiene una configuración base completa en menos de 1 segundo en al menos 95 de 100 ejecuciones locales, sin bloquear ni cerrar la shell.
- **SC-004**: El 100% de los casos de validación con múltiples errores devuelve en una sola evaluación todos los campos inválidos esperados y no altera la configuración previamente guardada.
- **SC-005**: En todos los escenarios definidos de archivo malformado, secreto no recuperable y fallo de escritura, el resultado es controlado, no expone secretos y conserva el último archivo válido o el archivo problemático para diagnóstico.
- **SC-006**: Una revisión de alcance confirma que ninguna prueba de conexión, validación de licencia, enumeración de impresoras, carga de reportes o pantalla de configuración fue implementada en esta feature.
- **SC-007**: El 100% de los consumidores de prueba puede cargar, validar y guardar configuración sin conocer la ubicación del archivo ni manipular directamente el mecanismo de protección del secreto.

## Assumptions

- SysTools se ejecuta bajo Windows y cada usuario del sistema operativo mantiene su propia configuración.
- El ambiente `a` es el valor inicial más seguro porque no representa operación productiva; `MX` y una columna continúan los valores base del proyecto mientras no exista una configuración guardada.
- El password inicial queda vacío; no se adoptará `masterkey` como secreto predeterminado para evitar persistir o promover una credencial conocida.
- La licencia se conserva como dato opaco en esta etapa; su importación, estructura y validez se definirán en la feature de licenciamiento.
- Los campos `impresora`, `reporte`, `informacion` y `licencia` pueden estar vacíos sin impedir guardar la configuración.
- Esta etapa proporciona la capacidad interna y sus contratos; la captura y edición visual se implementarán en la etapa de configuración visual.
- No se realizará importación automática de `configuracion.properties`; la compatibilidad se limita a representar los campos funcionales necesarios sin depender del archivo anterior.

## Dependencies

- La base de arquitectura, composición y logging entregada por `001-systools-shell-base`.
- Un perfil de usuario de Windows capaz de proporcionar almacenamiento local y protección de datos vinculada al usuario.
- La constitución vigente de SysTools y el orden incremental del roadmap de migración.

## Out of Scope

- Pantallas o controles para capturar, editar o mostrar configuración.
- Migración o importación automática desde archivos Java `.properties`.
- Construcción de cadenas, apertura o prueba de conexiones Firebird.
- Interpretación, importación, firma o validación de licencias.
- Detección de UUID, impresoras instaladas, existencia de reportes o validez de rutas externas.
- Impresión, vista previa, productos, precios y códigos de barras.
- Sincronización de configuración entre usuarios, equipos o servicios remotos.
