# Quickstart: Validación de la shell base de SysTools

## Prerequisites

- Windows 10/11 x64.
- .NET 10 SDK y Desktop Runtime/soporte WPF. El entorno actual solo tiene .NET 8 y debe actualizarse antes de implementar.
- Pantalla mínima 1280 × 720; repetir inspección principal con escalado 125% si está disponible.

```powershell
dotnet --list-sdks
dotnet --version
```

Debe seleccionarse `10.0.x`; `global.json` no permite major mayor ni prerelease.

## Build and automated checks

```powershell
dotnet restore SysTools.sln
dotnet build SysTools.sln --configuration Debug --no-restore
dotnet test SysTools.sln --configuration Debug --no-build
```

Expected: código 0, sin warnings nullable; pruebas confirman un módulo habilitado/seleccionado, seis entradas futuras deshabilitadas y valores neutrales.

## Run

```powershell
dotnet run --project src/Presentation/SysTools.Presentation.csproj
```

Expected:

1. Abre directamente `SysTools`, sin bienvenida ni ventana secundaria.
2. Verificador de precios está seleccionado y visible.
3. Impresión de etiquetas, Inventario rápido, Nueva utilería, Configuración, Licencias y Logs del sistema están visibles/deshabilitados.
4. Buscar, Imprimir y Configuración del encabezado están visibles/deshabilitados.
5. Código de barras e Información adicional están visibles/deshabilitados y no reciben foco.
6. Conexión y Licencia muestran `No disponible`; el mensaje, `Módulo en preparación`.
7. Descripción, Presentación, Existencia y Precio muestran `—`, no datos ficticios ni `$0`.

## Manual visual validation

Comparar con `docs/mockup-shell-utilerias.svg`:

- Sidebar oscuro, área clara, tarjetas blancas, bordes suaves y azul de acento.
- Las seis zonas requeridas se distinguen en menos de 30 segundos.
- A 1280 × 720 no hay solapamientos, recortes esenciales ni scroll horizontal global.
- Maximizada, aprovecha espacio sin deformar sidebar.
- La tabulación omite Código de barras, Información adicional y demás controles deshabilitados; cualquier elemento enfocable conserva foco visible.
- Estados con texto, no solo color.
- Al 125% de escalado, textos legibles y controles sin superposición.

## Placeholder behavior

1. Intentar enfocar o escribir en Código de barras e Información adicional por mouse y teclado.
2. Presionar Enter, Ctrl+F7, Ctrl+F8, Ctrl+F9 y Ctrl+F10.
3. Intentar activar las entradas deshabilitadas por mouse y teclado.

Expected: los campos no reciben foco ni texto; no hay consulta, búsqueda, configuración, impresión o cancelación; el módulo no cambia; no aparece producto/precio; no se crean configuración, reportes o etiquetas.

## Logging validation

Tras iniciar/cerrar:

```powershell
$logRoot = Join-Path $env:LOCALAPPDATA 'SysTools\Logs'
Get-ChildItem -LiteralPath $logRoot
$latestLog = Get-ChildItem -LiteralPath $logRoot -Filter 'systools-*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-Content -LiteralPath $latestLog.FullName
```

Expected: archivo diario con inicio, carga y cierre; sin secretos/datos simulados; retención configurada a 14 archivos.

## Architecture validation

```powershell
dotnet list src/Presentation/SysTools.Presentation.csproj reference
dotnet list src/Business/SysTools.Business.csproj reference
dotnet list src/Data/SysTools.Data.csproj reference
dotnet list src/Entities/SysTools.Entities.csproj reference
rg -n "SysTools\.Data|Firebird|FbConnection|PrintDialog|ZXing|FastReport" src/Presentation --glob '*.cs' --glob '*.xaml'
```

Expected: Presentation → Business/Entities/Data (Data solo en composition root); Business → Entities; Data → Business/Entities; Entities → ninguna capa. No hay infraestructura en Views/ViewModels.

## Recoverable error validation

Usar un seam de prueba para simular un fallo recuperable durante la inicialización del módulo inicial; no agregar una opción oculta de producción.

Expected: shell abierta con Verificador conservado o recuperado, mensaje comprensible sin detalles internos y log Error con excepción, `Stage=Initialization` y `ModuleId`.

## Scope guard

La validación falla ante conexión Firebird, configuración local, licencia real, búsqueda de productos, códigos de barras funcionales, reportes/impresión u otra utilería habilitada.
