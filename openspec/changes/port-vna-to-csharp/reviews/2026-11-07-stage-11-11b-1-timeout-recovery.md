# Stage 11.11b.1 — offline timeout and partial-frame recovery

## Changes

- Added a simulated one-shot serial timeout after copying a two-byte prefix into the frame buffer.
- Verified that a failed sweep closes the transport, and a later explicit connection starts a fresh sweep and completes from a newly delivered frame.
- Added a truncated-frame scenario that copies six of eight expected bytes. The driver throws before yielding any sample and closes the transport.
- Kept the parent 11.11b open: timeout/reconnect behavior has not been measured on the exact firmware, and no device was accessed during this stage.

## Verification

- `Vna65MHzMeasurementSourceTests`: **11 passed** in Release.
- No COM port or WOB was used.
- Independent reviewer verdict: **PASS**. The fixtures deliver partial prefixes before timeout/EOF; source closes and discards the frame, and a new explicit connection produces a complete sweep. Exact-firmware timeout behavior remains open.
