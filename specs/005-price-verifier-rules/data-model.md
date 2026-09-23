# Data Model: Reglas de negocio del verificador

## Product

Entidad inmutable existente de la feature 004. Esta feature no cambia sus campos ni invariantes. `PriceWithTax` puede ser nulo; esa condicion permite mostrar el producto, pero bloquea crear una etiqueta.

## LabelData

Instantanea inmutable de una captura destinada a una futura etiqueta.

| Field | Type | Required | Rules |
|---|---|---:|---|
| `Product` | `Product` | Yes | Referencia no nula a la entidad inmutable; no copiar ni alterar campos. |
| `FormattedPrice` | `string` | Yes | No nulo, no vacio y no whitespace; resultado de `IPriceFormatterService`. |
| `Information` | `string` | Yes | `null` se normaliza a `string.Empty`; cualquier otro texto se conserva exactamente, incluidos espacios y caracteres especiales. |

### Invariants

- La instancia no cambia despues de construirse.
- No representa precio ausente; el constructor rechaza un `Product.PriceWithTax` nulo.
- `ToString()` no expone codigo, descripcion, precio ni informacion adicional.

## PendingLabel

Etiqueta capturada que aun pertenece a la fila en progreso.

| Field | Type | Required | Rules |
|---|---|---:|---|
| `Position` | `int` | Yes | Entre 1 y 3; coincide con el orden de captura dentro de la fila. |
| `Data` | `LabelData` | Yes | No nulo e inmutable. |

### Invariants

- Las posiciones de una fila son contiguas y base 1.
- Una captura pendiente pertenece a una sola fila.

## LabelQueueResult

Instantanea devuelta despues de cada captura.

| Field | Type | Required | Rules |
|---|---|---:|---|
| `Capacity` | `int` | Yes | Solo 1, 2 o 3. |
| `Pending` | `IReadOnlyList<PendingLabel>` | Yes | Copia de solo lectura; vacia cuando la captura completo la fila. |
| `Completed` | `IReadOnlyList<LabelData>` | Yes | Copia de solo lectura; vacia mientras la fila este incompleta; al completar contiene exactamente `Capacity` elementos. |
| `Remaining` | `int` | Yes | Derivado: `0` si completo; en otro caso `Capacity - Pending.Count`. |
| `IsComplete` | `bool` | Yes | Derivado de que `Completed.Count == Capacity`. |

### Invariants

- `Pending` y `Completed` nunca contienen simultaneamente elementos.
- Las colecciones entregadas no permiten alterar el estado interno del servicio.
- Una fila completa conserva el orden de captura y no vuelve a aparecer en el siguiente resultado.

## Queue state

Estado interno efimero de `LabelQueueService`.

| Field | Type | Rules |
|---|---|---|
| Active capacity | `int?` | Nulo cuando no hay pendientes; 2 o 3 mientras una fila esta incompleta. |
| Pending labels | ordered collection | Cero a `capacity - 1`; nunca se expone directamente. |

### State transitions

```text
Empty
  | capture columns=1
  +----------------------> Empty + completed row(1)
  |
  | capture columns=2|3
  v
Pending(1..capacity-1)
  | capture same capacity before full
  +----------------------> Pending(next position)
  | capture fills capacity
  +----------------------> Empty + completed row(capacity)
  | cancel
  +----------------------> Empty + discarded count
  | capture different capacity
  +----------------------> Rejected; state unchanged
```

## Service relationships

```text
Presentation composition root
    ├── IProductService -> ProductService -> IProductRepository
    ├── IPriceFormatterService -> PriceFormatterService
    └── ILabelQueueService -> LabelQueueService

Product + formatted price + information
    -> LabelData
    -> PendingLabel(s)
    -> LabelQueueResult.Pending | LabelQueueResult.Completed
```
