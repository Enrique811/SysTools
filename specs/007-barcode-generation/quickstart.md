# Quickstart: Validacion de generacion de codigos de barras

Esta guia se ejecuta despues de implementar `tasks.md`. No requiere abrir WPF, Firebird, licencia, reportes ni impresora.

## Prerequisitos

- Windows con .NET SDK 10.0.100 o compatible segun `global.json`.
- Restauracion NuGet disponible para `ZXing.Net.Bindings.Windows.Compatibility` 0.16.16.
- Directorio de trabajo en la raiz de `SysTools`.

## 1. Restaurar y compilar

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
```

Esperado: compilacion exitosa sin dependencias ZXing en Entities o Business.

## 2. Validar clasificacion y checksums

```powershell
dotnet test tests/SysTools.Barcodes.Tests/SysTools.Barcodes.Tests.csproj --no-build --filter "FullyQualifiedName~Compatibility"
```

Esperado:

- `96385074` se detecta como EAN-8.
- `4006381333931` se detecta como EAN-13.
- `036000291452` se detecta como UPC-A.
- checksums invalidos, otras longitudes y texto ASCII caen a CODE128 sin alterar el valor;
- una matriz de al menos 100 casos cubre al menos 20 ejemplos por cada formato numerico y 20 CODE128.

## 3. Validar PNG y round-trip

```powershell
dotnet test tests/SysTools.Barcodes.Tests/SysTools.Barcodes.Tests.csproj --no-build --filter "FullyQualifiedName~Data"
```

Esperado:

- EAN/UPC produce PNG `340 x 56`;
- CODE128 produce `max(500, longitud * 18) x 60`, nunca mas de `900 x 60`;
- margen configurado en cero, contraste negro/blanco y archivo menor o igual a 1 MiB;
- el lector recupera exactamente texto y tipo, incluidos ceros iniciales y espacios interiores.

## 4. Validar resultados y fallos controlados

```powershell
dotnet test tests/SysTools.Barcodes.Tests/SysTools.Barcodes.Tests.csproj --no-build --filter "FullyQualifiedName~Business|FullyQualifiedName~Entities"
```

Esperado:

- nulo, vacio y solo espacios devuelven `MissingInput`;
- 51 caracteres devuelven `InputTooLong` sin invocar Data;
- caracteres fuera de ASCII devuelven `UnsupportedValue`;
- fallo inyectado o PNG invalido devuelve `GenerationFailed` sin bytes;
- PNG truncado, CRC invalido o sin `IEND` devuelve `GenerationFailed`;
- cada fallo incluye un mensaje amigable sin contenido ni detalle tecnico;
- cancelacion antes, durante o al terminar el encoder se propaga;
- cada acceso a bytes entrega una copia independiente.

## 5. Validar seguridad y arquitectura

```powershell
dotnet test tests/SysTools.Barcodes.Tests/SysTools.Barcodes.Tests.csproj --no-build --filter "FullyQualifiedName~Security"
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --no-build --filter "FullyQualifiedName~Architecture|FullyQualifiedName~Composition"
rg -n "ZXing|System\.Drawing" src/Entities src/Business src/Presentation
rg -n "BarcodeWriter|BarcodeReader|EncodePng" src/Presentation
```

Esperado:

- logs contienen etapa/categoria pero no los centinelas de codigo ni bytes/base64;
- `IBarcodeService` e `IBarcodeImageEncoder` resuelven desde DI;
- las busquedas no encuentran uso de ZXing/System.Drawing fuera de Data ni generacion directa en Presentation.

## 6. Validar rendimiento y estabilidad

```powershell
dotnet test tests/SysTools.Barcodes.Tests/SysTools.Barcodes.Tests.csproj --no-build --filter "FullyQualifiedName~Performance"
```

Esperado: 1,000 generaciones distribuidas entre los cuatro tipos producen cero resultados corruptos o dimensiones incorrectas y al menos 95% termina en menos de 100 ms en el entorno controlado documentado por la prueba.

## 7. Ejecutar regresion completa

```powershell
dotnet test SysTools.sln --no-build
```

Esperado: todas las suites existentes y la nueva suite de barcode pasan sin requerir servicios externos.

## Trazabilidad

- Modelo e invariantes: [data-model.md](data-model.md)
- API y fronteras: [contracts/barcode-contract.md](contracts/barcode-contract.md)
- Decisiones tecnicas: [research.md](research.md)
- Requisitos funcionales: [spec.md](spec.md)
