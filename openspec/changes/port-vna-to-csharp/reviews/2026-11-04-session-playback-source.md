# Saved-session playback source review

Date: 2026-11-04.

## Reviewed

- Added `SessionPlaybackDevice` as a third routed source beside Demonstration and Hardware.
- Loading a compatible session passes its original points and sweep to playback. A successful live sweep also becomes the latest in-memory playback session.
- Playback requires an explicit Connect and Start scan. It accepts only the saved sweep's exact `SweepSettings`, emits its original points in order, honors cancellation, and never interpolates.
- Source selection persists as `playback`; controls and hints are localized in Polish and English. The serial port controls only appear for Hardware.
- Session playback does not open a COM port. Opening a session still immediately displays the saved plot without silently changing the selected source.

## Review findings and disposition

1. A frequency-range or point-count mismatch fails the sweep, and the view model's existing atomic commit keeps the prior completed plot.
2. Playback rejects a session with a different device profile and cannot connect before a compatible session is loaded.
3. The generic router forwards session updates only when a playback implementation is configured; existing demo/hardware-only tests remain supported.
4. No generated or interpolated point is described as an original measurement. The source label distinguishes playback from hardware and demo data.

## Verification

- `dotnet test --no-restore -p:UsedAvaloniaProducts=`: 119 passed, including exact replay and mismatched-range rejection.
- `openspec validate port-vna-to-csharp --strict`: passed after the playback task and review were added.
- NuGet vulnerability metadata remains unavailable from this environment (NU1900); cached dependencies build successfully.

## Remaining hardware scope

Playback tests validate the session-source contract only. They do not establish serial protocol compatibility, calibration accuracy, or physical measurement agreement.
