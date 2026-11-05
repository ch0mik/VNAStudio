# Review — CSV import

Date: 2026-11-04.

- Added asynchronous, cancellable CSV reading for VNA Studio's eight-column export and the legacy Antenna layout identified in the decompiled VB6 writer.
- The recovered pseudocode names Frequency(Hz) and Return Loss(dB); string references also include Phase(deg). The importer maps columns by header name, so it accepts the identified two-column form and a three-column form with phase in either position.
- Legacy import preserves only values actually supplied. It does not infer SWR/impedance from missing phase, and the chart shows unavailable traces as missing.
- The reader validates invariant numeric syntax, field count, finite values, row limit, profile range, regular sweep spacing, quality and cancellation. MainWindow adds a localized File → Import CSV action.
- Tests cover VNA Studio round-trip and synthetic legacy two-/three-column examples. No actual Analyser CSV export was supplied, so runtime compatibility with a real file is not yet verified. Binary .vec import/export remains pending.
- Verification: dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts= — 97 passed, 0 failed; strict OpenSpec validation passes.
