# Review A01 — marker bandwidth

Date: 2026-11-04.

- Added bandwidth display to the Antenna readout, calculated as `abs(M2 − M1)` in Hz and formatted in MHz with the selected culture.
- It remains blank until both markers are set. Reversing marker order produces the same positive width.
- Added English and Polish resource labels.
- Verification: full Release test suite, 64 passed, 0 failed; marker interaction checks cover reversed markers and a 3 MHz separation.
- No Q value is inferred from this bandwidth; Q requires separate validated resonance boundaries.
