# RL offset display adjustment

## Evidence

The screenshot shows an `Offset` input alongside RL×2. The supplied `analyz.ini` documents `RL_OFFSET_vna` as the return-loss correction used to zero the close-port readout. VNA Studio keeps that profile setting in its driver and adds a separate user adjustment for display.

## Implementation

The localized RL offset field accepts finite values from −300 to +300 dB, persists the last valid value in application settings, and reformats it when the UI language changes. It applies to a derived display copy of valid samples using the established return-loss/phase calculation, so the plot and marker readouts update SWR and impedance consistently. Raw points used by sessions and session playback are not modified.

## Verification

- Avalonia view-model coverage checks adjusted RL/SWR/impedance, unchanged raw session points, invalid input handling, and settings persistence/reload.
- Offset recalculation uses the loaded session's `ProfileSnapshot`, including its reference impedance, before the first display refresh.
- Full Release suite and strict OpenSpec validation pass after implementation.

## Limit

The original screen's exact valid input limits and whether its field is additive or replaces the profile offset are not established. The implemented field is explicitly additive and independent from profile calibration.

## Independent review

A read-only subagent found that the initial implementation recalculated loaded session data using the current device profile rather than the saved snapshot. This was corrected, and the regression test now loads a 75 Ω session snapshot on the 50 Ω default device and checks the adjusted impedance.
