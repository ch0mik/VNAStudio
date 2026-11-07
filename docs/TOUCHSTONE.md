# Touchstone import and export

The `Vna.IO` library reads and writes a Touchstone v1.x S-parameter subset for one- and two-port files (`.s1p` and `.s2p`). The app does not yet expose a UI import/export command. Imported samples are stored as complex values with frequency in Hz and one explicit, common reference resistance (`Zref`). No impedance renormalization is performed.

The reader accepts `RI`, `MA`, and `DB` complex-value encodings, frequency units `Hz`, `kHz`, `MHz`, or `GHz`, comments beginning with `!`, and a shared reference resistance from the `R` option. If option-line values are omitted, it applies the v1 defaults: GHz, S parameters, MA, and 50 ohms. Per-port references are accepted only when they are numerically equal, because the current model stores one common `Zref`; a multi-value `R` entry must be the last option-line entry as required by Touchstone v1.1.

The writer emits `# Hz S RI R <Zref>`. S2P values are written in the v1 order `S11, S21, S12, S22`. Use `.s1p` for a one-port dataset and `.s2p` for a two-port dataset.

Touchstone 2.0/2.1 keyword files, parameters other than S, mixed-mode data, unequal per-port references, and more than two ports are rejected. Files are limited to 1,000,000 samples. Scalar transmission magnitude is never converted to S21 or another complex parameter.

The option-line defaults, supported frequency units/formats, comments, and two-port data order follow the [IBIS Open Forum Touchstone File Format Specification 2.1](https://ibis.org/touchstone_ver2.1/touchstone_ver2_1.pdf), which also documents the version-specific keywords and per-port reference features that this adapter does not yet support.
