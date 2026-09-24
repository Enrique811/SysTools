# US3 hardware and server-time validation

Date: 2026-09-23

- Red gate: tests failed before the Windows hardware provider and command contracts existed.
- Green gate: Hardware/Validity/Cancellation filter passed 19 tests.
- PowerShell is attempted first with fixed non-interactive arguments; System32/Sysnative WMIC candidates provide fallback.
- Output is accepted only as canonical UUID and normalized to uppercase; raw output is never logged.
- Missing or different UUID stops before the server repository.
- Start and end boundaries are inclusive; before/after produce `NotYetValid`/`Expired`.
- Repository failures become `ServerTimeUnavailable`; cancellation is propagated.
- No licensing production source references `DateTime.Now` or `DateTime.UtcNow`.
