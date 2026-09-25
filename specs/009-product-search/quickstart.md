# Quickstart: Validación de búsqueda de productos

## Prerequisites

- Windows con el SDK indicado por `global.json`.
- Trabajo desde la raíz de SysTools.
- Para integración real opcional: entorno Firebird no productivo autorizado y variables opt-in existentes.

## 1. Restore and build

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --no-restore
```

Expected: cero errores/warnings y ningún paquete/proyecto nuevo.

## 2. Validate Business search

```powershell
dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --no-build --filter "FullyQualifiedName~PriceVerifierSearch"
```

Expected: límites 0/1/255/256, matriz de 50 prefijos, casing, precios opcionales, NoMatches, fallo/invalidation, cancelación, 100 búsquedas y logs sanitizados.

## 3. Validate repository prefix semantics

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj --no-build --filter "FullyQualifiedName~Search"
```

Expected: prefijo literal parametrizado con `%` y `_` tratados como texto, orden descripción/id, cancelación y errores seguros.

## 4. Validate dialog and parent integration

```powershell
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --no-build --filter "FullyQualifiedName~ProductSearch|FullyQualifiedName~PriceVerifier|FullyQualifiedName~DependencyInjection"
```

Expected: abrir/cancelar/seleccionar, Enter/doble clic equivalentes, consulta exacta única, single-flight, generaciones, foco, diálogo transitorio y bloqueo por fallo operativo.

## 5. Validate architecture, security and accessibility

```powershell
dotnet test tests/SysTools.Presentation.Tests/SysTools.Presentation.Tests.csproj --no-build --filter "FullyQualifiedName~Architecture|FullyQualifiedName~Security|FullyQualifiedName~Accessibility"
rg -n "SysTools.Data|FirebirdSql|FbConnection|System.IO|Cryptography|ZXing|FastReport|PrintDialog|ILabelQueueService" src/Presentation/Modules/PriceVerifier
```

Expected: cero infraestructura o features futuras en View/ViewModel; WPF STA carga y mide diálogo, valida comandos, nombres accesibles, estados y layout.

## 6. Controlled integration

```powershell
dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj --no-build --filter "Category=RepositoryIntegration"
```

Expected con entorno autorizado: prefijos reales, acentos/casing, caracteres especiales, nulls, decimales, orden y encoding coinciden con consulta de referencia. Sin entorno, registrar SKIPPED.

## 7. Manual walkthrough

```powershell
dotnet run --project src/Presentation/SysTools.Presentation.csproj --no-build
```

1. Preparar el módulo con entorno de prueba autorizado.
2. Abrir Buscar con botón y Ctrl+F7; confirmar foco inicial.
3. Buscar por prefijo con teclado y revisar Código, Descripción, Precio y Existencia.
4. Repetir con acentos, casing y símbolos literales; verificar lista/orden.
5. Seleccionar con Enter y doble clic; confirmar una consulta exacta, diálogo cerrado y código enfocado/seleccionado.
6. Probar vacío, 256 caracteres, sin coincidencias y doble Enter.
7. Cerrar durante una búsqueda demorada; verificar cero publicación tardía.
8. Provocar fallo controlado, confirmar bloqueo/Retry y recuperación.
9. Repetir a 1280x720, maximizado y 125% DPI; recorrer con Tab/Shift+Tab/Escape.

No registrar criterios, productos, códigos, rutas, licencia ni credenciales.

## 8. Full regression

```powershell
dotnet test SysTools.sln --no-build
```

Expected: todas las suites deterministas pasan; únicamente integraciones externas no habilitadas pueden quedar skipped.

## Evidence to retain

- Conteos de restore/build/tests.
- Matrices de 50 prefijos y 100 búsquedas.
- Auditorías de seguridad/arquitectura/alcance.
- PASS/FAIL/SKIPPED de integración y recorrido visual.

## References

- [Specification](spec.md)
- [Data model](data-model.md)
- [Product search contract](contracts/product-search-contract.md)
- [Research decisions](research.md)
