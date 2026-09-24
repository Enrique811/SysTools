# Foundation validation

Date: 2026-09-23

- Red gate: `dotnet test tests/SysTools.Licensing.Tests/SysTools.Licensing.Tests.csproj --no-restore` failed because the licensing entities did not exist.
- Green gate: the same command passed 13/13 tests after adding the entities and Business contracts.
- `LicenseValidationResult` exposes only status, controlled message, issuer and temporal values.
- Reflection assertions confirm that no public member exposes UUID, hardware ID, signature, path, content, configuration, exception, password or connection string.
- The test project is part of `SysTools.sln`; `.gitignore` retains existing patterns and now excludes private-key containers.
