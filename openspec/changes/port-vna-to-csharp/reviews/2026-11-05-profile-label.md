# Device profile label

The owner supplied photographs identifying the enclosure as VNA 1-65 MHz and requested removal of analyz.ini from the profile name. The current display name is now `VNA 1-65 MHz` in the bundled INI, typed INI defaults, static device profile, example session and README translations. The existing profile binding test checks the updated label.

Review: the stable profile ID is retained for session compatibility; only the display label changes. The default UI label comes from the profile. Explicit display names in user-supplied profiles and historical session snapshots remain their recorded values. Photographs identify the enclosure label; they do not establish the internal DDS part number.

The detailed communication guide is available in `docs/VNA-COMMUNICATION.md` and `docs/VNA-COMMUNICATION.pl.md`, linked from both README translations and the protocol evidence notes. It describes actual configuration binding, wire bytes, DDS arithmetic, binary frame layout, ADC scaling, connection lifecycle and the limits of timeout/Stop behavior.

Validation: 27 targeted profile/session and preview-rendering tests passed in Debug, rebuilding the runnable application. All seven published theme previews were refreshed; the 1024x768 preview was visually inspected and shows the new profile label. Strict OpenSpec validation and relative Markdown link checks passed. An independent subagent confirmed the document's byte sequences, formulas and lifecycle descriptions against the code; a remaining label in the measurement report was corrected after its review.
