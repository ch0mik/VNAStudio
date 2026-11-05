# Independent review follow-up

Date: 2026-11-04.

The subagent review identified one correctness issue in `MainWindowViewModel.StartScanAsync`: the view model cleared the previous complete plot before acquisition and could expose a partial result after cancellation or failure. The scan now stages candidate samples and commits plot/session/profile metadata and duration only after validating the complete count, ordered frequencies, and sample quality values.

Regression coverage runs a successful sweep followed by both a cancelled partial sweep and a failing partial sweep. In both cases the last complete points, session, and elapsed duration remain available while progress reports the attempted sweep. Follow-up subagent review confirmed the correction; no new findings were reported.

Other reviewed areas: v1/v2 session persistence and validation, the example JSON, offline playback/profile mismatch handling, README/OpenSpec claims, and explicit demo-only wiring. Source protocol limits and missing COM/legacy sample files are documented constraints, not independent findings.
