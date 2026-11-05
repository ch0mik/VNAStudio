# E0 source review — device, format and control evidence

Date: 2026-11-04. This is a source inventory, not a protocol specification.

## First-party local artifacts

- The user-provided local software folder contained the 249,856-byte `analyser306b.exe` and `analyz.ini`; copies and decompilation outputs remain in sibling folders outside this project and are not part of the GitHub repository.
- The INI explicitly describes `Port_type=1` as LPT and `Port_type=2` as RS232; its comment identifies serial mode for original miniVNA / VNA with MEGA8 / MAX LCD. `VNA_type=1` is described as miniVNA or VNA 40 dB. The user's INI selects Port_type 2, VNA_type 1, FMin 1 MHz, FMax 65 MHz and a DDS clock of 191,993,220 Hz. This narrows the hardware family but does not prove this unit's board/firmware revision.
- The supplied driver directory is a generic FTDI package supporting common VID 0403 PIDs; the INF describes USB serial converters and does not identify a VNA-specific protocol.
- The EXE contains MSComm32 controls and port-setting fields, but the recovered strings do not contain baud rate, framing, request bytes, response layout or verified measurement calibration. The main Ghidra export yielded pseudocode for 74/78 VB6 procedure candidates and failed on four. A targeted follow-up recovered the mode-switch handler `VB6_00425e1d`; the calibration and stop/hold candidates are still under separate recovery attempts. Success counts do not demonstrate complete recovery.
- The user's `Pomiary` folder has PDFs, not `.vec` or CSV examples suitable for file-format roundtrip tests.

## Comparison source

The user-provided [pa7n/miniVNA repository](https://github.com/pa7n/miniVNA) includes a Lazarus/FreePascal application and a generic COM-port component. The component documents configurable port/baud/byte-size/parity/stop-bits and send/receive APIs. It is useful for serial lifecycle patterns, but its visible source does not specify the user's Analyser 306b/MEGA8 measurement frames; its existence is not evidence that NanoVNA or a different miniVNA protocol is compatible.

## Remaining unknowns

- Physical device identity/revision, serial line settings, command and response framing, ADC field scaling, signed phase/reactance convention, scan timing and stop/reconnect behavior.
- Full, RL×2 and their exact mapping to axes/readouts; original tick profiles.
- Bands `F/F`, its relation to `HF`/`VHF`, and exact preset edges.
- No COM/FTDI serial port is currently present in this environment, so no passive capture or known-load validation is possible.

No hardware commands or calibration transformations were implemented from this evidence. Device acquisition, filter/generator modes and physical Q remain gated on a verified contract and reference measurements.
