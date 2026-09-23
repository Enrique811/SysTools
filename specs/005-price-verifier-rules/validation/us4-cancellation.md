# US4 - Cancellation validation

**Command**: `dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --filter FullyQualifiedName~LabelQueueCancellationTests --no-restore`

**Result**: PASS, 3 tests, 0 failed, 0 skipped.

Validated empty idempotent cancellation, discarded counts for partial rows, restart at position 1 with a different capacity, and independence of an already returned completed row.
