# Stage 2.3c.6 review — neutral CSV profile metadata

## Review result

**PASS.** The normal application CSV import now uses the active `DeviceProfileDescriptor`, validates the imported sweep against its frequency range, and stores the descriptor in `ProfileDescriptorSnapshot`. It leaves the legacy `ProfileSnapshot` unset. An unknown descriptor `Zref` therefore remains unknown through session loading, saving, playback, and Smith rendering; the temporary compatibility profile's 50 Ω fallback is not promoted into the imported session.

`DemoVnaDevice` also validates sweep bounds against the descriptor when one is supplied. The legacy `DeviceProfile` CSV overload remains available for compatibility and older callers.

## Static checks

- The C# language server loaded `Vna.slnx` with SDK 10.0.402.
- `openspec validate device-plugin-architecture --strict` passed.
- `git diff --check` passed for the changed source and planning files.
- No tests, build, application run, or hardware connection was performed.

## Remaining boundary

The old CSV overload still creates a legacy profile snapshot by design. The application composition supplies the neutral descriptor, so its normal import path does not use that overload.
