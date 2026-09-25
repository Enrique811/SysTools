# Controlled integration

**Result**: SKIPPED — 2026-09-24

Se ejecutaron los filtros `Category=FirebirdIntegration` y `Category=RepositoryIntegration`. El entorno no contiene las variables opt-in/autorización necesarias, por lo que las seis pruebas se omitieron de manera controlada:

- Firebird: 0 passed, 3 skipped, 0 failed.
- Repositories: 0 passed, 3 skipped, 0 failed.

No se imprimieron ni persistieron credenciales. La omisión está permitida por quickstart; las pruebas deterministas con dobles sí pasaron.
