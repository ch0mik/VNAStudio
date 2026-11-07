# Stage 2.3c.3 review — session format v6 neutral profile snapshot

**Result:** PASS

## Scope

- Added optional `MeasurementSession.ProfileDescriptorSnapshot`.
- Bumped the session format to v6 and accepted v1–v6.
- Migrations create the neutral descriptor from an available legacy `ProfileSnapshot`; legacy profile data remains available for older consumers.
- Kept sample points and recorded frequencies unchanged during migration.
- Updated session-format and architecture documentation to describe the optional snapshot and current UI limitation accurately.

## Review findings

- Migration review found no loss or recalculation of stored samples/frequencies. v3–v5 update format metadata; v1–v2 add phase confidence/provenance while preserving numeric samples.
- The neutral descriptor is optional and is synthesized only when a valid legacy snapshot exists. Documentation now says so and does not claim current cross-profile UI rendering.
- Current session loading still requires a matching active profile and uses the legacy or active profile for rendering. Cross-profile presentation and playback remain task 2.3c.4.
- The OpenSpec design and dependency inventory describe the same state.

## Verification

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution successfully.
- `git diff --check` passed.
- No test or build commands were run.
