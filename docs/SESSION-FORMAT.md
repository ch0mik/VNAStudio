# VNA Studio session format

VNA Studio saves completed measurements as UTF-8 JSON (`.vnasession.json`). The root object contains:

An example session that can be opened by the application is available at [`examples/demo.vnasession.json`](examples/demo.vnasession.json).

- `FormatVersion`: current version `4`; versions `1`–`3` are migrated when loaded. Version 4 adds the optional `TransmissionDb` sample field used by Filter mode.
- `ProfileId` and `ProfileSnapshot`: the profile identifier and the measurement-time DDS, sweep, calibration, generator memory, and plot color settings.
- `AnalysisMode`, `DataSource`, and `CapturedAtUtc`: mode and provenance metadata. Cable Loss saves the underlying reflection samples; Filter saves the separate scalar response in `TransmissionDb`.
- `Sweep`: start/stop frequency and point count.
- `Points`: one entry per frequency, in ascending sweep order. Missing measurements are represented with `null` values and/or a non-`Valid` quality. Display preferences, including RL×2 and the extra RL display offset, are saved separately in `settings.json`; display correction does not rewrite stored session samples.

The reader rejects unsupported versions, invalid snapshots, inconsistent point counts, unknown quality values, non-increasing frequencies, and samples outside the saved start/stop range. The stored point frequencies are preserved as measured, including the miniVNA DDS grid whose last point is below Stop. Version 1 sessions load as Antenna mode. Saving and loading accept a `CancellationToken`.

To exercise offline playback, open a `.vnasession.json` file, choose **Session playback**, and use **Connect** followed by **Start scan**. Playback returns the stored points only when the current start/stop frequencies and point count exactly match the session; it does not interpolate or open a COM port. A profile mismatch is rejected when loading the session. Opening a session alone continues to display its stored plot and does not start playback. A session is not a legacy `.vec` format and does not claim compatibility with files from Analyser 306b or vna/J.

CSV export is a separate unit-labelled VNA Studio format. The current Studio header includes `Transmission_dB` and `AnalysisMode`; the importer preserves scalar Filter sessions. The previous eight-column Studio header remains supported and loads as Antenna mode. The CSV importer also recognizes the Analyser 306b antenna CSV header recovered from the VB6 writer and an optional Phase(deg) column. Legacy rows preserve only columns present in the file; missing SWR, impedance, resistance, and reactance remain null. The legacy layout was not verified against an actual user-exported CSV sample. Binary .vec files and other undocumented CSV layouts are not supported.
