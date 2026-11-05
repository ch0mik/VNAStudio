# README preview refresh review

Date: 2026-11-04.

## Reviewed

- Ran the existing `RenderThemePreviewsFromFullDemonstrationSweep` test as part of the 122-test suite. It captures all three themes in English and Polish from a 649-point synthetic session using Avalonia Headless and Skia.
- Refreshed the six README PNGs from those test artifacts after the source selector and scan-range controls changed.
- Inspected the 1440×900 English Light image. It shows the explicit Demonstration source, profile scan range controls, responsive plot, marker values and branding; synthetic data are labelled as a session from demonstration data.
- Kept `decompiled` and `reference` outside `VnaStudio`; the README screenshots contain no source binaries.

## Verification and limits

- The refreshed images are outputs of the same test run that passed 122 tests.
- These previews verify current rendering at 1440×900. They do not establish pixel compatibility with the original 604×379 screenshot or replace manual minimized/maximized and physical-hardware review.
