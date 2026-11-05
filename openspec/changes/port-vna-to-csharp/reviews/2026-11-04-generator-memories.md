# Review — generator memories

Date: 2026-11-04.

- Added typed MEMORY options F_M0 through F_M9, loaded them from the supplied INI, and validate that the profile contains either no memories or ten values within its frequency range.
- Added the ten exact frequencies to the bundled user profile and DeviceProfile snapshot. Existing v2 snapshots without these values deserialize to an empty list.
- Generator mode now lists the ten profile memories and allows an editable frequency, but its output button is disabled and the UI states that no RF signal is being generated.
- Tests verify all ten INI values, snapshot serialization, profile range and disabled hardware output.
- Verification: dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts= — 98 passed, 0 failed.
- Remaining: serial command format, output level/frequency validation and physical frequency counter verification are not available.
