# Dependency and architecture audit

- PASS: FastReport y otros motores comerciales no fueron agregados.
- PASS: Entities conserva modelos e invariantes sin WPF, Data ni serialización.
- PASS: Business usa puertos para template, renderer y printer; no referencia Data/WPF/System.Drawing/ZXing.
- PASS: Presentation no enumera impresoras, imprime, genera barcodes ni abre archivos de plantilla.
- PASS: Data contiene JSON administrado, SHA-256, raster y spool Windows.
- PASS: workflow, puertos y preview dialog son singleton; backend físico es sustituible en pruebas.
- PASS: templates se copian como contenido y permanecen no aprobados para producción.

Validado por `LayerDependencyTests`, `DependencyInjectionTests` y build con cero warnings.
