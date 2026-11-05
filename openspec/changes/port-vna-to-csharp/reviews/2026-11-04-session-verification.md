# Review — session persistence and offline playback

Date: 2026-11-04.

- Verified JSON round-trip of points, nullable sample values, capture time, sweep, profile/calibration snapshot and plot palette.
- Verified v1 migration to v2 Antenna mode and v2 preservation of the selected analysis mode.
- Added load rejection coverage for point/frequency mismatch and cancellation; malformed JSON, unsupported versions, bad snapshots, and save cancellation are covered as well.
- Verified a stored session populates the chart and source label in the view model without using the serial-port catalog. A profile mismatch is rejected before replacing the currently displayed data.
- Added `docs/SESSION-FORMAT.md` and a loadable fixture at `docs/examples/demo.vnasession.json`; the fixture is included in tests and opened successfully. The documentation gives the playback procedure and a clear statement that session/CSV formats do not imply legacy `.vec` compatibility.
- Verification: `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=` — 80 passed, 0 failed.
- Limitation: no supplied legacy `.vec` or CSV sample exists; legacy import remains unimplemented.
