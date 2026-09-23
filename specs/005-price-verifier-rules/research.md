# Research: Reglas de negocio del verificador

## Decision 1: Contrato de consultas de producto

**Decision**: Crear `IProductService`/`ProductService` en Business como fachada sobre `IProductRepository`. La configuracion es obligatoria; las entradas nulas o whitespace devuelven `null` o lista vacia antes de invocar el repositorio. Las entradas no vacias se recortan solo en los extremos. Cancelaciones y `RepositoryAccessException` se propagan sin envolver.

**Rationale**: Mantiene Presentation fuera de Data, hace verificable la normalizacion y preserva los errores seguros ya catalogados por la feature 004.

**Alternatives considered**:

- Consumir `IProductRepository` directamente desde el ViewModel: viola el limite Presentation -> Business y dispersa reglas.
- Crear nuevos DTOs de producto: duplicaria `Product` sin aportar aislamiento adicional.
- Convertir fallos en producto inexistente: ocultaria indisponibilidad de Firebird como si fuera un resultado valido.

## Decision 2: Formato y redondeo monetario

**Decision**: `PriceFormatterService` acepta `decimal` y un codigo opcional. `MX` redondea a dos decimales con `MidpointRounding.ToEven` y usa `InvariantCulture` con patron `$0.00`; cualquier otro codigo sigue el fallback legado `CO`, redondea a cero decimales con `ToEven` y usa una copia explicita de `es-CO` con formato moneda. Un precio nulo se rechaza antes de llamar al formatter.

**Rationale**: Java `DecimalFormat` usa por defecto HALF_EVEN; `decimal` evita errores binarios y las culturas explicitas hacen el resultado independiente de `CurrentCulture`.

**Alternatives considered**:

- Usar la cultura activa del equipo: produciria resultados diferentes por instalacion.
- Redondear AwayFromZero: alteraria mitades respecto al comportamiento estandar del formatter legado.
- Formatear precio nulo como cero: puede imprimir un precio falso.

## Decision 3: Instantanea de datos de etiqueta

**Decision**: `LabelData` sera inmutable y contendra la referencia al `Product` inmutable, `FormattedPrice` no vacio e `Information` no nulo. Normaliza solamente `null` a `string.Empty`; no recorta ni altera informacion adicional.

**Rationale**: Evita duplicar los campos del producto en esta etapa, conserva el texto exacto del operador y entrega a reportes futuros un valor estable por captura.

**Alternatives considered**:

- Leer informacion adicional global al imprimir: mezclaria capturas si el operador cambia el texto.
- Guardar un objeto mutable: permitiria que una fila ya completada cambiara despues de entregarse.

## Decision 4: Estado de la cola

**Decision**: `LabelQueueService` mantiene como maximo tres `PendingLabel` bajo un bloqueo privado. `Capture(LabelData, columns)` valida 1..3 antes de mutar, asigna posiciones base 1 y devuelve `LabelQueueResult` con copias de solo lectura. Al alcanzar capacidad, el resultado contiene la fila completa y el estado interno se vacia en la misma seccion critica. `Cancel()` vacia y devuelve el conteo descartado.

**Rationale**: Modela directamente la fila fisica, impide observacion de estados intermedios y garantiza que cada etiqueta aparezca una sola vez incluso si futuros consumidores llaman desde mas de un hilo.

**Alternatives considered**:

- Tres variables especiales como el Java legado: escala mal, duplica ramas y facilita residuos.
- Devolver la lista interna: permite corrupcion externa del orden y conteo.
- Persistir la cola: esta fuera de alcance y agregaria recuperacion/versionado no requerido.

## Decision 5: Cambio de columnas durante una fila incompleta

**Decision**: La primera captura pendiente fija la capacidad activa. Una captura con capacidad distinta se rechaza mediante error de estado antes de mutar. Al completar o cancelar, una nueva fila puede usar cualquier capacidad valida.

**Rationale**: No existe una asignacion segura y obvia entre posiciones de filas de distinto ancho. Rechazar conserva pendientes y obliga a una decision explicita del operador.

**Alternatives considered**:

- Cancelar automaticamente: perderia capturas sin una accion explicita.
- Migrar pendientes a la nueva capacidad: podria completar e imprimir una fila que el operador no esperaba.

## Decision 6: Composicion y ciclo de vida

**Decision**: Registrar `IProductService`, `IPriceFormatterService` e `ILabelQueueService` como singletons en la aplicacion WPF. El primero depende del repositorio singleton, el formatter no tiene estado y la cola representa la unica sesion del modulo verificador actual.

**Rationale**: Coincide con los ciclos existentes de la shell y conserva los pendientes mientras vive la aplicacion. El bloqueo interno mantiene operaciones atomicas.

**Alternatives considered**:

- Transient para la cola: perderia pendientes entre resoluciones.
- Estado de cola en el ViewModel: colocaria reglas de negocio en Presentation y dificultaria pruebas futuras.

## Decision 7: Estrategia de pruebas

**Decision**: Agregar `SysTools.BusinessRules.Tests` con xUnit para contratos de entidad, consultas con doble de repositorio, culturas/precios, colas y arquitectura. Mantener la prueba de DI en `SysTools.Presentation.Tests`. No se requiere Firebird real porque la feature solo consume una abstraccion ya validada.

**Rationale**: La constitucion exige pruebas de reglas puras y la separacion evita que fallos externos vuelvan inestable esta bateria.

**Alternatives considered**:

- Agregar las pruebas a `SysTools.Repositories.Tests`: mezclaria responsabilidades y haria menos visible el alcance de la feature.
- Pruebas manuales de WPF: no validan la independencia requerida y la UI funcional pertenece a otra feature.
