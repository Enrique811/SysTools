# Security audit: local configuration

**Date**: 2026-09-23

## Secret protection

- `DpapiSecretProtector` uses Windows DPAPI with `DataProtectionScope.CurrentUser`.
- Two protections of the same sentinel produce different ciphertexts; both round-trip for the current user.
- The binary DPAPI output is Base64-encoded only for JSON transport.
- `StoredConfigurationDocument` exposes `passwordProtegido`; it has no plaintext `password` property.
- `AppConfiguration.ToString()` returns only the type name and cannot enumerate the password or license.

Result: PASS.

## Persistence and temporary files

- Twenty round-trips searched the serialized JSON for each plaintext password and found no match.
- The complete document is serialized only after protection, so the atomic writer never receives a plaintext-password document.
- Writes use a unique temporary file in the destination directory and replace only after flush.
- Cancellation and injected I/O/access failures preserve the previous destination byte for byte.
- The test temp root contained no files after cleanup.

Result: PASS.

## Logging and messages

- Security tests use distinct password, license, host, database-path and username sentinels.
- Captured Business/Data logs contain operation outcomes and issue metadata but none of the sentinels.
- Static audit found no configuration destructuring, direct configuration serialization or log call that passes Password/Licencia/IpEmpresa/RutaEmpresa.
- User-facing issues contain stable codes and safe messages, never caught exception text.

Result: PASS.

## Failure behavior

- Invalid protected data produces `SecretUnavailable` and no partially populated `AppConfiguration`.
- Malformed existing JSON is not silently replaced.
- AppData resolution, permissions, I/O and cancellation are differentiated into controlled results.
- A configuration failure does not prevent shell resolution.

Result: PASS.

## Boundary note

The license remains an opaque configuration value and is not encrypted by this feature; only the Firebird password is required to use DPAPI. The license value is nevertheless excluded from logs and diagnostics. License parsing, import and validation remain out of scope.

## Overall result

Approved: no tested path exposes the Firebird password, protected payload, license value or complete configuration in storage diagnostics.
