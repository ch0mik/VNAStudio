# Review F02 — explicit CSV export

Date: 2026-11-04.

- Added an asynchronous, cancellation-aware CSV exporter and a save-file action.
- CSV columns explicitly state frequency and electrical units. Numbers use invariant decimal separators, invalid/missing measurements are empty, quality is preserved as a field, and output is UTF-8 without a BOM.
- Legacy `.vec` is intentionally not conflated with this new CSV format.
- Verification: full Release suite, 65 passed, 0 failed. Export test checks header, invariant decimal points and empty values.
