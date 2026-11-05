# DISPLAY Full marker table

## Evidence

Comparing the supplied S1 and S2 screenshots shows the full marker table in S1 (frequency, SWR, impedance, RL, phase, resistance and reactance) and a compact frequency/RL table in S2. The separate `Full` checkbox appears in the `DISPLAY` group in both screenshots. It is distinct from the `Full` scan-range button.

## Implementation

Added a localized and persisted **Full marker values** checkbox. The full table remains the default for existing settings; clearing it shows only frequency and return-loss columns for M1/M2. Measurement data and marker selection remain unchanged.

## Verification

- Avalonia coverage checks that the checkbox swaps the full and compact tables.
- Settings tests cover options reload and JSON persistence, including the full-table default for older settings.
- Full Release suite and strict OpenSpec validation were run after implementation.

## Limit

The screenshots establish the displayed columns. Exact typography and column widths remain subject to pixel comparison against the original at matching window sizes.
