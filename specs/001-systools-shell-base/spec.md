# Feature Specification: Base de shell modular de SysTools

**Feature Branch**: `001-systools-shell-base`

**Created**: 2026-09-20

**Status**: Draft

**Input**: User description: "Crear la base de SysTools como aplicación Windows Desktop en C# con .NET 10 LTS y WPF, arquitectura de 3 capas + Entities, shell modular con navegación lateral y Verificador de precios como módulo inicial placeholder, siguiendo `docs/mockup-shell-utilerias.svg`, MVVM, inyección de dependencias y logging, sin implementar todavía integraciones ni funciones operativas."

## Clarifications

### Session 2026-09-20

- Q: ¿Cuál debe ser el tamaño mínimo de ventana en el que toda la shell permanezca utilizable sin superposiciones ni contenido esencial cortado? → A: 1280 × 720.
- Q: ¿Cómo deben presentarse en esta feature los controles de Buscar, Imprimir y Configuración que todavía no tienen funcionalidad? → A: Visibles y deshabilitados.
- Q: ¿Qué opciones debe mostrar el menú lateral además de Verificador de precios durante esta primera feature? → A: Mostrar las utilerías futuras del mockup visibles y deshabilitadas.
- Q: ¿El campo de código de barras debe permitir escribir durante esta feature aunque todavía no realice ninguna consulta? → A: No; debe permanecer visible y deshabilitado.
- Q: ¿El área de información adicional también debe permanecer deshabilitada durante esta feature? → A: Sí; debe permanecer visible y deshabilitada.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Abrir la suite en el verificador (Priority: P1)

Como operador, quiero que SysTools abra directamente en una shell funcional con el módulo Verificador de precios activo para reconocer de inmediato el área de trabajo y comenzar mi jornada sin atravesar una pantalla de bienvenida.

**Why this priority**: Establece la experiencia mínima utilizable y el punto de entrada sobre el que se incorporarán las demás capacidades de la suite.

**Independent Test**: Se inicia la aplicación desde un entorno limpio y se verifica que la primera ventana visible sea la shell de SysTools, con navegación lateral, área central y el Verificador de precios seleccionado y cargado.

**Acceptance Scenarios**:

1. **Given** que la aplicación no está abierta, **When** el operador la inicia, **Then** se muestra una única shell principal de SysTools con el Verificador de precios como módulo activo.
2. **Given** que la shell acaba de abrir, **When** el operador observa la pantalla inicial, **Then** no aparece una landing page, bienvenida decorativa ni ventana intermedia.
3. **Given** que el Verificador de precios está activo, **When** el operador revisa la navegación, **Then** su opción aparece visualmente seleccionada y su contenido ocupa el área central.
4. **Given** que ninguna otra utilería está implementada, **When** el operador revisa la navegación, **Then** las utilerías futuras del mockup aparecen visibles pero deshabilitadas y Verificador de precios es la única opción seleccionable.

---

### User Story 2 - Reconocer la estructura del verificador (Priority: P2)

Como operador, quiero visualizar la distribución prevista del Verificador de precios para familiarizarme con dónde capturaré códigos y dónde aparecerán la información, el precio y los mensajes cuando esas funciones se implementen.

**Why this priority**: Permite validar temprano el flujo visual del módulo sin mezclarlo con conexiones, reglas de negocio o funciones aún fuera de alcance.

**Independent Test**: Con el módulo inicial abierto, se comprueba visualmente la presencia de cada zona solicitada y que ninguna simule una consulta real.

**Acceptance Scenarios**:

1. **Given** que el Verificador de precios está visible, **When** el operador recorre su contenido, **Then** encuentra encabezado, campo visual de código de barras, área de información adicional, panel de detalles, panel lateral de precio final y zona inferior de estado o mensajes.
2. **Given** que todavía no existe consulta de productos, **When** se muestra el módulo, **Then** las áreas de producto y precio presentan valores neutros o vacíos y no información ficticia que pueda confundirse con un resultado real.
3. **Given** que las funciones de búsqueda, impresión y configuración todavía no existen, **When** el operador observa sus controles placeholder, **Then** aparecen visibles pero deshabilitados, no aceptan interacción y la aplicación permanece estable.
4. **Given** que la consulta de productos está fuera del alcance, **When** el operador observa el campo de código de barras, **Then** aparece visible pero deshabilitado y no admite foco, escritura ni envío.
5. **Given** que la captura de datos está fuera del alcance, **When** el operador observa el área de información adicional, **Then** aparece visible pero deshabilitada y no admite foco ni escritura.

---

### User Story 3 - Identificar estados operativos sin funciones simuladas (Priority: P3)

Como operador o personal de soporte, quiero distinguir los espacios reservados para conexión, licencia y mensajes para entender el estado general de la suite sin que el placeholder afirme que servicios aún no implementados están disponibles.

**Why this priority**: Define un lenguaje visual consistente para estados futuros y evita comunicar disponibilidad falsa en esta primera base.

**Independent Test**: Se abre la shell sin servicios externos configurados y se verifica que los indicadores de estado sean visibles, distinguibles y expresen un estado neutral, pendiente o no disponible.

**Acceptance Scenarios**:

1. **Given** que conexión y licenciamiento están fuera del alcance, **When** se abre la shell, **Then** sus indicadores no muestran un estado exitoso ni un dato real no comprobado.
2. **Given** que la aplicación está visible, **When** se presenta un mensaje de estado, **Then** el mensaje es legible, controlado y no contiene detalles técnicos internos.
3. **Given** una ventana con el tamaño mínimo soportado, **When** se visualizan los estados, **Then** permanecen identificables y no cubren el contenido principal.

### Edge Cases

- Si la shell se abre sin un módulo previamente seleccionado, debe recuperar el Verificador de precios como selección inicial segura.
- Si un módulo futuro no puede cargarse, la shell debe permanecer abierta y reservar la zona de mensajes para una explicación controlada, sin mostrar detalles técnicos ni cerrarse abruptamente.
- En el tamaño mínimo soportado, las zonas principales deben seguir siendo visibles y utilizables sin superposiciones; al ampliar la ventana, el área central debe aprovechar el espacio adicional.
- Los textos extensos de estado o del módulo deben truncarse, ajustarse o desplazarse sin romper la composición visual.
- Los controles que representan capacidades futuras deben permanecer inactivos o comunicar claramente que aún no están disponibles, sin producir efectos laterales.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La aplicación MUST iniciar mostrando la shell principal de SysTools como primera y única experiencia de entrada.
- **FR-002**: La shell MUST mostrar una navegación lateral oscura y un área central clara destinada a cargar módulos.
- **FR-003**: La navegación MUST identificar visualmente a Verificador de precios como el módulo seleccionado al iniciar.
- **FR-004**: La shell MUST cargar el placeholder del Verificador de precios en el área central desde el primer inicio, sin requerir una acción previa del operador.
- **FR-004A**: La navegación inicial MUST mostrar las utilerías futuras ilustradas en el mockup como opciones visibles y deshabilitadas; Verificador de precios MUST ser la única opción de módulo seleccionable durante esta feature.
- **FR-005**: El placeholder MUST mostrar un encabezado de módulo, un campo visual para código de barras, un área de información adicional, un panel de detalles del producto, un panel lateral de precio final y una zona inferior de estado o mensajes.
- **FR-005A**: El campo de código de barras MUST permanecer visible y deshabilitado; MUST NOT admitir foco, escritura, envío ni activar consultas durante esta feature.
- **FR-005B**: El área de información adicional MUST permanecer visible y deshabilitada; MUST NOT admitir foco, escritura ni persistir información durante esta feature.
- **FR-006**: El panel de detalles MUST reservar campos reconocibles para descripción, presentación y existencia, usando valores neutros mientras no haya datos reales.
- **FR-007**: El panel de precio MUST destacar visualmente el precio final y usar un valor neutral que no pueda confundirse con el resultado de una consulta real.
- **FR-008**: La shell MUST incluir espacios visuales diferenciados para estado de conexión, estado de licencia y mensajes operativos.
- **FR-009**: Los estados de conexión y licencia MUST presentarse como pendientes, neutrales o no disponibles mientras sus validaciones estén fuera de alcance; MUST NOT afirmar éxito sin validación real.
- **FR-010**: La presentación visual MUST mantener la intención de `docs/mockup-shell-utilerias.svg`: suite sobria de uso diario, menú lateral oscuro, superficie principal clara, tarjetas blancas con bordes suaves y azul como color de acción y acento.
- **FR-011**: La solución MUST permitir incorporar nuevos módulos en la navegación y cargarlos en el área central sin convertir el Verificador de precios en la aplicación completa ni exigir rediseñar la shell.
- **FR-012**: Las vistas MUST limitarse a presentación, enlaces de estado y comportamiento visual; la coordinación de estado e interacciones MUST residir fuera de las vistas.
- **FR-013**: La interfaz MUST NOT acceder directamente a fuentes de datos, licencias, reportes, impresión, configuración local ni otros recursos externos.
- **FR-014**: La base MUST separar responsabilidades de presentación, negocio, datos y entidades compartidas, respetando que presentación use capacidades de negocio y que el acceso a recursos externos quede aislado de la interfaz.
- **FR-015**: La base MUST disponer de un mecanismo central para suministrar dependencias a los componentes y de registro estructurado para diagnóstico, sin exponer detalles técnicos al operador.
- **FR-016**: Esta feature MUST crear las áreas iniciales recomendadas para shell, módulos, Verificador de precios, vistas, modelos de vista, comandos, estilos, servicios, conexión, repositorios, entidades y plantillas de reportes, aunque las áreas fuera del alcance puedan quedar vacías o con marcadores mínimos.
- **FR-017**: Los controles placeholder de Buscar, Imprimir y Configuración MUST permanecer visibles y deshabilitados; MUST NOT aceptar interacción ni ejecutar las capacidades futuras que representan.
- **FR-018**: Esta feature MUST NOT implementar conexión Firebird, configuración local, licenciamiento, búsqueda o consulta de productos, generación de códigos de barras, reportes ni impresión.
- **FR-019**: La aplicación MUST manejar fallos recuperables de inicialización mostrando un mensaje controlado, conservando la shell cuando sea posible y registrando diagnóstico sin secretos ni trazas técnicas visibles para el operador.
- **FR-020**: La base MUST cumplir la constitución vigente de SysTools y quedar preparada para que las funciones excluidas se incorporen como features incrementales posteriores.

### Technical and Organizational Constraints

- La aplicación objetivo es Windows Desktop y la base aprobada por el proyecto es C#, .NET 10 LTS y WPF.
- La organización obligatoria es Presentation, Business, Data y Entities, con una carpeta adicional Reports/Templates reservada para una feature futura.
- La interfaz sigue MVVM; la composición de dependencias usa Microsoft.Extensions.DependencyInjection y el registro estructurado usa Serilog.
- La estructura recomendada incluye `Presentation/Shell`, `Presentation/Modules/PriceVerifier`, `Presentation/Views`, `Presentation/ViewModels`, `Presentation/Commands`, `Presentation/Styles`, `Business/Services`, `Data/Connection`, `Data/Repositories`, `Entities` y `Reports/Templates`.
- El archivo `docs/mockup-shell-utilerias.svg` es la referencia visual de intención; no exige reproducción literal píxel por píxel ni convierte en funcionales los controles ilustrados.

### Key Entities

- **Módulo de utilería**: Representa una capacidad que puede aparecer en la navegación y cargarse en el área central; incluye identidad, nombre visible, disponibilidad y estado de selección.
- **Estado de shell**: Representa el módulo activo y los estados visuales globales de conexión, licencia y mensaje operativo, sin realizar por sí mismo validaciones externas.
- **Estado del Verificador de precios**: Representa los valores visibles del placeholder, como código de barras, información adicional, descripción, presentación, existencia, precio final y mensaje; en esta feature solo admite valores iniciales neutrales.
- **Mensaje operativo**: Representa información legible para el operador con contenido y nivel visual, sin detalles técnicos internos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En el 100% de 10 inicios consecutivos, la primera pantalla utilizable es la shell de SysTools con Verificador de precios visible y seleccionado, sin pantalla intermedia.
- **SC-002**: Una revisión visual identifica las seis zonas obligatorias del placeholder en menos de 30 segundos y sin consultar documentación adicional.
- **SC-003**: El 100% de las capacidades excluidas permanece sin efectos operativos durante las pruebas de interacción y ninguna comunica falsamente una conexión, licencia o resultado exitoso.
- **SC-004**: En tamaños de ventana desde 1280 × 720 hasta pantalla completa, las seis zonas principales y los estados globales permanecen legibles, sin superposición ni contenido esencial cortado.
- **SC-005**: Una revisión estructural confirma que el 100% de las áreas iniciales requeridas existe y que ningún elemento de interfaz contiene acceso directo a datos, licencias, reportes, impresión o configuración.
- **SC-006**: Ante un fallo recuperable provocado durante el inicio, la aplicación muestra un mensaje comprensible sin traza técnica, registra información de diagnóstico y evita un cierre abrupto en el 100% de los casos de prueba definidos.
- **SC-007**: Un nuevo módulo demostrativo puede registrarse y mostrarse en el área central sin modificar la composición visual interna del Verificador de precios ni crear otra ventana principal.
- **SC-008**: En una revisión comparativa, todos los rasgos de intención visual enumerados en FR-010 son reconocibles y no existen desviaciones que conviertan la primera pantalla en una landing page decorativa.

## Assumptions

- El usuario inicial es un operador de escritorio en Windows; autenticación y perfiles de usuario no forman parte de esta feature.
- La aplicación se ejecutará inicialmente como una sola instancia visual con una ventana principal; el control de múltiples procesos queda fuera de alcance.
- El tamaño mínimo soportado es 1280 × 720; el diseño debe adaptarse desde ese tamaño hasta pantalla completa.
- Los nombres de utilerías futuras que aparecen en el mockup se mostrarán como contexto visual en opciones deshabilitadas y no representan módulos funcionales comprometidos por esta feature.
- Los accesos rápidos mostrados en el mockup pueden aparecer como referencia inactiva; su comportamiento se especificará en features posteriores.
- No se requiere persistir selección, tamaño de ventana, tema ni preferencias porque la configuración local está excluida.
- Los estados iniciales serán neutrales o explícitamente no disponibles, dado que conexión y licenciamiento aún no existen.
- La validación de Presentation será una revisión manual guiada; no se requieren pruebas de integración con servicios externos para este placeholder.

## Dependencies

- Constitución vigente de SysTools, versión 1.1.0.
- Referencia visual `docs/mockup-shell-utilerias.svg`.
- Entorno Windows capaz de ejecutar la plataforma de escritorio aprobada por el proyecto.

## Out of Scope

- Conexión o pruebas contra Firebird.
- Lectura o escritura de configuración local y protección de secretos.
- Validación, activación o administración de licencias.
- Consulta, búsqueda o persistencia de productos y precios.
- Generación o lectura funcional de códigos de barras.
- Generación, vista previa o impresión de reportes y etiquetas.
- Implementación funcional de otras utilerías mostradas en la navegación.
