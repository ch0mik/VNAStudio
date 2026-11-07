# Stage 6.10a review — named comparison snapshot format

**Result:** PASS

## Implemented

- Added a named immutable snapshot wrapper that deep-copies measurement points and profile/calibration collections into read-only collections.
- Added `.vnasnapshot.json` read/write with an independent format version and the complete validated session as its payload.
- Nested sessions pass through `MeasurementSessionFile` validation and migration; stored frequencies and samples are not resampled or reinterpreted.
- Asynchronous read/write APIs propagate `CancellationToken`. Malformed outer or nested JSON is localized in English and Polish.

## Review

Subagent review passed. It confirmed the snapshot collections are frozen, nested session validation/migration is reused, and cancellation is preserved. A review finding for malformed/empty JSON was corrected before the final pass.

## Verification

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution.
- `git diff --check` passed with only existing line-ending warnings.
- No tests, builds, or device connections were run.
