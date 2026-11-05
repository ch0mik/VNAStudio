# Cable Length manual limits review

Date: 2026-11-04.

## Source evidence

The supplied Polish measurement manual, pages 5–6 (`reference/hardware/manual-3.txt`, kept outside the application project), specifies an open- or short-ended cable and markers on peaks or dips at matching positions. It says measurements are poor when resonances are at or below 60 MHz and recommends resonances above 60 MHz. It says velocity factor should be obtained experimentally or from a table, gives 0.63 for a LAN cable and 0.50 for an outdoor extension as examples, and has 0.66 as the INI default.

## Change

- Updated the Cable Length hint in Polish and English with the setup, marker rule, 60 MHz limitation and the manual's example VF values.
- Updated `docs/CABLE-LENGTH.md` and the screenshot roadmap. The limitation is guidance and is not enforced as a frequency cutoff.
- The existing round-trip estimate remains clearly identified as a derived estimate; it is not presented as cable-loss measurement or physical verification.

## Verification

- Localization parity tests validate that both language resources keep the same keys and format arguments.
- The manual text is the source for the advisory; its example VF values are explicitly not treated as universal cable constants.
