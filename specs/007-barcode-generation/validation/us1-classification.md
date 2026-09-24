# Validacion US1: clasificacion

Fecha: 2026-09-23

Comando:

```powershell
dotnet test tests/SysTools.Barcodes.Tests/SysTools.Barcodes.Tests.csproj --no-build --filter "FullyQualifiedName~Compatibility|FullyQualifiedName~BarcodeClassifierTests"
```

- La matriz y el clasificador suman 111 casos verdes.
- Hay al menos 20 casos para EAN-8, EAN-13, UPC-A y CODE128.
- Se verificaron checksums validos e invalidos, fallback CODE128, digitos ASCII, digitos Unicode y ceros iniciales.
- La normalizacion retira solo `U+0000..U+0020` en ambos extremos y conserva contenido interior y caracteres exteriores superiores.

Resultado: PASS.
