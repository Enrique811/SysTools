# Feature Specification: Reportes, etiquetas e impresión

**Feature Branch**: `[011-label-reports-printing]`

**Created**: 2026-09-25

**Status**: Complete (production printing gated by physical approval)

**Input**: User description: "Implementar generación, vista previa e impresión de etiquetas de precio, evaluando primero un motor de reportes compatible y soportando plantillas de 1, 2 y 3 columnas."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Revisar etiquetas antes de producirlas (Priority: P1)

Como operador en ambiente de pruebas, quiero generar y revisar una vista previa de la fila de etiquetas preparada para comprobar producto, precio, código de barras e información adicional sin gastar papel.

**Why this priority**: La vista previa reduce el riesgo de imprimir precios o diseños incorrectos y es la forma segura de validar la migración contra las etiquetas heredadas.

**Independent Test**: Con productos preparados y una configuración de pruebas, se completa una fila de una, dos o tres etiquetas y se comprueba que aparece una única vista previa con todos los datos, posiciones y códigos correctos, sin enviar trabajos a una impresora.

**Acceptance Scenarios**:

1. **Given** ambiente de pruebas y una fila completa de una etiqueta, **When** el operador solicita imprimir, **Then** se genera una vista previa legible y no se crea ningún trabajo de impresión.
2. **Given** una fila completa de dos o tres etiquetas, **When** se genera la vista previa, **Then** cada captura ocupa exactamente su posición y ninguna etiqueta se duplica u omite.
3. **Given** una vista previa abierta, **When** el operador la cierra, **Then** regresa al verificador estable y preparado para otra captura.
4. **Given** una plantilla ausente, incompatible o dañada, **When** se intenta generar la vista previa, **Then** se informa un fallo controlado y no se pierde ni imprime silenciosamente la fila preparada.

---

### User Story 2 - Imprimir etiquetas en producción (Priority: P1)

Como operador en ambiente productivo, quiero enviar la fila completa a la impresora configurada para obtener etiquetas equivalentes a las del sistema anterior sin diálogos inesperados ni destinos ambiguos.

**Why this priority**: La impresión física es el resultado operativo de la feature y afecta directamente precios, consumibles y atención al cliente.

**Independent Test**: Con una impresora controlada y una plantilla aprobada, se imprime una fila de cada capacidad y se verifica un único trabajo por fila, destino exacto, contenido legible y resultado controlado.

**Acceptance Scenarios**:

1. **Given** ambiente productivo, plantilla válida e impresora disponible, **When** se completa una fila, **Then** se envía exactamente un trabajo a la impresora configurada sin mostrar vista previa.
2. **Given** una impresora configurada que ya no está disponible, **When** se intenta imprimir, **Then** no se redirige a otra impresora y el operador recibe un mensaje recuperable.
3. **Given** un fallo al crear o enviar el trabajo, **When** termina el intento, **Then** el sistema no informa éxito, conserva estabilidad y registra la categoría técnica sin datos sensibles.
4. **Given** dos solicitudes rápidas para la misma fila, **When** la primera está en curso, **Then** la segunda no produce un trabajo duplicado.

---

### User Story 3 - Operar la cola desde el verificador (Priority: P2)

Como operador del verificador, quiero preparar etiquetas con teclado y conocer el avance de una fila de varias columnas para imprimir únicamente cuando esté completa o cancelar pendientes de forma explícita.

**Why this priority**: Integra las reglas ya migradas con el flujo diario sin ocultar estado ni mezclar capturas entre filas.

**Independent Test**: Desde un producto visible se preparan etiquetas con una acción de teclado, se observa el avance 1/2 o 2/3, se completa la fila y se dispara una sola salida; cancelar elimina los pendientes y la captura siguiente vuelve a posición uno.

**Acceptance Scenarios**:

1. **Given** un producto válido visible, **When** el operador activa la acción de etiqueta, **Then** se captura una instantánea con el precio presentado, información adicional y código de barras correspondiente.
2. **Given** capacidad de dos o tres columnas y una fila incompleta, **When** se agrega una captura, **Then** se informa posición, pendientes y restantes sin generar salida todavía.
3. **Given** una fila que alcanza su capacidad, **When** entra la última captura, **Then** se genera exactamente una salida conforme al ambiente y la cola queda lista para la siguiente fila.
4. **Given** etiquetas pendientes, **When** el operador cancela la fila, **Then** se informa cuántas fueron descartadas y ninguna aparece en una salida posterior.

### Edge Cases

- El producto no tiene precio, código utilizable o datos suficientes para una etiqueta.
- El código contiene ceros iniciales, checksum inválido, texto o caracteres no representables por el formato preferido.
- La descripción o información adicional contiene acentos, saltos, caracteres especiales o excede el espacio visible.
- La configuración cambia mientras existe una fila pendiente o una salida en curso.
- La plantilla seleccionada no coincide con la cantidad de columnas o contiene campos desconocidos.
- La vista previa se cierra durante la generación o la aplicación se cierra durante una impresión cooperativa o no cooperativa.
- La impresora queda fuera de línea, cambia de nombre, rechaza el trabajo o no reporta resultado concluyente.
- Se solicita salida repetidamente, llega una respuesta tardía o dos filas terminan casi simultáneamente.
- No existen plantillas aprobadas o muestras físicas de referencia para una medida heredada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST ofrecer desde el verificador una acción visible y una acción de teclado para preparar/imprimir la etiqueta del producto actualmente mostrado.
- **FR-002**: Solo un producto válido con precio final disponible MUST poder convertirse en etiqueta; un rechazo MUST conservar estable el producto visible y explicar la causa.
- **FR-003**: Cada etiqueta MUST usar una instantánea inmutable de producto, precio presentado e información adicional correspondiente al momento de captura.
- **FR-004**: Cada etiqueta MUST incluir al menos código, descripción, presentación, precio, información adicional y una representación legible del código de barras.
- **FR-005**: La representación del código de barras MUST conservar el valor normalizado y la clasificación ya definida por el sistema, incluidos ceros iniciales y fallback controlado.
- **FR-006**: La salida MUST admitir exclusivamente filas de una, dos o tres etiquetas, conservando el orden y la posición de captura.
- **FR-007**: Para dos o tres columnas, el sistema MUST informar el avance sin generar salida hasta completar la fila.
- **FR-008**: El operador MUST poder cancelar una fila incompleta; la cancelación MUST eliminar únicamente sus pendientes e informar la cantidad descartada.
- **FR-009**: La cantidad de columnas, ambiente, plantilla e impresora MUST provenir de una configuración válida al iniciar una fila.
- **FR-010**: Un cambio de configuración durante una fila pendiente MUST NOT mezclar criterios dentro de la fila; el cambio aplica únicamente después de completarla o cancelarla.
- **FR-011**: En ambiente de pruebas, una fila completa MUST abrir una vista previa y MUST NOT enviar trabajos de impresión automáticamente.
- **FR-012**: En ambiente productivo, una fila completa MUST enviarse directamente a la impresora configurada y MUST NOT abrir vista previa.
- **FR-013**: El sistema MUST requerir una plantilla administrada, disponible y compatible con la capacidad de la fila antes de generar cualquier salida.
- **FR-014**: El sistema MUST requerir que la impresora configurada esté disponible antes de imprimir y MUST NOT sustituirla silenciosamente por la predeterminada u otra impresora.
- **FR-015**: Una fila completa MUST producir como máximo una vista previa o un trabajo de impresión, incluso ante activaciones repetidas o respuestas tardías.
- **FR-016**: Mientras se genera o imprime, la interfaz MUST comunicar estado ocupado e impedir solicitudes duplicadas y cierres contradictorios.
- **FR-017**: El resultado MUST distinguir al menos éxito, cancelación, configuración inválida, plantilla ausente/incompatible, renderizado fallido, impresora ausente y envío fallido.
- **FR-018**: Todo fallo recuperable MUST mantener operable la shell y permitir reintentar sin reutilizar accidentalmente una fila ya consumida.
- **FR-019**: La vista previa MUST permitir revisar visualmente las etiquetas completas en una escala legible antes de cerrarse.
- **FR-020**: Las plantillas y tamaños aprobados MUST demostrar compatibilidad suficiente con muestras del sistema anterior antes de habilitar impresión productiva.
- **FR-021**: La evaluación del motor MUST comprobar vista previa, impresión directa, selección exacta de impresora, distribución de plantillas y filas de una, dos y tres columnas antes de adoptarlo.
- **FR-022**: Si el motor evaluado no satisface los criterios de adopción, el sistema MUST documentar la alternativa seleccionada y MUST NOT introducirlo como dependencia productiva.
- **FR-023**: Las capacidades de renderizado e impresión MUST estar separadas de la interfaz y ser sustituibles por dobles controlados para validación.
- **FR-024**: La interfaz MUST NOT abrir archivos de plantilla, generar reportes, acceder a APIs de impresión ni enumerar impresoras directamente.
- **FR-025**: Los registros MUST incluir etapa, categoría, duración y un identificador opaco de operación, sin password, UUID completo, ruta privada, contenido de licencia, datos completos del producto, imagen de código ni documento renderizado.
- **FR-026**: Los mensajes al operador MUST ser controlados, textuales y no contener excepciones, stack traces ni detalles internos del dispositivo.
- **FR-027**: La acción de etiqueta, el progreso, la cancelación y el cierre de vista previa MUST poder operarse con teclado y anunciar estado mediante texto además de color.
- **FR-028**: La vista previa MUST permanecer utilizable desde 1280x720 hasta pantalla completa y a 125% de escala.
- **FR-029**: La feature MUST reutilizar las reglas existentes de precio, cola y código de barras sin duplicarlas dentro de reportes o UI.
- **FR-030**: Esta feature MUST NOT editar plantillas visualmente, instalar/configurar impresoras, reimprimir historial, generar PDF como archivo permanente ni modificar productos, precios, licencias o configuración.

### Key Entities

- **Solicitud de etiqueta**: Instantánea segura de una captura, su posición y la configuración fija de la fila.
- **Fila de etiquetas**: Conjunto inmutable y ordenado de una, dos o tres solicitudes listo para una única salida.
- **Plantilla administrada**: Diseño aprobado identificado por un ID seguro, capacidad y medida, sin exponer rutas privadas a Presentation.
- **Documento de etiqueta**: Resultado efímero y renderizado de una fila que puede previsualizarse o enviarse a impresión.
- **Trabajo de impresión**: Solicitud de salida asociada a una impresora exacta y a un identificador opaco para impedir duplicados.
- **Resultado de salida**: Categoría segura que permite a UI informar éxito, espera, cancelación o fallo recuperable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En 100 filas por cada capacidad 1, 2 y 3, cada captura aparece exactamente una vez, en orden y en su posición correcta.
- **SC-002**: En 50 activaciones duplicadas por ambiente, cada fila produce como máximo una vista previa o un trabajo de impresión.
- **SC-003**: El 100% de las ejecuciones en pruebas produce cero trabajos de impresión y el 100% de las ejecuciones productivas exitosas evita abrir vista previa.
- **SC-004**: Una matriz de todos los estados de plantilla, renderizado e impresora produce una categoría y mensaje distinguibles sin cerrar la aplicación.
- **SC-005**: El 100% de códigos de referencia conserva valor y clasificación, y sus imágenes superan una verificación automatizada de estructura y decodificación.
- **SC-006**: Las muestras aprobadas de 1, 2 y 3 columnas contienen código, descripción, presentación, precio e información adicional legibles y dentro de sus áreas asignadas.
- **SC-007**: Cerrar o cancelar en 20 operaciones en curso no publica resultados tardíos ni genera trabajos adicionales.
- **SC-008**: Una auditoría automatizada encuentra cero datos sensibles o payloads completos en mensajes y registros de 100 operaciones exitosas y fallidas.
- **SC-009**: Un operador puede preparar, completar/cancelar y revisar una fila usando solo teclado, con las acciones siempre accesibles a 1280x720 y 125%.
- **SC-010**: La decisión de motor queda respaldada por evidencia reproducible para vista previa, impresión, impresora exacta, distribución y 1/2/3 columnas antes de habilitar producción.

## Assumptions

- `a` continúa significando ambiente de pruebas con vista previa y `b` ambiente productivo con impresión directa.
- La cola existente de etiquetas sigue siendo la fuente de verdad para completar filas; esta feature coordina su salida y no cambia sus reglas.
- Las plantillas productivas serán administradas por la aplicación y se distribuirán con versiones posteriores; no se editarán desde SysTools.
- La comparación visual usa muestras ficticias o anonimizadas aprobadas y dispositivos de prueba, nunca datos sensibles de clientes.
- Un envío aceptado por el subsistema de impresión se considera éxito técnico; confirmar que el papel salió físicamente requiere validación controlada.

## Dependencies

- Configuración y secretos de Feature 002.
- Reglas de precio y cola de etiquetas de Feature 005.
- Generación y clasificación de código de barras de Feature 007.
- Flujo del verificador de Feature 008 y configuración visual/catálogos de Feature 010.
- Una impresora controlada y muestras de referencia para la validación física opt-in.

## Out of Scope

- Diseñador o editor de plantillas.
- Instalación, eliminación, configuración o mantenimiento de impresoras.
- Historial, reimpresión, auditoría contable o servidor central de trabajos.
- Exportación permanente a PDF, imagen u otros formatos por el operador.
- Impresión de documentos distintos a etiquetas de precio.
- Cambios a productos, precios, consultas Firebird, licenciamiento o formato regional.
