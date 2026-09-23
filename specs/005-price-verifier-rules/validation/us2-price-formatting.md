# US2 - Price formatting validation

**Command**: `dotnet test tests/SysTools.BusinessRules.Tests/SysTools.BusinessRules.Tests.csproj --filter FullyQualifiedName~Pricing --no-restore`

**Result**: PASS, 27 tests, 0 failed, 0 skipped.

Validated:

- Ten MX cases with `$`, invariant decimal point and exactly two decimals.
- Ten CO/fallback cases with explicit `es-CO` currency and zero decimals.
- `MidpointRounding.ToEven` for positive and negative midpoints.
- Identical output while current culture/UI culture changes among `en-US`, `es-MX` and `es-CO`.

The table exceeds the 20-case target in SC-002.
