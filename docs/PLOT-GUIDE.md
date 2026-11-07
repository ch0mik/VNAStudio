# Plot guide

VNA Studio provides Frequency, Smith, and Both views for Antenna measurements. The view selection is saved in `settings.json`. The chart controls change the displayed view; they do not change the acquired sweep settings.

## Frequency plot

- Left-click inside the plot to place M1; right-click to place M2. Drag a marker line to move it. With the plot focused, Left/Right moves the active marker by one measured sample.
- Use `+` / `−` or the buttons to zoom the frequency window. **Zoom M1–M2** uses the two marker frequencies even when M2 is lower than M1. **Previous range** returns through the zoom history.
- **Full range** returns to the complete loaded sweep. **Profile range** uses the selected profile limits. **Save range** and **Restore range** remember a temporary view range; they do not save or load measurement files.
- Hold the middle mouse button and drag, or Shift-drag with the left button, to pan horizontally. The visible range is clamped to the profile.
- Use the trace checkboxes or All/None to choose which quantities are drawn. Color changes affect the view preference, not stored samples.
- Missing, non-finite, or invalid samples break a trace. The chart does not interpolate or smooth measurement gaps.

The frequency axis is linear. Each quantity has its own vertical scale; the current visible tick values come from the supplied screenshots. Additional range-specific legacy tick rules have not been established for every Analyser 306b range.

## Smith plot

- Left-click places M1 and right-click places M2 at the nearest visible measured frequency. Drag to move a marker.
- Use the Smith zoom controls or mouse wheel to zoom at the pointer. Middle-button drag pans the chart.
- Hover a point to see its frequency, impedance, reference impedance, and phase-sign confidence.
- When the source does not establish the physical sign of phase/reactance, the plot shows both mirrored possibilities. The view does not guess whether the load is inductive or capacitive.
- If the dataset has no usable phase or complex impedance, the Smith plot explains that no complex point can be shown. An unknown reference impedance is not replaced with 50 Ω.

## Resizing and data limits

The window can be resized and maximized; its minimum is 1024×768 DIP. The settings panel scrolls vertically. The graph and marker table use the available window size. Resize and DPI checks use Avalonia's headless renderer; a native desktop window manager can have platform-specific behavior.

### 649-point rendering measurement

The local Release smoke measurement used 649 synthetic `DemoVnaDevice` samples, all six traces visible, a 1440×900 DIP window, and 20 warmed headless redraws. The median time for invalidation, layout, forced render, and frame capture was **13.681 ms**; the observed range was **11.803–26.133 ms**. This is a local headless smoke measurement, not a performance guarantee for other machines or native display backends. The PNG and timing text are emitted under the ignored test build directory `tests/Vna.Tests/bin/Release/net10.0/layout-review/`.

## Accuracy and comparison limits

The chart geometry, tick text, markers, missing-data gaps, and responsive layout have regression coverage. Mathematical Smith-chart tests use ideal loads and analytical complex vectors. Exact pixel parity with the original application, tick profiles for all frequency ranges, and physical phase/calibration accuracy remain unverified. The reported COM9 50 Ω runs test basic acquisition and consistency, not metrological calibration. See [reference vectors and tolerance limits](../openspec/changes/port-vna-to-csharp/reference-vectors-and-tolerances.md) and [VNA communication details](VNA-COMMUNICATION.md).
