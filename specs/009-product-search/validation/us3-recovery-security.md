# Validación US3 — recuperación y seguridad

- PASS: cancelar/deactivar invalida generación, cancela CTS y descarta respuesta no cooperativa tardía.
- PASS: fallo operativo del catálogo invalida la sesión vigente, cierra el diálogo y deja Retry habilitado.
- PASS: entrada inválida y sin resultados mantienen la ventana utilizable.
- PASS: auditorías de logs no encuentran criterio, código ni descripción; solo Stage, Status, Category y DurationMs en Business.
- PASS: cerrar/desactivar el módulo solicita cierre seguro del diálogo activo.
