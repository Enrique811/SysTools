# Contract: Barcode generation

## Public Business API

```csharp
public interface IBarcodeService
{
    BarcodeType DetectType(string? input);

    BarcodeGenerationResult Generate(
        string? input,
        CancellationToken cancellationToken = default);
}
```

`DetectType` conserva la semantica del Java: normaliza una vez; devuelve el tipo numerico solo para longitud y checksum validos, y `Code128` para cualquier otro caso, incluida entrada ausente. No genera imagen ni registra el contenido.

`Generate` es la operacion segura para consumidores. Nunca lanza por entrada o fallo recuperable; solo permite que se propague la cancelacion solicitada y errores fatales del proceso.

## Business-to-Data port

```csharp
public interface IBarcodeImageEncoder
{
    byte[] EncodePng(
        string normalizedValue,
        BarcodeType type,
        int width,
        int height,
        CancellationToken cancellationToken = default);
}
```

- Business entrega valores ya normalizados, representables y acotados.
- Data no cambia tipo ni dimensiones.
- Exito devuelve un PNG completo y no vacio.
- Un fallo recuperable se traduce a `BarcodeEncodingException` sin incluir entrada o bytes en el mensaje.
- `OperationCanceledException` se propaga intacta.
- El arreglo devuelto pertenece a Business; el resultado publico hace copia defensiva.

## Normalization and validation contract

1. Observar cancelacion.
2. Retirar una sola vez caracteres `U+0000..U+0020` de ambos extremos, como `String.trim()` de Java; preservar cualquier otro caracter exterior y todo el contenido interior.
3. Si queda vacio, devolver `MissingInput`.
4. Si supera 50 caracteres, devolver `InputTooLong` sin llamar al encoder.
5. Detectar EAN/UPC solo con digitos ASCII y checksum valido.
6. Si cae a CODE128, exigir cada caracter entre `U+0000` y `U+007F`.
7. Calcular dimensiones:
   - EAN-8/EAN-13/UPC-A: `340 x 56`.
   - CODE128: `max(500, longitud * 18) x 60`.
8. Observar cancelacion y llamar al encoder una sola vez.
9. Verificar firma PNG, `IHDR`, limites y CRC de bloques, presencia de datos, `IEND`, dimensiones, longitud no cero y limite de 1 MiB.
10. Devolver `Success` o `GenerationFailed`; nunca bytes parciales.

## Checksum contract

Para los digitos anteriores al verificador:

- EAN-8: pesos `3,1,3,1,3,1,3`.
- EAN-13: pesos `1,3,1,3,1,3,1,3,1,3,1,3`.
- UPC-A: pesos `3,1,3,1,3,1,3,1,3,1,3`.
- Verificador: `(10 - (suma % 10)) % 10`.

Un verificador invalido nunca modifica el texto: selecciona CODE128.

## Result contract

| Estado | Valor/tipo/dimensiones | Bytes |
|---|---|---|
| `Success` | Presentes y coherentes | Copia PNG no vacia |
| `MissingInput` | Ausentes/cero | Copia vacia |
| `UnsupportedValue` | Ausentes/cero | Copia vacia |
| `InputTooLong` | Ausentes/cero | Copia vacia |
| `GenerationFailed` | Ausentes/cero | Copia vacia |

Dos llamadas a `GetImageBytes()` deben devolver contenido igual pero referencias distintas. Modificar una copia no altera el resultado ni copias posteriores.

Cada estado expone un mensaje amigable estable. Los mensajes de fallo no contienen entrada, fragmentos, bytes, excepciones ni detalles tecnicos.

## ZXing mapping

| Dominio | ZXing |
|---|---|
| `Ean8` | `BarcodeFormat.EAN_8` |
| `Ean13` | `BarcodeFormat.EAN_13` |
| `UpcA` | `BarcodeFormat.UPC_A` |
| `Code128` | `BarcodeFormat.CODE_128` |

Opciones obligatorias: ancho y alto calculados, `Margin = 0`, primer plano negro, fondo blanco y sin texto decorativo. No se fuerza UTF-8 para CODE128.

## Diagnostics contract

Eventos de Business/Data pueden contener:

- etapa (`normalize`, `classify`, `encode`, `serialize`, `validate-output`);
- tipo elegido cuando exista;
- categoria de fallo;
- longitud normalizada.

No pueden contener entrada completa o parcial, PNG/base64, excepcion serializada, ruta de archivo, producto o datos de licencia. Una cancelacion solicitada no se registra como error.

## Composition contract

El composition root registra:

- `IBarcodeImageEncoder -> ZxingBarcodeImageEncoder`;
- `IBarcodeService -> BarcodeService`.

Las Views y ViewModels futuros solo resuelven `IBarcodeService`. Ningun proyecto distinto de Data y `SysTools.Barcodes.Tests` referencia ZXing.Net.
