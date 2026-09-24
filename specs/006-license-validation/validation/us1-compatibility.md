# US1 compatibility validation

Date: 2026-09-23

- Red gate: Compatibility tests initially failed because parser, verifier and service did not exist.
- Green gate: 19 Compatibility tests passed before the mutation matrix was added.
- Fixed non-secret vectors use UUID `00112233-4455-6677-8899-AABBCCDDEEFF` and dates in 2026.
- Both legacy public keys authenticate their corresponding signatures as `DistributorColombia` and `Developer`.
- Payload bytes are UTF-8, use `LF`, have no trailing newline and canonicalize boundaries to `00:00:00` / `23:59:59`.
- Altering payload or signature yields `InvalidSignature` before UUID or server access.
- Repository search found no private-key material in production or test source.
