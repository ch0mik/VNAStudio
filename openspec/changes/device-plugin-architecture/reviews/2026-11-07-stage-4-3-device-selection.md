# Stage 4.3 review — device selection and settings validation

## Scope

Separated the selected plugin/profile identity into `DeviceSelection`, resolved it against the explicit plugin catalog, validated persisted settings, and added neutral compatibility handling for the existing UI/router.

## Review result

**PASS after fixes.** The subagent review identified and the implementation fixed:

- Compatibility profile placeholders could be recorded as applied reflection calibration. Hardware sources now report the actual configured processing snapshot; sources without provenance are recorded as unknown.
- A missing descriptor `Zref` could fall back to an assumed 50 Ω in live plotting and RL display correction. A present descriptor is now authoritative even when its `Zref` is null; demo data omits derived impedance in that case.
- Invalid profile schema versions were accepted. The catalog now rejects schema versions below 1.
- Options validation discarded its detailed diagnostic. The registered validator now returns the actual settings validation message.
- A duplicate local introduced while making demo impedance conditional was removed; demo SWR uses the existing reflection magnitude.

## Static checks

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution successfully.
- `openspec validate device-plugin-architecture --strict` passed.
- `git diff --check` passed; Git reports existing LF-to-CRLF normalization notices.
- No tests, build, application run, or hardware connection was performed.

## Boundaries

- The active plugin/profile is resolved at application startup from `settings.json`; changing it requires restarting the app. The Help catalog remains informational and does not hot-switch a source.
- Plugin assemblies remain explicitly registered by the host. Arbitrary plugin discovery/loading is not added.
- Hardware calibration provenance remains `ConfiguredUnverified`; this change does not claim physical calibration validation.
- No WOB fixture was connected or exercised.
