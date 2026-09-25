# Validación US1 — búsqueda

- PASS: repositorio usa `STARTING WITH` parametrizado, tamaño 255, sin concatenar `%` y con orden `DESCRIPCION, ID`.
- PASS: `%` y `_` se conservan literalmente en el parámetro.
- PASS: null, espacios y 256 caracteres no consultan catálogo; 1..255 sí son aceptados.
- PASS: se conserva orden y se formatea únicamente cuando existe precio.
- PASS: matriz automatizada de 50 prefijos y pares de casing, 100 consultas totales.
- PASS: diálogo con entrada TwoWay, Enter, progreso textual, tabla read-only y primera fila seleccionada.
- Pruebas: Repository 45 PASS; Business incluye `PriceVerifierSearchTests`/`ReliabilityTests`; Presentation incluye `ProductSearchViewModelTests`/`AccessibilityTests`.
