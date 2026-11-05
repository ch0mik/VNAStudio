# DDS session round-trip review

Date: 2026-11-04.

## Reviewed

- Hardware sweeps use the frequency grid represented by the DDS tuning words. With a 1–4 MHz range and three points, the stored points are 1, 2 and 3 MHz; Stop is the exclusive upper bound for this device grid.
- Session validation now preserves these measured frequencies and requires only a strictly increasing sequence within the saved start/stop range, alongside the existing point-count and profile checks.
- Session playback reports the stored frequencies for the exact saved sweep instead of reconstructing an inclusive-endpoint grid.
- The view model accepts a valid stored hardware grid when loading and playing a session.

## Verification

- `dotnet test --no-restore -p:UsedAvaloniaProducts= --verbosity quiet`: 124 passed.
- Added an integration round-trip test covering serialization, load, explicit playback connection and sweep; it asserts that 1/2/3 MHz are returned unchanged for a 1–4 MHz, three-point sweep.
- Session validation rejects a sample outside the saved frequency range.
- No physical VNA was connected; this verifies persistence and playback of the software's hardware-derived grid only.
