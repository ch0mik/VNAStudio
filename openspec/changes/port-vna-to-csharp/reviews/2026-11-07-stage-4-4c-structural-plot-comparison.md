# Stage 4.4c — structural plot comparison

## Scope

Compared the supplied V1.3.2 legacy-family application screenshot with the current VNA Studio headless renders. The archived title/version is not used to assert exact Analyser 306b identity or control layout.

## Findings

- Both images show a multiseries frequency plot, separately scaled quantities, M1/M2 markers and marker values associated with the plot.
- The legacy screenshot shows a compact centered plot and places controls around the chart; VNA Studio uses a resize-aware chart, a scrollable side panel, controls above it and summaries below it.
- The supplied legacy screenshot's raw measurement and provenance are not available. `minimum-1024x768.png` uses five `TestData.Session()` samples; `plot-649.png` uses synthetic `DemoVnaDevice` data.
- The original application was not run for a matching capture. Exact pixel parity, identical curve positions, range-specific axis parity and physical accuracy remain unverified, so parent task 4.4 stays open.

## Verification

- Visual inspection of original and generated images; implementation claims checked against current plot control/layout documentation.
- No application binaries were executed; no COM device or WOB was used.
- Independent reviewer verdict: **PASS** after correcting image identity, provenance, table headings and window wording. The parent 4.4 remains open for matched-data pixel/runtime parity.
