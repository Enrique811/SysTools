# Integración controlada

- Comando: `dotnet test tests/SysTools.Repositories.Tests/SysTools.Repositories.Tests.csproj --no-build --filter Category=RepositoryIntegration`.
- Resultado: SKIPPED, 3 pruebas omitidas, 0 fallidas.
- Motivo: no están presentes las variables opt-in de una instancia Firebird autorizada.
- La prueba de referencia quedó alineada a `STARTING WITH`, parámetro literal de 255 y orden estable. No se registraron valores de conexión ni secretos.
