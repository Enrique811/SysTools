# Preview routing validation

- PASS: capacidades 1, 2 y 3 renderizan PNG con dimensiones físicas a 300 DPI.
- PASS: 100 filas por capacidad conservan orden y producen una sola preview.
- PASS: ambiente `a` produce cero llamadas al spool.
- PASS: la configuración se congela al iniciar la fila y la fecha proviene del reloj de servidor.
- PASS: cada barcode se decodifica desde su celda final renderizada.
- PASS: plantilla ausente, incompatible o dañada y fallo de render producen estados recuperables.

Evidencia: `LabelOutputWorkflowTests`, `LabelPreviewWorkflowTests`, `NativeLabelDocumentRendererTests` y `RenderedBarcodeRoundTripTests`.
