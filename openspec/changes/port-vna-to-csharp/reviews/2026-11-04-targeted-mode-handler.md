# Targeted mode-handler recovery

Date: 2026-11-04.

## Recovery

The main Ghidra export marked `VB6_00425e1d` as failed. A targeted run against the saved project, with analysis disabled and a 300-second decompilation timeout, recovered pseudocode in `decompiled/targeted-recovery/` outside `VnaStudio`.

The recovered procedure compares the selected mode against Antenna, Cable Loss, Cable Length, SVNA, Filter and Generator, then updates mode-specific controls. It contains the literal modal message `Calibration RL OFF - Mode change` and clears an internal calibration-active flag in the mode-change path.

## Scope and limits

- The source confirms that the old application warns the user and turns off active calibration on a mode change.
- The recovered control flow does not establish the calibration math, acquisition reference, RL×2 behavior or hardware command contract.
- VNA Studio documents this rule and continues to gate calibration calculations on a verified implementation.
- A separate attempt to recover `VB6_0043094f`, the calibration procedure, still timed out at 300 seconds. The Hold/Stop procedure recovery is running separately.
