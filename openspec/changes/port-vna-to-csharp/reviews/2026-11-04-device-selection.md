# Device selection and connection stage review

Date: 2026-11-04.

## Reviewed

- Added a source router with explicit Demo/Hardware selection. A failed connection or sweep stays on the selected source and is surfaced as an error; there is no demo fallback.
- Persisted the source mode in application settings alongside the user-provided serial path. The UI disables source selection and port editing while connecting, scanning, or connected.
- Added explicit Connect/Disconnect commands. A user-connected device remains connected after a complete scan; a scan that owns its connection closes it on completion, cancellation, or error.
- Made async relay-command execution directly awaitable, while retaining normal `ICommand` execution for Avalonia.
- Kept device routing/profile compatibility explicit and disposal delegated to the routed sources.

## Review findings and disposition

1. A scan is accepted only after the requested point count and frequency sequence have been validated. Incomplete or failed scans keep the previously committed plot.
2. The router records a connection only after the chosen driver's connect operation succeeds. It never invokes the other source after failure.
3. Port paths are passed unchanged to the selected driver, including non-Windows serial paths.
4. The new headless test covers manual connect, selected port, port/source locking, a full sweep on the same connection, and explicit disconnect. A separate router test covers no fallback after connection failure.
5. No test in this stage establishes physical device communication. Protocol gaps and physical-device verification remain open in the roadmap.
6. Follow-up regression coverage also verifies that a sweep failure on the selected hardware source does not invoke the demo source or relabel the data.

## Verification

- `dotnet test --no-restore -p:UsedAvaloniaProducts=`: 123 passed in the current tree; the manual connection and no-fallback scenarios are covered.
- Build output includes NU1900 because NuGet vulnerability metadata is unavailable in this environment; dependencies were restored from cache.
- The Avalonia property disables a telemetry build target that otherwise attempts to write outside the permitted workspace.

## Follow-up

- Add replay as a source if the user-facing source-selection contract requires scanning saved measurements.
- Keep physical hardware, calibration and recovery verification separate from fake-transport and UI-state tests.
