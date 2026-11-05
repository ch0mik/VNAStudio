# Review E1 — bandwidth and plot ranges

Date: 2026-11-04.

- Added `BandwidthHz = abs(M2 − M1)` and a culture-formatted MHz readout in the Antenna summary. It is empty until both markers exist; reversed marker order is supported.
- Added a profile-range action that sets only the plot viewport to `Profile.MinimumFrequencyHz..MaximumFrequencyHz`. The scan's physical limits remain controlled by scan settings/profile validation.
- Added independent save/recall of the current plot viewport for the app session. Recall is disabled until a range has been saved. Zoom history remains a separate stack; recalling a saved range pushes the previous viewport to that history.
- Added EN/PL labels.
- Verification: full Release suite, 64 passed, 0 failed. Interaction coverage verifies 3 MHz bandwidth, save/clear/recall, and profile bounds.
- Limitation: saved range is in-memory only and HF/VHF presets remain pending confirmation of legacy `F/F`/Bands semantics and profile rules.
