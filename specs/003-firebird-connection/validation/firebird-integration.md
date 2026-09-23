# Integración Firebird real

Fecha: 2026-09-23

Estado operacional: PENDIENTE / SKIPPED.

La comprobación segura indicó `INTEGRATION_ENV_READY=False`; no se imprimió el
nombre ni el valor de ninguna variable sensible. Se ejecutaron los filtros:

```powershell
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Release --no-build --filter 'Category=FirebirdIntegration'
dotnet test tests/SysTools.Firebird.Tests/SysTools.Firebird.Tests.csproj -c Release --no-build --filter 'Category=FirebirdFailureIntegration'
```

Resultados:

- Integración general: 0 passed, 0 failed, 3 skipped.
- Fallos reales autorizados: 0 passed, 0 failed, 2 skipped.

Las pruebas quedan preparadas para diez aperturas válidas consecutivas, password
inválido dedicado y base inexistente dedicada. Los fallos no se habilitan sin
`SYSTOOLS_FIREBIRD_TEST_BAD_PASSWORD` y
`SYSTOOLS_FIREBIRD_TEST_MISSING_DATABASE`, evitando fabricar credenciales o rutas.

Esta evidencia confirma una omisión explícita, no validación operacional. No se
afirma conectividad real hasta ejecutar contra un entorno no productivo autorizado.
