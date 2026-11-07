# Stage 2.3c.4 review — neutral session presentation and offline playback

**Result:** PASS

## Implemented

- Historical session profile labels, range bounds, band presets, palette, and Smith `Zref` use the saved neutral descriptor when present.
- New sessions write the neutral profile descriptor and do not serialize the legacy profile shape. Legacy session snapshots remain readable and are retained when an older session is re-saved.
- Sessions with a neutral or legacy profile snapshot can be opened even if their plugin or profile differs from the active hardware configuration.
- The generic playback source validates against saved metadata, returns the original point objects at their recorded frequencies, requires the exact saved sweep, and checks cancellation while enumerating.
- Hardware connect and scan require a matching profile and plugin ID when the session contains `PluginId`. The explicit `legacy-vna65` alias is accepted only for profile `iw3hev-65mhz`; sessions without `PluginId` retain profile-only matching.
- Snapshot-less sessions are validated against the active profile, never a previously loaded session's descriptor. Filter/reflection playback mode is gated by saved data kind and saved analysis mode where the kind is unknown.
- A loaded session replay preserves original data source, plugin/profile metadata, capture time, calibration and frequency-grid provenance; no data changes means the replay does not mark it dirty.

## Review

Subagent review passed after three correction rounds. Reviewers confirmed plugin/profile mismatch gating (including the migration alias), snapshot-less active-profile validation, saved-snapshot validation, exact sweep matching, mode compatibility, cancellation, and preservation of source/plugin provenance through replay. A same-profile/different-plugin session now reports the saved plugin ID with a localized plugin-specific error; the playback hint names both plugin and profile differences in English and Polish.

## Verification

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution successfully.
- `git diff --check` passed.
- No test or build commands were run.
