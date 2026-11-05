# Legacy band limits review

## Evidence

- `decompiled/analyser306b/strings.tsv` contains `BANDS_LIMIT` and paired names `F_M160s/e` through `F_M2s/e`, plus `F_M70cs/e`.
- `decompiled/analyser306b/unrecovered/0041649f.asm` shows these keys being read from the `BANDS_LIMIT` section.
- The supplied `reference/software/analyz.ini` has no such section, so it does not provide default values for those ranges.

## Change

- Added typed optional INI fields for all recovered range pairs and converted configured pairs into labeled HF/VHF scan shortcuts.
- Each pair must be either absent or complete with positive start below stop. UI visibility filters ranges against the selected profile. The existing UKE presets remain the fallback when the section is empty.
- The app now prefers an adjacent `analyz.ini` over its bundled 65 MHz profile, making a user's original INI directly usable without copying reverse-engineering artifacts into the project.
- Added tests for options binding, invalid incomplete pairs, custom shortcut selection, profile filtering, and corrupt profile snapshots.
- Documented the supported keys and explicitly recorded that absent source values are not synthesized.

## Review result

- Frequency units are Hz as in the legacy parameter conventions; the selector changes only sweep start/stop and does not exceed profile limits.
- `F/F` remains unresolved. The implementation does not infer a meaning for that UI label.
- `decompiled` and `reference` remain outside the `VnaStudio` project root.

## Verification

- `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=` — 111 passed.
- `openspec validate port-vna-to-csharp --strict` — valid.
