# Stage 4.2 review — legacy profile and session migration

**Result:** PASS for metadata present in the legacy session.

## Findings

- VNA65 profile creation, typed INI options, and validation are owned by `Vna.Device.Vna65MHz` (`2.1a`/`2.1b`); prior reviews for those stages passed.
- The stable legacy profile ID `iw3hev-65mhz` remains readable. Qualifying pre-v5 `hardware` sessions without a plugin ID receive only the compatibility ID `legacy-vna65`.
- Session migration preserves recorded point values and sweep settings. When a legacy profile snapshot exists, its `Z0`, colors, range and other profile metadata are copied into the neutral descriptor. Existing phase values remain unchanged; old phase-sign confidence is conservatively marked ambiguous.
- If a legacy file does not contain a profile snapshot, unavailable historical `Z0`/colors cannot be reconstructed. The app leaves `Z0` unknown instead of borrowing the active profile.
- Playback uses the saved sweep and points without requiring the originating plugin.

## Verification

Subagent static audit passed. No tests, builds, or hardware checks were run.
