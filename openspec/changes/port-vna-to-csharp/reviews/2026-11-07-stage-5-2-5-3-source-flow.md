# Stages 5.2, 5.3 and 11.10 — device acquisition and source flow evidence audit

## 5.2 — DDS driver

- `Vna65MHzMeasurementSource` derives start and step tuning words from the profile's configurable DDS clock and writes the recovered four ASCII commands.
- `Vna65MHzMeasurementSourceTests` checks command bytes, DDS-grid frequencies, 4/6-byte frame selection, ADC decoding/conversion and the recorded 50 Ω sample vector.
- Physical COM9 scan framing and application-flow results are summarized in `2026-11-04-hardware-com9.md` and `docs/DEVICE-PROTOCOL.md`. These establish acquisition, not calibrated RF accuracy.

## 5.3 — UI source and scan flow

- UI source/port selection, pass-through of the opaque port path, validation, source locking, cancellation and no fallback are covered by `DeviceSourceSelectionTests` and `Vna65MHzMeasurementSourceTests`.
- Continuous/hold/single-scan behavior is covered by `PlotInteractionTests`.
- The historical COM9 report records a completed 649-point scan, Stop during serial read, and a successful subsequent scan. No new hardware access was done for this audit.

## 11.10 — source architecture

- Demonstration, plugin-backed hardware, and session playback are explicit source modes.
- `device-plugin-architecture` stages 3.2b, 3.4 and 4.4 verify capability gating, cancellation/disposal, playback without an installed plugin, and no hardware-to-demo fallback.

## Verification

- Targeted Release test run for the DDS source, device selection, and scan interactions: **67 passed**.
- Independent reviewer verdict: **PASS**. The historical COM9 Stop/restart run predates the plugin refactor and was not repeated; current source-flow and plugin lifecycle tests cover the refactored path offline.
- No physical device or WOB was accessed in this stage.
- Remaining hardware, calibration, original-application comparison and RF accuracy tasks remain open under their dedicated checklist items.
