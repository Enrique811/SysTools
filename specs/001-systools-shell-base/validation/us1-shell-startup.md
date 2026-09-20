# Validación US1 — arranque de shell

Fecha: 2026-09-20. Configuración: Release, Windows 11 x64, .NET SDK/runtime local 10.0.401.

- Se realizaron 10 arranques consecutivos con `DOTNET_ROOT=.dotnet`.
- En los 10 casos la primera ventana publicada tuvo título `SysTools`; no existe `StartupUri`, landing ni ventana intermedia.
- Tiempos hasta `MainWindowHandle` con título `SysTools`: 1625, 902, 807, 742, 803, 749, 784, 761, 765 y 771 ms.
- El catálogo automatizado confirma siete entradas, `price-verifier` como única habilitada/seleccionada y contenido `PriceVerifierViewModel` no nulo.
- Las otras seis entradas tienen `IsEnabled=false` y una prueba confirma que no sustituyen `ActiveModule`.

Resultado: aprobado (10/10).
