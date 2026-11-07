# Stage 4.5 — plot guide and performance evidence

## Scope

- English and Polish chart usage guides and links from the README/PLOTS documentation.
- Avalonia headless rendering smoke measurement using 649 synthetic `DemoVnaDevice` points, all six traces, a 1440×900 DIP layout and 20 redraws.
- Local screenshot and timing report in ignored test output.

## Verification

- `PlotPerformanceMeasurementTests` passed; its report records median **13.681 ms** and range **11.803–26.133 ms** for invalidation, layout, forced render and frame capture.
- Reviewer inspected the guide against XAML, view-model and plot controls. Marker placement, zoom/history, range actions, pan, traces, Smith hover and invalid-data behavior match the implementation.
- Reviewer confirmed that the test uses 649 actual synthetic sweep points and the PNG contains all six plotted series.
- Reviewer verdict: **PASS**. Timing and visual claims are scoped to local Avalonia headless rendering; exact legacy pixel parity, full legacy tick profiles and physical measurement accuracy remain open.

## Limitations

This stage does not verify a native desktop backend or instrument rendering performance. The reported elapsed times are local smoke measurements, not user-facing performance guarantees.
