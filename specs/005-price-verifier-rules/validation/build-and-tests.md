# Build and feature tests

## Build

**Command**: `dotnet build SysTools.sln --no-restore`

**Result**: PASS. Nine projects built, 0 warnings, 0 errors.

## Business rules suite

**Command**: `dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --no-restore`

**Result**: PASS. 72 passed, 0 failed, 0 skipped.

## Architecture subset

**Command**: `dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --filter FullyQualifiedName~Architecture --no-build --no-restore`

**Result**: PASS. 2 passed, 0 failed, 0 skipped.

The build emitted no new compiler or analyzer warnings. Business and Entities retain their permitted dependency direction.
