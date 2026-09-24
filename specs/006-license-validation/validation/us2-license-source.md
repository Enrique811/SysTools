# US2 license source validation

Date: 2026-09-23

- Red gate: source tests failed before `FileLicenseSourceReader` existed.
- Green gate: the Source/Parsing filter passed 23 tests.
- Direct JSON performs zero source-reader calls; a path is trimmed and read exactly once.
- Absolute and relative UTF-8 files pass.
- Missing files, directories, invalid UTF-8, I/O/access failures and files over 64 KiB return controlled source failures.
- Cancellation is propagated; paths and exceptions do not enter the public result.
