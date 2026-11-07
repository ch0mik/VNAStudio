# Review: Smith chart SWR circle

## Scope

Added an optional constant-SWR circle to the Smith chart. The user can hide it, set a finite fixed SWR value, or bind it to marker M1. The value is stored in application settings and localized in English and Polish.

## Review outcome

PASS after moving the SWR value label outside the Smith disk clip. The circle itself remains clipped to the unit disk. Fixed mode maps SWR to `|Γ| = (SWR - 1) / (SWR + 1)` and rejects non-finite values or values below 1. Marker mode requires an in-range valid point, a usable reflection coefficient with `|Γ| < 1`, and a finite resulting SWR. It only reads the plotted samples and does not modify measurements or session data.

## Verification

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution successfully.
- `git diff --check` completed without whitespace errors.
- Reviewer subagent: PASS.
- No build, tests, or physical WOB connection were used.
