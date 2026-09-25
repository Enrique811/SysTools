# Research: Búsqueda de productos por descripción

## Decision 1: Extender la sesión existente del verificador

**Decision**: Agregar `SearchAsync` a `IPriceVerifierWorkflow` y reutilizar su configuración privada, readiness y epoch.

**Rationale**: Buscar requiere la misma sesión validada que consultar por código. Un workflow separado duplicaría preparación/licencia o expondría configuración sensible.

**Alternatives considered**: Inyectar `IProductService` en Presentation; crear un workflow paralelo; recargar configuración por búsqueda.

## Decision 2: Resultado de búsqueda seguro y específico

**Decision**: Crear estado, resultado e item inmutables. Cada item contiene solo código, descripción, precio formateado y existencia.

**Rationale**: La UI recibe exclusivamente los datos que muestra y no aplica formato ni reglas comerciales. Los estados inválidos o de fallo contienen cero items.

**Alternatives considered**: Devolver `Product` completo; devolver tuplas; transportar configuración/formato a Presentation.

## Decision 3: Prefijo literal con `STARTING WITH`

**Decision**: Cambiar la consulta existente a `UPPER(descripción) STARTING WITH UPPER(@descriptionPrefix)` y pasar el prefijo sin `%`.

**Rationale**: `LIKE` interpreta `%` y `_` como comodines. `STARTING WITH` conserva esos caracteres como texto literal y expresa directamente la regla funcional; la documentación oficial de Firebird confirma tanto la semántica de comodines de `LIKE` como la operación de prefijo de `STARTING WITH`.

**Alternatives considered**: Mantener `LIKE` escapando `%`, `_` y el carácter de escape, lo que amplía el parámetro y aumenta complejidad; filtrar resultados en memoria, lo que consulta datos incorrectos y desperdicia recursos.

**Reference**: [Firebird Language Reference — predicates](https://www.firebirdsql.org/file/documentation/html/en/refdocs/fblangref50/firebird-50-language-reference.html).

## Decision 4: Preservar el orden de Data

**Decision**: Business conserva el orden estable `descripción, id` entregado por el repositorio y no reordena con la cultura del equipo.

**Rationale**: Evita diferencias de collation entre la consulta real y la interfaz, además de mantener un desempate reproducible.

**Alternatives considered**: Ordenar en ViewModel; ordenar con cultura actual; omitir desempate.

## Decision 5: Diálogo transitorio detrás de un servicio

**Decision**: Crear un ViewModel nuevo por apertura y un `IProductSearchDialogService` que encapsula la ventana modal y devuelve cancelación, selección o fallo operativo.

**Rationale**: El ViewModel principal no conoce `Window`, las pruebas sustituyen el servicio y una instancia transitoria impide contaminación entre aperturas.

**Alternatives considered**: Crear la ventana desde el ViewModel; reutilizar una ventana singleton; incrustar la tabla permanentemente en el módulo.

## Decision 6: Selección por código y consulta exacta existente

**Decision**: El diálogo devuelve únicamente el código exacto. El ViewModel principal establece `Barcode` y ejecuta una vez el `LookupAsync` existente.

**Rationale**: Reutiliza formato, neutralidad, NotFound, bloqueo y foco de Feature 008 sin duplicar publicación comercial.

**Alternatives considered**: Copiar el item directamente a la pantalla; devolver `Product`; agregar una segunda ruta de publicación.

## Decision 7: Teclado contextual

**Decision**: Ctrl+F7 abre; Enter en criterio busca; Enter en tabla selecciona; Escape cancela; doble clic invoca el mismo comando de selección.

**Rationale**: Evita que un botón predeterminado global intercepte Enter en la tabla y mantiene equivalencia entre teclado y mouse.

**Alternatives considered**: `IsDefault` global; handlers de negocio en code-behind; combinaciones distintas al sistema anterior.

## Decision 8: Single-flight, cancelación y generación

**Decision**: El diálogo usa comando async, CTS y generación propia. Cerrar cancela e incrementa generación; respuestas antiguas no publican. El epoch Business continúa protegiendo la sesión global.

**Rationale**: Token más generación cubren proveedores cooperativos y no cooperativos sin convertir búsquedas sucesivas en latest-wins.

**Alternatives considered**: Cola de búsquedas; cancelar la búsqueda vigente ante cada Enter; depender solo de CancellationToken.

## Decision 9: Fallo operativo coordinado

**Decision**: Input inválido y lista vacía permanecen en el diálogo. Un fallo de catálogo invalida Business, cierra el diálogo y hace que el padre limpie/bloquee el módulo con Retry.

**Rationale**: Mantiene la taxonomía de Feature 008 y evita que el diálogo muestre disponibilidad contradictoria.

**Alternatives considered**: Mantener el diálogo abierto tras perder sesión; tratar catálogo caído como lista vacía; volver a preparar automáticamente.

## Decision 10: Validación visual y seguridad

**Decision**: Combinar pruebas puras, WPF STA real, auditorías de arquitectura/logs, integración Firebird opt-in y recorrido manual documentado.

**Rationale**: Texto XAML por sí solo no prueba carga, foco o comandos; integración real es necesaria para collation/encoding, pero no debe inventarse cuando falte entorno autorizado.

**Alternatives considered**: Solo unitarias; solo recorrido manual; automatización UI externa con paquete nuevo.
