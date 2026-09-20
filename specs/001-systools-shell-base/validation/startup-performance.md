# Rendimiento de arranque

Entorno: Windows 11 Home x64 10.0.22631, 12 procesadores lógicos, 16,329,916,416 bytes de RAM y proyecto en almacenamiento local. Build Release, .NET 10.0.401 local.

Método: iniciar el apphost con `DOTNET_ROOT=.dotnet`, medir con `Stopwatch` hasta que el proceso publique un `MainWindowHandle` cuyo título sea `SysTools`, cerrar normalmente y repetir diez veces.

Resultados (ms): 1625, 902, 807, 742, 803, 749, 784, 761, 765, 771. Promedio: 870.9 ms. Máximo: 1625 ms.

Resultado: 10/10 por debajo de 2 segundos.
