# Validación US2 — layout del verificador

La revisión contra `docs/mockup-shell-utilerias.svg` confirma sidebar oscuro, superficie principal clara, tarjetas blancas, acento azul y jerarquía sobria. La vista usa `Grid`, columnas relativas, wrapping y mínimos de ventana; no usa `Canvas` ni coordenadas absolutas.

Escenarios revisados:

- 1280 × 720: las seis zonas permanecen dentro de la shell y la barra inferior pertenece exclusivamente a `ShellWindow`.
- Pantalla completa: el contenido y las tarjetas crecen mediante columnas `*`, sin estirar el sidebar.
- Escalado Windows 125%: WPF trabaja en unidades independientes del dispositivo; el mínimo 1280 × 720, wrapping y scroll del menú conservan acceso al contenido.
- Barcode e información adicional son visibles, `IsEnabled=false`, `Focusable=false`/`IsTabStop=false` por estilo.
- Buscar, Imprimir y Configuración son visibles y deshabilitados; no hay comandos ni atajos funcionales.

Las pruebas automatizadas confirman valores vacíos/inmutables, guiones neutrales y las cinco disponibilidades en `false`.

Resultado: aprobado.
