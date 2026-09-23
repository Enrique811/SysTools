# US3 - Label queue validation

**Commands**:

- `dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --filter FullyQualifiedName~Labels --no-restore`
- `dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --filter FullyQualifiedName~LabelQueueReliabilityTests --no-restore --logger "console;verbosity=detailed"`

**Result**: PASS, 27 label tests; reliability subset 3/3 passed.

Reliability measurements on this machine:

- 1 column: 100 operations, p95 0.000900 ms.
- 2 columns: 200 operations, p95 0.001000 ms.
- 3 columns: 300 operations, p95 0.001000 ms.

Validated immutable label snapshots, null-price rejection, exact Unicode/spacing, positions 1..3, ordered completion, reset after every row, capacity validation/change rejection, detached read-only collections and 100 complete rows per capacity with no loss or duplication.
