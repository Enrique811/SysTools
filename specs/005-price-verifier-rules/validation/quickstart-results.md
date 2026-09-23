# Quickstart results

Executed on 2026-09-23 from the repository root.

| Step | Command | Result |
|---|---|---|
| Restore | `dotnet restore SysTools.sln` | PASS; all nine projects restored or already current |
| Build | `dotnet build SysTools.sln --no-restore` | PASS; 0 warnings, 0 errors |
| Feature tests | `dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --no-build --no-restore` | PASS; 72/72 |
| Full regression | `dotnet test SysTools.sln --no-build --no-restore` | PASS; 228 passed, 6 opt-in Firebird tests skipped, 0 failed |
| Business references | `dotnet list src/Business/SysTools.Business.csproj reference` | Only `../Entities/SysTools.Entities.csproj` |
| Entities references | `dotnet list src/Entities/SysTools.Entities.csproj reference` | No project references |

The three-column manual scenario is covered deterministically by `LabelQueueCaptureTests` and `LabelQueueCancellationTests`: ordered `001/002/003`, exact Unicode information, empty pending state after completion, discarded count after cancellation, and restart at position 1 with a new capacity.
