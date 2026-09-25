# Regresión final

Fecha: 2026-09-25.

- `dotnet build SysTools.sln --no-restore`: PASS, 0 warnings, 0 errors.
- `dotnet test SysTools.sln --no-restore`: 632 PASS, 0 FAIL, 6 SKIPPED.
- Skips: 3 Firebird + 3 repositorios, todos opt-in y documentados en `controlled-integrations.md`.
- `git diff --check`: sin errores de whitespace; únicamente avisos informativos de normalización LF/CRLF.
