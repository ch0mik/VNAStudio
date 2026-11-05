# Legacy Hold toggle review

Date: 2026-11-04.

## Source evidence

- The supplied user manual says measurement starts after Hold is unchecked (`reference/hardware/manual-3.txt`, outside the project).
- Targeted Ghidra output for `VB6_0042c7ba` references the message `VNA measurment stopped... for start VNA measurment - press HOLD button !`.

## Change

- Removed the separate **Repeat scans** checkbox. **Hold** now controls the continuous scan loop: checking it stops after the active complete sweep, unchecking it starts or resumes continuous scans.
- **Start scan** remains a one-sweep action. **Stop** cancels an active sweep, turns Hold on and preserves the last complete result.
- The view model exposes an awaitable `HoldCommand`; scan loops continue to use the existing async device API and cancellation token. The Hold checkbox is beside the sweep range, and Start/Stop share a row so the controls remain visible with the refreshed 1440×900 layout.
- Expanded the settings column to 350 DIP after visual inspection found the marker-range button clipped behind the scrollbar at 310 DIP.
- Regenerated the six Light/Dark/Night Vision PL/EN README previews with the Avalonia Headless renderer and inspected the English Light screenshot for visible Hold and unclipped range controls.
- Updated Polish and English hints and READMEs.

## Verification

- Added an Avalonia test that clears Hold, observes automatic repeated sweeps, restores Hold after the second completion and checks the loop stops with complete data.
- Existing tests continue to cover Hold during the inter-sweep pause, cancellation and atomic result preservation.
