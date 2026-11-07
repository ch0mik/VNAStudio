# Comparison snapshots

VNA Studio can save a completed measurement as a named `.vnasnapshot.json` file and load one or more of these files as Smith-chart and frequency-plot overlays. This is an offline comparison feature: saving or opening a snapshot does not connect to a device or open a serial port.

## Data and provenance

A snapshot wraps a validated measurement session. It retains the exact acquired point frequencies and values, sweep settings, source, plugin/profile IDs, profile presentation metadata, analysis mode, data kind, frequency-grid convention, capture time, and available calibration/provenance metadata. A separate finite display-only return-loss offset records the current chart view; it does not modify the session samples or claim to be a calibration. Loading a snapshot runs the same session validation and migration as opening a `.vnasession.json` file.

The in-memory snapshot copies its sample and metadata collections into read-only collections. The source measurement is not modified. Snapshots with different sample counts or frequency grids are each rendered from their own recorded samples; the chart does not resample or interpolate between them.

## Smith-chart rendering

The active sweep remains the primary solid trace. On the Smith chart, snapshots are drawn behind it with dashed lines and use each saved profile's `Zref`. If a neutral profile descriptor exists, its `Zref` is authoritative even when it is unknown; the overlay does not fall back to an older profile snapshot or borrow the active device's reference impedance. Ambiguous reactance sign continues to render both conjugate candidates with separate dash patterns. On the frequency plot, choose one metric from the comparison selector; snapshots draw behind the active trace in their own colors. The snapshot's own display-only return-loss offset is applied to both overlays, while its source session remains unchanged. Scalar transmission sessions are rejected by these reflection comparison views.

When no active sweep is loaded, the Smith chart labels the comparison reference impedance as trace-specific instead of displaying the active profile's `Zref`. The snapshot tooltip identifies each trace's own reference impedance. Legacy FILTER sessions whose data kind is unknown are also rejected: older files can contain scalar transmission values without recording that distinction.

Use the name field and **Save current** to create a snapshot file. Use **Open snapshots** to load multiple snapshots, then toggle each checkbox to show or hide its trace. **Clear** removes the current overlays from the view; the saved files remain available to reopen.

Snapshots are not currently persisted as an application-wide overlay list. They can be reopened from their files after restarting VNA Studio. Transmission-line transforms and conversion of scalar FILTER data into complex S-parameters are not part of this feature; the gated design is documented in [TRANSMISSION-LINE-TRANSFORMS.md](TRANSMISSION-LINE-TRANSFORMS.md).
