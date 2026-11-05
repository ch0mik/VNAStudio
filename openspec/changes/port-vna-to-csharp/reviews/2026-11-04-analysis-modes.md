# Review U11 — analysis mode navigation

Date: 2026-11-04.

- Added six mode buttons with exclusive state in the view model and localized EN/PL labels/reasons.
- Antenna controls and the result table only appear in Antenna mode. Other modes do not reuse the demonstration sweep; the view explains the missing verified data/algorithm.
- Replaced native radio indicators with app-styled buttons after night vision pixel tests found Fluent's fixed blue indicator. The red night palette remains consistent through hover/selection.
- Verification: `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=` — 64 passed, 0 failed.
- Limitation: selection is UI context only; no hardware mode command is sent.
