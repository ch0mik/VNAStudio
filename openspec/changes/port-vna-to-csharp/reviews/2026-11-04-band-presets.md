# Band preset implementation review

- Added Polish category 3 HF and 2 m sweep presets from the UKE allocation page.
- The selector only changes sweep edges and filters presets to the selected device profile; no RF output behavior is attached to it.
- The bundled 65 MHz profile shows the nine HF ranges and omits the 144–146 MHz VHF range.
- `AmateurBandPresetsSetOnlyProfileValidSweepEdges` checks that selecting 80 m sets 3.5–3.8 MHz and VHF remains unavailable under the 65 MHz profile.
- Limitation: allocations are localized to Poland/category 3 and require periodic source review; the legacy `F/F` control remains unidentified.
