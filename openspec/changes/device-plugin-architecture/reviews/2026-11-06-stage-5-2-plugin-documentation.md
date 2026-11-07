# Review: plugin architecture and protocol documentation

## Scope

Updated the EN/PL README and detailed communication guides to reflect the VNA65 plugin and neutral serial transport split.

## Review outcome

PASS after correcting two documentation details. The guides now point to files in their current projects, distinguish the compatibility `DeviceProfile` factory from its mapping to `Vna65MHzProtocolSettings`, describe the configured port as an application setting passed through unchanged, and identify the VNA65 converter as the owner of ADC-to-RF conversion. The README also avoids claiming that the bundled INI supplies a COM port. Calibration coefficient semantics remain clearly marked as not independently validated.

## Verification

- All listed source links were checked against the current project paths.
- `git diff --check` completed without whitespace errors.
- Reviewer subagent: PASS.
- No build, tests, or physical WOB connection were used.
