# Review: descriptor-driven ViewModel presentation

## Scope

Moved active profile ranges, label, point-count default, band presets, cable VF default, and plot colors to the neutral profile descriptor. Retained the legacy compatibility fallback for hosts that do not supply a descriptor. Updated trace picker/reset defaults when the active presentation source changes.

## Review outcome

PASS after review fixes. Base trace colors are theme-independent; rendered palettes separately apply the selected theme and explicit user overrides. Trace controls refresh their default, selected, and selectable colors when switching between the active descriptor and a historical snapshot. A session with a saved legacy profile snapshot uses its saved colors and Zref. A loaded session with no saved Zref remains unknown instead of inheriting the current device's Zref. Sessions with no legacy color snapshot use the descriptor palette.

## Verification

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution successfully.
- `git diff --check` completed without whitespace errors.
- Reviewer subagents: PASS.
- No build, tests, or physical WOB connection were used.
