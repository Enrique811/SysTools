# Full regression

**Command**: `dotnet test SysTools.sln --no-build --no-restore`

**Result**: PASS.

| Suite | Passed | Skipped | Failed |
|---|---:|---:|---:|
| SysTools.BusinessRules.Tests | 72 | 0 | 0 |
| SysTools.Presentation.Tests | 27 | 0 | 0 |
| SysTools.Firebird.Tests | 53 | 3 | 0 |
| SysTools.Repositories.Tests | 43 | 3 | 0 |
| SysTools.Configuration.Tests | 33 | 0 | 0 |
| **Total** | **228** | **6** | **0** |

The six skipped tests are pre-existing opt-in Firebird integration scenarios that require authorized environment variables/database access: three connection scenarios and three repository scenarios. No unit, architecture, composition or business-rule test was skipped.
