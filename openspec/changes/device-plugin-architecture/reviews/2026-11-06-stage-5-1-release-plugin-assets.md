# Stage 5.1 review — release plugin assets

**Result:** PASS

## Implemented

- Release output validation now requires `Vna.Device.Vna65MHz.dll` and the configured profile INI for each supported RID.
- `Publish-Release.ps1` validates each publish output before either creating an archive or returning the directory through `-PublishOnly`.
- The GitHub release workflow calls the publish script for every RID in its 11-platform matrix.
- `docs/RELEASING.md` documents the plugin assembly and validation step.

## Review

Subagent review passed. It confirmed that the expected plugin assembly name matches the project output and app project reference, and that every matrix RID flows through the validation before packaging. No findings.

## Verification

- No tests, builds, or publishes were run.
- No device was connected.
