# Quickstart results

- Automatizado: restore/build/test PASS; 688 passed, 0 failed, 7 controlled skips.
- Preview: PASS para 1/2/3, una salida por fila, cero spool y barcode decodificable.
- Producción lógica: PASS con backend controlado, destino exacto, sin preview/fallback y sin duplicados.
- Fallos/retry: PASS para template, render, printer, cancelación y outcome unknown.
- Seguridad/arquitectura: PASS para sentinels, logs, DI y dependencias.
- Producción física: SKIPPED conforme al protocolo; templates no aprobados impiden envío real.
- Integridad: PASS; los tres templates coinciden con hashes SHA-256 fijados y una alteración se rechaza.
