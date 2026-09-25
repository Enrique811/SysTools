# Regresión completa

- Fecha: 2026-09-24.
- `dotnet build SysTools.sln --no-restore`: PASS, 0 advertencias, 0 errores.
- `dotnet test SysTools.sln --no-build`: PASS, 582 aprobadas, 0 fallidas, 6 omitidas.
- Totales: BusinessRules 135; Firebird 53+3 skip; Licensing 91; Repositories 45+3 skip; Presentation 62; Configuration 33; Barcodes 163.
- `git diff --check`: PASS; únicamente avisos informativos de normalización LF/CRLF.
- Smoke de proceso WPF: PASS, el ejecutable permaneció activo durante 5 segundos y se cerró de forma controlada por la validación.
