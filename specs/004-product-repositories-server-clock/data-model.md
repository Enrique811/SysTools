# Data Model: Repositorios de productos y fecha del servidor

## Product

Articulo de solo lectura utilizado por Business sin tipos Firebird.

| Field | Type | Required | Source | Rules |
|---|---|---:|---|---|
| `Id` | `int` | Yes | `PRODUCTOS.ID` | Positivo; sustituye los dos alias heredados del mismo ID. |
| `Barcode` | `string` | Yes | `PRODUCTOS.CODIGO` | No vacio, maximo 50 caracteres, conserva ceros iniciales. |
| `Description` | `string` | Yes | `PRODUCTOS.DESCRIPCION` | Maximo 255; nulo se normaliza a `string.Empty`; no truncar. |
| `Presentation` | `string?` | No | `PRODUCTOS.TVENTA` | Se conserva nulo; no inventar presentacion. |
| `PriceWithTax` | `decimal?` | No | `PRODUCTOS.PFINAL` | Se conserva nulo; sin formato `CO`/`MX`. |
| `Stock` | `string` | Yes | `INVENTARIO_BALANCES.CANTIDAD_ACTUAL` | Nulo, vacio o balance ausente se vuelve `Sin registro`; otro valor usa cultura invariante. |

### Invariants

- `Id` debe ser mayor que cero.
- `Barcode` no admite nulo, vacio ni mas de 50 caracteres.
- `Description` nunca es nulo y no supera 255 caracteres.
- `Stock` nunca es nulo o vacio.
- El objeto es inmutable despues de construirse.
- `ToString()` no incluye descripcion, precio, stock ni codigo de barras completo.

## RepositoryOperation

Identificador estable de la operacion que fallo sin exponer SQL o entradas.

| Value | Meaning |
|---|---|
| `GetProductByBarcode` | Consulta exacta de producto. |
| `SearchProductsByDescription` | Busqueda por prefijo. |
| `GetServerTimestamp` | Lectura de `CURRENT_TIMESTAMP`. |

## RepositoryAccessException

Fallo seguro entregado a Business cuando Firebird no completa una operacion.

| Field | Type | Rules |
|---|---|---|
| `Operation` | `RepositoryOperation` | Valor definido y estable. |
| `Message` | `string` | Catalogo controlado; no copia mensaje del proveedor. |
| `InnerException` | N/A | No se conserva para impedir filtracion accidental por capas superiores. |

La cancelacion no se representa con esta excepcion: conserva `OperationCanceledException` y el token del consumidor.

## Server timestamp

Se representa como `DateTime` devuelto por el proveedor. No se cambia `Kind`, no se convierte de zona y no se sustituye por reloj local. No es una entidad persistida.

## Source Relationships

```text
PRODUCTOS (ID unique, CODIGO unique)
    1
    |
    | LEFT JOIN by PRODUCTO_ID
    0..1
INVENTARIO_BALANCES

RDB$DATABASE
    `-- CURRENT_TIMESTAMP (one row)
```

## Lifecycle

Los modelos no tienen transiciones ni persistencia local. Cada llamada crea recursos de lectura, materializa el resultado y libera todos los recursos antes de devolverlo.
