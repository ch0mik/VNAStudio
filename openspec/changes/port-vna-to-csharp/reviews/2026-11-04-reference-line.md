# Review U08 — 50 ohm display reference

Date: 2026-11-04.

- Added a Display checkbox and persisted setting for an independent, dashed 50 Ω reference line.
- The line uses the chart's provisional impedance scale and the impedance trace color. It has no input into profile reference impedance, marker calculations, calibration, or sample data.
- Added English and Polish labels.
- Verification: full Release suite, 65 passed, 0 failed; settings startup, creation and JSON roundtrip are covered; strict OpenSpec validation passes.
- Limitation: exact legacy axis placement still depends on E0 confirmation of legacy scales/ticks (U07). This feature is only a view annotation.

