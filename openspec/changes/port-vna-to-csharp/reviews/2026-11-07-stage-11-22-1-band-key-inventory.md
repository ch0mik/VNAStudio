# Stage 11.22.1 — static band key inventory

## Finding

The retained string-reference export for `VB6_0041649f` lists `BANDS_LIMIT`, `FMin`, `FMax`, `F_M0`–`F_M9`, and 13 raw `F_M...s/e` key pairs. This confirms that the INI-related routine contains optional band-limit key names. The export does not establish units, default values, how each key is consumed, or what the screenshot's `F/F` control does.

The supplied `analyz.ini` has no `[BANDS_LIMIT]` section and no values for the 13 `F_M...s`/`F_M...e` keys. Its `F_M0`–`F_M9` values are under `[MEMORY]` as generator frequency presets; they are not band-limit values. No legacy behavior or VNA Studio UI behavior was inferred from the names. Parent task 11.22 and the original tick-profile tasks remain open.

## Evidence and review

- Static source-gap report: `../../../../../VNA-ReverseEngineering/analysis/analyser306b-source-gaps-2026-11-07.md`.
- Function/address map: `../../../../../VNA-ReverseEngineering/inventory/decompilation-function-map-2026-11-06.md`.
- String references: `../../../../../VNA-ReverseEngineering/decompiled/analyser306b/string-references.tsv`.
- Supplied profile: `../../../../../VNA-ReverseEngineering/reference/software/analyz.ini`.
- No executable or downloaded tool was run; no device/WOB was accessed.
- Independent reviewer verdict: PASS after revisions. The reviewer confirmed the 13 raw `s`/`e` suffix pairs, the `[MEMORY]` location of `F_M0`–`F_M9` generator presets, and the absence of `[BANDS_LIMIT]` values in the supplied INI; no units or `F/F` semantics are inferred.
