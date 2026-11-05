# ADC conversion review

## Evidence

- `reference/software/analyz.ini` documents `Adc_phres` as degrees per 1023 ADC steps and `Adc_magres` as dB per 1023 ADC steps, but the recovered code does not establish raw ADC packing, sign, or zero point.
- The same file documents `[SPECIAL_CAL]` as `y=ax+b` and provides `RL_a`, `RL_b`, `PH_a`, and `PH_b`.
- The selected profile already loads those values through typed `LegacyAnalyzIniOptions` and `LegacyAnalyzIniProfileFactory`.

## Change

- Added `RfSampleCalculator.FromScaledReadings`, applying the documented affine coefficients to already-scaled signed return-loss and phase readings before calling the existing RF derivation.
- Added a test with non-identity coefficients to verify the converted inputs and downstream RF values.
- The method intentionally does not apply `RL_OFFSET_vna` or `RL_OFFSET_filter`: the INI comments describe calibration goals, but the original acquisition and offset application flow is not established.

## Review result

- No raw ADC decoding, transport frame, phase sign convention, or physical-calibration claim is introduced.
- This is a pure conversion API and does not complete task 11.12; hardware protocol and 50 Ω/OPEN/SHORT/known-load verification remain outstanding.
- `decompiled` and `reference` remain outside the `VnaStudio` project root.

## Verification

- `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=` — 107 passed.
- `openspec validate port-vna-to-csharp --strict` — valid.
