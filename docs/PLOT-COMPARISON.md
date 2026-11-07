# Plot comparison notes

This is a structural comparison between a supplied V1.3.2 legacy-family application screenshot (`original-page2-R19.png`) and VNA Studio Avalonia headless renders (`minimum-1024x768.png` / `plot-649.png`). As noted in [PLOTS.md](../PLOTS.md), the legacy-family screenshot is not proof of exact Analyser 306b control layouts. The original images and application binaries stay in the neighboring `VNA-ReverseEngineering` research folder; headless screenshots under the test project's ignored `bin` directory are generated locally.

| Visual element | V1.3.2 legacy-family screenshot | VNA Studio render |
| --- | --- | --- |
| Frequency traces | Six colored quantities share one plotting area and use separate labeled scales. | Six independently toggled series share one plot; each plotted quantity uses its own scale and the corresponding edge labels. |
| Markers | Red M1 and blue M2 vertical lines with labels below the trace. | Red M1 and blue M2 lines, draggable/clickable, with values in the marker table below the chart. |
| Plot frame | The screenshot shows a compact chart in the center of the pictured application window. | Chart expands with the window beside a scrollable scan-settings panel; minimum window size is 1024×768 DIP. |
| Controls and results | Mode buttons above the chart, marker values below it, sweep settings and trace colors below the chart. | Analysis modes are above the chart; range/zoom and comparison controls sit between the mode bar and chart; settings are in the side panel and marker summaries below the plot. |
| Data basis | Values and traces shown in the supplied application screenshot; the raw measurement and its provenance are unavailable. | `minimum-1024x768.png` uses five points from `TestData.Session()`; `plot-649.png` uses 649 synthetic `DemoVnaDevice` points for the performance test. Neither generated image is an instrument recording. |

The supplied screenshot and generated renders have different dimensions, plotted data, typography and control layout. The available original screenshot is not accompanied by a matching raw measurement/session file, and the legacy program was not run to capture a controlled second image. Therefore the visual review confirms corresponding chart concepts and documents layout differences; it does not establish numerical pixel parity, identical curve positions, axis tick rules for every range, or physical measurement accuracy.

For interaction behavior and the local headless render timing, see the [plot guide](PLOT-GUIDE.md). The original screenshot references are archived outside the VNA Studio source tree as requested.
