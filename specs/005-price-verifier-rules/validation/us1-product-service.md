# US1 - Product service validation

**Command**: `dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --filter FullyQualifiedName~Products --no-restore`

**Result**: PASS, 13 tests, 0 failed, 0 skipped.

Coverage exercised:

- FR-001/FR-002: Business facade delegates textual barcodes without losing leading zeroes.
- FR-003/FR-004: missing and empty inputs are normal results; empty inputs make zero repository calls.
- FR-005: description prefixes trim outer whitespace and preserve result order/identity.
- FR-006: consumer cancellation and the same safe `RepositoryAccessException` propagate unchanged.

No test starts WPF or accesses Data/Firebird.
