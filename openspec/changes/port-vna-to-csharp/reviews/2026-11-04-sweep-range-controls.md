# Sweep-range controls review

Date: 2026-11-04.

## Evidence

The recovered Analyser 306b strings identify `cmdFullSweep` with the tooltip “Full range scan” and `cmdZoomSweep` with “Zoom to marker”. These are separate controls from `chkMkrFull` and from plot-view zoom. The command handlers are not recovered, so this implementation does not start a hardware scan automatically.

## Change

- Added a full-scan-range action that sets start/stop to the selected device profile limits.
- Added a marker-range action that sets scan start/stop to the lower/upper marker frequencies, retaining Hz precision and rejecting unavailable or out-of-profile markers.
- Kept the existing chart zoom, full-view, profile-view, and save/recall view-range commands independent.
- Both new actions are localized and disabled while scanning. The user explicitly starts the scan after changing limits.

## Verification

- `dotnet test --no-restore -p:UsedAvaloniaProducts=`: 122 passed, including reversed marker ordering and profile limits.
- Exact original command-handler timing could not be confirmed from the recovered pseudocode; no automatic rescan is assumed.
