# Stage 2.3c.5 review — neutral processing provenance

## Review result

**PASS.** `MeasurementCalibrationSnapshot.Capture` no longer accepts or reads `DeviceProfile`. The ViewModel provides the active analysis/source state and the source's optional neutral `AppliedReflectionCalibration` snapshot. Hardware reflection data without a report stays `Unknown`; VNA65 reports the parameters it applies as `ConfiguredUnverified`. This metadata change does not alter measurement samples.

The dependency map distinguishes plugin-owned acquisition/calibration settings from legacy session snapshot validation. The review found no stale claim in the touched scope.

## Static checks

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution.
- `openspec validate device-plugin-architecture --strict` passed.
- No tests, build, application run, or hardware connection was performed.

## Remaining boundary

Core and IO still retain legacy `DeviceProfile` fields to read, validate, and preserve old session snapshots. The transitional `IVnaDevice`/UI compatibility profile also remains; this substage removes it as the source for new processing provenance but does not complete the full legacy model removal in task 2.3c.
