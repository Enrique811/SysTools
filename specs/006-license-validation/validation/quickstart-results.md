# Quickstart results

Date: 2026-09-23

| Scenario | Result | Evidence |
|----------|--------|----------|
| Build and full regression | PASS | 0 warnings/errors; 322 passed, 6 gated skips |
| Compatibility and cryptography | PASS | both issuers, exact payload, mutation matrix |
| Source and parsing | PASS | direct JSON, file, 64 KiB, UTF-8 and cancellation |
| Hardware and server time | PASS | fixed commands, fallback, UUID and inclusive boundaries |
| Security and architecture | PASS | 3 security + 15 architecture/composition tests |
| Scope review | PASS | targeted searches returned no prohibited implementation |

No real Firebird connection, production license, real UUID command execution or WPF startup was required.
