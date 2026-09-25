# Validación de fundamentos

- PASS: `PriceVerifierSearchItem` exige código, limita código a 50 y descripción a 255, normaliza descripción nula y existencia vacía.
- PASS: `PriceVerifierSearchResult` aplica invariantes por estado y conserva un snapshot de solo lectura.
- PASS: `IPriceVerifierWorkflow.SearchAsync` no expone configuración ni tipos de infraestructura.
- PASS: el resultado modal solo admite código en el outcome `Selected`.
- Evidencia: `PriceVerifierSearchResultTests` y compilación completa sin advertencias.
