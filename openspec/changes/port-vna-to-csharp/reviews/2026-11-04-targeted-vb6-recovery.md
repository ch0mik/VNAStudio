# Targeted VB6 recovery review

## Recovery run

- Reopened the existing Ghidra project read-only and decompiled only `00425e1d` and `0043094f` to `decompiled/targeted-recovery/targeted-pseudocode.c`, outside the `VnaStudio` project.
- `00425e1d` completed with pseudocode. It branches on the named modes Antenna, Cable Loss, Cable Length, SVNA, Filter and Generator and changes many mode-dependent control states. Its property accessors remain unresolved, so this does not identify the meaning of individual DISPLAY checkboxes.
- `0043094f` still times out after the extended five-minute per-function limit. Its original assembly remains available in `decompiled/analyser306b/unrecovered/0043094f.asm`.

## `.vec` format evidence

- `0041fda3` opens the selected file with VB6 binary mode (`0x20`) and writes three array owners (`0043b1c4`, `0043b1c8`, and the form's array at `param_1 + 0x36`) followed by two single values stored at form offsets `0x2f` and `0x30`.
- `00420024` performs the matching binary reads into those same arrays and scalar fields.
- This confirms that `.vec` is not plain CSV and that the source has paired read/write paths. The array element types, bounds/length encoding and exact value semantics are not established by these calls alone; no real `.vec` sample is present in the supplied references.

## Decision

- No parser or writer was added on this evidence alone; doing so could produce files incompatible with the original program.
- `DISPLAY Full`, `RL×2`, and `F/F` remain unresolved. The targeted recovery has not established their handlers.
- Hardware protocol behavior remains unimplemented until request/response framing and ADC decoding are proven.
