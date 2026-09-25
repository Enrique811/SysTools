# Validación US2 — selección

- PASS: selección conserva códigos con ceros y se completa una sola vez mediante guardia atómica.
- PASS: Enter y doble clic convergen en `SelectCommand`.
- PASS: el padre asigna el código exacto y reutiliza `LookupAsync` exactamente una vez.
- PASS: cancelar conserva el producto anterior y solicita devolver el foco.
- PASS: 100 aperturas/selecciones secuenciales producen 100 diálogos y 100 lookups, sin duplicados; el producto final corresponde al número 100.
