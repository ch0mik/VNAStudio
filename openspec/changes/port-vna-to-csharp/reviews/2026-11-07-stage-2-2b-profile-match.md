# Stage 2.2b review — VNA 1–65 MHz profile and INI limits

## Review result

**PASS.** The bundled application INI and `LegacyAnalyzIniProfileFactory` match the source INI for the runtime profile values: 1–65 MHz range, 649 points, 191,993,220 Hz DDS clock, VNA type, PLL and scan delays, ADC scales, reference impedance, calibration coefficients/offsets, generator memories, and plot colors. Tests bind the file through `IOptions`, compare the created profile, verify inclusive range boundaries, and reject out-of-range sweeps; UI range controls exercise MHz/Hz conversion.

Differences from the source file are explicit: the app asset adds profile ID/display metadata and `SVNA=0`; serial/LPT addresses are not used to select an OS-specific port; `Step=1` is omitted because the source marks it normally unused; `ForeColor` and `R2d` are not runtime profile fields. The separate COM path remains user configuration. No unsupported INI keys or absent `[BANDS_LIMIT]` values are invented.

The independent subagent review confirmed that task 2.2b can be closed.

## Verification

- `dotnet test Vna.slnx --no-restore`: 208 passed, 0 failed (including `BundledIniBindsThroughOptionsAndPreservesAllConstants`, inclusive range validation, and MHz sweep-range controls).
- Strict OpenSpec validation passed for `device-plugin-architecture`, `port-vna-to-csharp`, and `inventory-vna65-research`.
- No hardware connection was made for this audit.

## Scope boundary

This closes profile-file consistency and software range conversion checks. DDS sample frequency accuracy and RF calibration remain separate physical/reference measurement gates.
