# Stage 5.1c — offline serial stream simulator

## Changes

- Extracted exact-length byte stream reading from `SerialPortTransport` into `SerialStreamReader` so its transport behavior can be tested without opening a system serial port.
- Preserved the transport's validation order, zero-length no-op, partial-read loop, timeout mapping, caller cancellation and serial-port disposal behavior.
- Added tests for fragmented reads, unread surplus bytes, timeout, cancellation and EOF after a partial frame.
- Documented that the simulator tests the host's expected payload length; extra firmware bytes and any physical resynchronization behavior remain unverified.

## Verification

- `SerialStreamReaderTests` and `Vna65MHzMeasurementSourceTests`: **13 passed** in Release.
- Reviewer verdict: **PASS**. A wording note about “length-delimited” was corrected to describe the host's expected payload size; reviewer confirmed the corrected wording.
- No serial device or WOB was accessed.
