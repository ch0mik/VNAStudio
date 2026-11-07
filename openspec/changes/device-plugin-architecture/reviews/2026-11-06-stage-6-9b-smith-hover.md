# Review: Smith chart nearest-sample hover

## Scope

Added a hover readout for the nearest plotted Smith sample. It reports frequency, impedance when the data supports it, reference impedance, and phase-sign confidence. Ambiguous signs are shown as two possible impedances.

## Review outcome

PASS. Hit testing compares the pointer only with actual sample coordinates and candidate branches; it does not interpolate between samples. It filters to valid samples inside the active frequency range and unit disk. Verified impedance keeps its signed reactance, while ambiguous impedance shows both signs. Hover state clears on pointer exit and on marker/pan interactions. The readout is drawn outside the Smith disk clip and clamped inside the control. It only reads plotted samples and does not modify them.

## Verification

- Both EN and PL resource files parsed as XML.
- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution successfully.
- `git diff --check` completed without whitespace errors.
- Reviewer subagent: PASS.
- No build, tests, or physical WOB connection were used.
