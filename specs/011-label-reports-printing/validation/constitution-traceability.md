# Constitution and requirements traceability

## Functional requirements

- FR-001..010: PASS — comandos visibles/teclado, producto privado, snapshot, campos, barcode, capacidades, progreso, cancelación y configuración congelada.
- FR-011..019: PASS — routing preview/producción, template administrado, impresora exacta, single-flight, busy, resultados tipados, retry y preview accesible.
- FR-020: GATED — compatibilidad lógica PASS; aprobación física SKIPPED y `productionApproved=false` conserva el bloqueo obligatorio.
- FR-021..024: PASS — evaluación FastReport documentada, fallback nativo sustituible y fronteras de capas verificadas.
- Integridad de plantillas: PASS — el manifest distribuido fija SHA-256 por template y el proveedor rechaza ausencia o alteración antes de renderizar.
- FR-025..030: PASS — logs seguros, mensajes controlados, teclado/texto, 1280×720, reutilización de reglas y exclusiones de alcance.

## Success criteria

- SC-001..005: PASS — matrices 100/50, routing, estados y barcode final.
- SC-006: PASS automatizado para campos/geometría; comparación física queda GATED.
- SC-007..010: PASS — 20 cancelaciones, sentinels, accesibilidad y decisión de motor; impresión productiva no habilitada.

## Constitución

- Arquitectura 3 capas + Entities: PASS.
- WPF/MVVM: PASS.
- Migración incremental Spec Kit: PASS.
- Compatibilidad y mejora del legado: PASS con gate físico conservador.
- Seguridad/logging/configuración: PASS.

La implementación de la feature está cerrada; la activación productiva es una decisión operativa posterior y requiere completar el protocolo físico.
