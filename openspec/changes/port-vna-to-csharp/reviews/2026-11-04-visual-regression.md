# Review E5 — screenshot regression and language previews

Date: 2026-11-04.

- Generated distinct English and Polish screenshots for Light, Dark and Night Vision and updated both READMEs.
- Manually inspected the English and red night-vision captures. The app keeps its adaptive 1440×900 layout; screenshot content is explicitly synthetic demonstration data.
- Existing headless checks cover 1080×700 minimum, larger sizes and 1.0/1.25/1.5 render scaling. Added numeric checks reproduce both screenshot bandwidths and their displayed Q hints.
- Verification: full Release suite, 76 passed, 0 failed; strict OpenSpec validation passes.
- Limitation: exact trace/tick matching needs original captured samples; hardware-dependent validation has not been performed. Provisional axes remain documented as such.
