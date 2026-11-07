# Stage 11.19c.1 — static `.vec` association audit

## Findings

- The Analyser 306b function map associates `VB6_004258dd` with `.vec` save-dialog/filter text and `VB6_00429b00` with a generic `Analyzer_Files (*.vec)` filter. The filter string alone does not establish a load handler.
- Separate miniVNA230 associations identify `VB6_004236f0` with the `Load .vec file` and `Incompatible .vec file format` strings, `VB6_00428810` with the save UI, and `VB6_004291f0` with a load-dialog association. These are not evidence that both program families share the same file grammar.
- The source inventory reports no original `.vec` sample or complete, validated serialization grammar. Existing CSV captures from VNA Studio are not legacy format oracles.
- No `.vec` parser or compatibility declaration was added. Parent task 11.19c remains open for sample files and format validation.

## Evidence

- [Function/address evidence map](../../../../../VNA-ReverseEngineering/inventory/decompilation-function-map-2026-11-06.md)
- [Software artifact and file-format index](../../../../../VNA-ReverseEngineering/inventory/software-artifact-index-2026-11-06.md)

Static analysis only. No original program, downloaded tool, device or WOB was run or accessed.

Independent reviewer verdict: **PASS** after correcting family attribution. Reviewer notes that `VB6_004291f0` is a load-dialog association from filter strings, not a complete handler or serialization claim. Parent task 11.19c remains open for sample validation.
