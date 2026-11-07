# Stage 1.6 review — VNA 1–65 MHz protocol contract

## Result

**PASS.** The independent review verified that the bilingual communication guides and code map document the user's device protocol and separate verified behavior from unknown firmware features.

The sources cover the OS-provided port path, serial settings, reflection command order and bytes, DDS word calculation and sample-frequency convention, ADC frame size/encoding/conversion, read-timeout semantics, cancellation/Stop behavior, and the unsupported or unverified SVNA, FILTER, Generator, and firmware paths. The linked COM9 report records physical reflection scans, Stop/restart, and 50 Ω observations. It does not claim absolute RF calibration or reactance-sign accuracy.

## References and scope

- `docs/VNA-COMMUNICATION.md` and `docs/VNA-COMMUNICATION.pl.md`
- `docs/DEVICE-PROTOCOL.md`
- `docs/measurements/2026-11-04-com9-50ohm.md`
- `src/Vna.Device.Vna65MHz/Vna65MHzMeasurementSource.cs`

No new device operation was performed for this documentation review; no WOB connection was used.
