# Auditoria de dependencias y arquitectura

Fecha: 2026-09-23

Comandos:

```powershell
dotnet list tests/SysTools.Barcodes.Tests/SysTools.Barcodes.Tests.csproj package --vulnerable --include-transitive
rg -n "ZXing|System\.Drawing" src/Entities src/Business src/Presentation
rg -n "BarcodeWriter|BarcodeReader|EncodePng" src/Presentation
```

- `ZXing.Net.Bindings.Windows.Compatibility` 0.16.16 queda confinado a Data y pruebas.
- El paquete declara licencia Apache-2.0 en sus metadatos restaurados.
- La auditoria NuGet no encontro paquetes vulnerables conocidos.
- Entities y Business no referencian ZXing/System.Drawing; Presentation no contiene generacion directa.
- Los guards automatizados de capas y composicion pasan.

Resultado: PASS.
