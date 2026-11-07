# Stage 11.2b.4 — static RL label branches

## Finding

The retained Analyser 306b disassembly for `VB6_0043094f` shows a label-group split at conditional jump `0x004322fe`, following compare/boolean setup at `0x004322e1–0x004322f8`. One path writes labels `0/20/40/60 dB`; the other writes `0/10/20/30 dB`. Static string references map the literal strings to the displayed label addresses. The branch predicate's source remains unresolved.

This recovers two visible label groups, not their meaning. The disassembly does not identify the controlling UI property, map it to `RL×2` or `Bands F/F`, establish frequency-span conditions, recover plotting coordinates/transforms, or define every tick profile. Parent task 11.2b and subtasks 11.2b.2/.3 remain open.

## Evidence and review

- Static source-gap report: `../../../../../VNA-ReverseEngineering/analysis/analyser306b-source-gaps-2026-11-07.md`.
- Function/address evidence map: `../../../../../VNA-ReverseEngineering/inventory/decompilation-function-map-2026-11-06.md`.
- Retained listing: `../../../../../VNA-ReverseEngineering/decompiled/analyser306b/unrecovered/0043094f.asm`.
- No executable or downloaded tool was run; no device/WOB was accessed.
- Independent reviewer verdict: PASS. The reviewer confirmed `0x004322fe` is the label-group branch, while `0x004322df` is an HRESULT-path instruction; the predicate meaning remains unresolved.
