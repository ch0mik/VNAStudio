# Targeted calibration-handler recovery

## Source and method

Retried Ghidra decompilation of the saved Analyser 306b project for procedure `VB6_0043094f`, writing the output under the sibling `../decompiled/targeted-recovery-cal/` directory, outside the VnaStudio project. This recovered 364,798 bytes of pseudocode.

## Confirmed strings and control-flow observations

- The procedure prompts `Connect LOOP cable a then press OK` and then iterates the current sample count while populating working arrays.
- A path reports `Calibration 0dB done.`
- Other paths report `Calibration RL OFF.` and `Auto calibration OFF.`
- The handler updates graph scale labels using 0/20/40/60 dB or 0/10/20/30 dB sequences.

## Limits

The recovered pseudocode does not identify the exact controls selecting these branches or the physical meaning, units, and application formula for the working arrays. It does not establish the calibration formula. VNA Studio uses the screenshot label and recovered 0/20/40/60 dB scale branch to implement RL×2 strictly as a display-range option; no calibration correction is enabled. Verify the calibration formula against the original application and known loads before implementation.

## Review

Manual inspection of the recovered pseudocode and existing `docs/DEVICE-PROTOCOL.md`; OpenSpec strict validation passed after recording the evidence. The source files and project database remain outside VnaStudio.
