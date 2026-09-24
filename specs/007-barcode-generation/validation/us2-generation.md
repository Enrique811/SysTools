# Validacion US2: generacion

Fecha: 2026-09-23

- Las pruebas de Data, servicio y round-trip quedaron verdes: 14 casos en la suite de barcodes y 2 casos de composicion/arquitectura en Presentation.
- EAN-8, EAN-13 y UPC-A producen PNG de `340 x 56`; CODE128 produce `max(500, longitud * 18) x 60`.
- El encoder usa margen cero, fondo blanco y barras negras.
- Un lector independiente recupera el formato y texto exactos. Para respetar el margen cero heredado, la prueba coloca el artefacto sobre un lienzo blanco externo antes de decodificar, sin alterar los bytes ni dimensiones producidos.
- La composicion resuelve `IBarcodeService` e `IBarcodeImageEncoder`; Presentation no genera codigos directamente.

Resultado: PASS.
