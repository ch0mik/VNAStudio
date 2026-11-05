# Review C03 — scan progress and duration

Date: 2026-11-04.

- Added point-count progress, a progress bar while acquisition runs, and the duration of the last completed scan.
- Samples are staged during acquisition; the chart and last complete session are replaced atomically only after a full, ordered sweep. Cancellation, transport failure, wrong sample count, or a frequency mismatch leaves the previous result intact.
- The displayed count tracks the in-progress candidate while the displayed duration remains that of the last completed sweep. A new duration is committed only on successful completion.
- The same display refreshes while a loaded measurement is shown; target sample count follows the actual sweep settings.
- Verification: full Release suite, 82 passed, 0 failed. Tests verify loaded-session point counts, a completed 649-point demo sweep, and preservation of the last complete result/duration after a cancelled or failed second sweep.
- Continuous mode, Hold, serial timeout/reconnect remain dependent on an acquisition contract and actual device transport.
