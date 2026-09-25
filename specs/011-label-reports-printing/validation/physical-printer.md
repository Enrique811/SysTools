# Physical printer validation

## Estado

`SKIPPED` — no hay impresora física controlada ni operador presente en este entorno. La omisión es explícita y no equivale a PASS.

La impresión productiva permanece bloqueada porque los tres manifests distribuidos tienen `productionApproved=false`.

## Protocolo opt-in

1. Usar exclusivamente una impresora no productiva, papel de prueba y operador presente.
2. Marcar toda muestra como `TEST / NO VENDER`.
3. Confirmar nombre exacto, 300 DPI y papel de 57 × 40 mm por celda.
4. Ejecutar una fila de 1, 2 y 3 columnas; esperar exactamente tres jobs y ningún diálogo/fallback.
5. Medir dimensiones, comparar contra muestra heredada anonimizada y escanear cada barcode.
6. Registrar dispositivo, driver, resultados y aprobación antes de cambiar `productionApproved`.

El test `PhysicalPrinterIntegrationTests` queda `SKIPPED` para impedir impresiones accidentales desde CI.
