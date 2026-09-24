# Data Model: Generacion de codigos de barras

## BarcodeType

Clasificacion final usada tanto por Business como por el encoder.

| Valor | Regla |
|---|---|
| `Ean8` | Exactamente 8 digitos ASCII y checksum EAN-8 valido |
| `Ean13` | Exactamente 13 digitos ASCII y checksum EAN-13 valido |
| `UpcA` | Exactamente 12 digitos ASCII y checksum UPC-A valido |
| `Code128` | Alternativa para cualquier otro valor representable |

No existe valor `Unknown`: una entrada presente y aceptada siempre se clasifica en uno de los cuatro tipos.

## BarcodeGenerationStatus

| Valor | Significado | Imagen |
|---|---|---|
| `Success` | PNG completo y validado | Presente |
| `MissingInput` | Entrada nula, vacia o vacia despues de normalizar | Ausente |
| `UnsupportedValue` | Contiene caracteres que CODE128 no puede reproducir exactamente | Ausente |
| `InputTooLong` | Supera 50 caracteres normalizados | Ausente |
| `GenerationFailed` | ZXing o la serializacion no pudo producir un PNG valido | Ausente |

Los estados son finales y excluyentes. La cancelacion no es un estado: se propaga mediante `OperationCanceledException`.

## BarcodeGenerationResult

Resultado sellado e inmutable que cruza capas.

| Campo | Tipo | Regla |
|---|---|---|
| `Status` | `BarcodeGenerationStatus` | Obligatorio |
| `Message` | `string` | Mensaje amigable estable; nunca contiene entrada, bytes o detalle tecnico |
| `NormalizedValue` | `string?` | Solo en `Success`; resultado de retirar espacios exteriores una vez |
| `Type` | `BarcodeType?` | Solo en `Success` |
| `Width` | `int` | Positivo solo en `Success`; cero en fallos |
| `Height` | `int` | Positivo solo en `Success`; cero en fallos |
| `ImageLength` | `int` | `1..1,048,576` solo en `Success`; cero en fallos |
| imagen privada | `byte[]` | Copia defensiva; nunca se expone por referencia |

### Operaciones

- `GetImageBytes()` devuelve una nueva copia en cada llamada.
- Las fabricas de exito y fallo validan invariantes y evitan constructores con combinaciones arbitrarias.
- El resultado no contiene excepciones, mensajes tecnicos ni el valor rechazado.
- Los fallos exponen mensajes seguros: entrada requerida, caracteres incompatibles, longitud excedida o generacion no disponible.

### Invariantes de exito

- `Status == Success`.
- `NormalizedValue` no es vacio y mide como maximo 50 caracteres.
- `Type` esta presente.
- La imagen inicia con la firma PNG y no supera 1 MiB.
- Para `Ean8`, `Ean13` y `UpcA`: `Width == 340` y `Height == 56`.
- Para `Code128`: `Width == max(500, NormalizedValue.Length * 18)` y `Height == 60`.

### Invariantes de fallo

- `Status != Success`.
- `NormalizedValue` y `Type` son nulos.
- `Width`, `Height` e `ImageLength` son cero.
- `GetImageBytes()` devuelve un arreglo vacio nuevo.

## BarcodeEncodingRequest

Valor interno del contrato Business -> Data; puede representarse mediante parametros sin convertirse en entidad publica.

| Campo | Tipo | Regla |
|---|---|---|
| `NormalizedValue` | `string` | No vacio, maximo 50 y ya validado como representable |
| `Type` | `BarcodeType` | Tipo ya clasificado por Business |
| `Width` | `int` | 340, 500..900 segun tipo |
| `Height` | `int` | 56 o 60 segun tipo |

Data no reclasifica ni cambia dimensiones; traduce el tipo a `ZXing.BarcodeFormat`, genera y serializa.

## Flujo de estado

```text
entrada
  |-- cancelada ------------------------------> OperationCanceledException
  |-- nula/vacia/solo espacios --------------> MissingInput
  |-- longitud normalizada > 50 -------------> InputTooLong
  |-- no representable en CODE128 -----------> UnsupportedValue
  `-- valida
       |-- clasificar y calcular dimensiones
       |-- cancelada -------------------------> OperationCanceledException
       |-- encoder/PNG invalido --------------> GenerationFailed
       `-- PNG valido y decodificable --------> Success
```

## Reglas derivadas

- La normalizacion nunca convierte el texto a numero y conserva ceros iniciales y espacios interiores.
- La comprobacion de longitud ocurre antes de clasificar o reservar una imagen.
- Los checksums solo inspeccionan caracteres ASCII numericos.
- El mismo valor normalizado produce el mismo tipo y dimensiones; el PNG debe ser funcionalmente determinista.
