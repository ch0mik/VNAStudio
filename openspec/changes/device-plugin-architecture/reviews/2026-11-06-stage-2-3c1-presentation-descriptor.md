# Review: neutral profile presentation descriptor

## Scope

Added a host-facing `DeviceProfilePresentationDescriptor` for optional frequency-band shortcuts, RGB trace colors, and a default cable velocity factor. The VNA65 compatibility profile maps its legacy values into this descriptor. The plugin catalog validates and copies the values into read-only collections.

## Review outcome

PASS. Presentation hints remain separate from the protocol settings. Frequency bands and trace-color maps are copied into read-only wrappers; invalid bands, malformed colors, and invalid velocity factors are rejected. The VNA65 COLORREF conversion matches the legacy RGB order. The plugin factory compares the descriptor's presentation values rather than relying on collection reference equality.

## Verification

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution successfully.
- `git diff --check` completed without whitespace errors.
- Reviewer subagent: PASS.
- No build, tests, or physical WOB connection were used.
