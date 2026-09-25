# Auditoría de dependencias y arquitectura

- PASS: no se modificó ningún `.csproj`, paquete o proyecto.
- PASS: Presentation consume `IPriceVerifierWorkflow`; no referencia repositorios, Firebird, archivos, licencia, criptografía, ZXing, reportes o impresión desde Search.
- PASS: DI registra workflow singleton, servicio modal singleton y `ProductSearchViewModel` transitorio.
- PASS: el cambio Data se limita al operador literal, parámetro y prueba de integración de referencia.
- PASS: guards arquitectónicos de la regresión completa.
