# Validacion US3: fallos y seguridad

Fecha: 2026-09-23

- 32 pruebas de entradas, fallos, cancelacion y logging quedaron verdes.
- Nulo, vacio y solo caracteres Java-trim producen `MissingInput`; 51 caracteres producen `InputTooLong`; contenido no ASCII produce `UnsupportedValue`.
- Cada rechazo evita invocar el encoder y devuelve cero bytes con un mensaje amigable estable.
- Se rechazaron PNG vacio, firma o dimensiones incorrectas, truncamiento, CRC invalido, ausencia de `IEND` y tamano mayor de 1 MiB.
- Excepciones recuperables se convierten en `GenerationFailed`; `OperationCanceledException` se propaga antes, durante y despues del encoder.
- Los registros contienen solo etapa, tipo, categoria y longitud; no contienen codigo, fragmentos, PNG ni base64.

Resultado: PASS.
