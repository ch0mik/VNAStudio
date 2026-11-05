# Serial protocol support

Detailed wire-level walkthrough: [English](VNA-COMMUNICATION.md) · [Polski](VNA-COMMUNICATION.pl.md).

VNA Studio contains a source-derived serial driver for the miniVNA/Analyser text-command protocol. The UI explicitly selects Demonstration, Hardware, or Session playback and does not fall back to demo data after a hardware connection error. Serial scan acquisition and Stop/reconnect were exercised on the user's physical VNA on COM9 on 2026-11-04. Basic RF scaling was checked with the user's 50-ohm termination and independently compared with the original RF algebra. Absolute measurement accuracy and calibration remain unestablished; see the [50-ohm report](measurements/2026-11-04-com9-50ohm.md).

## Evidence and variants

- The [PA7N miniVNA client](https://github.com/pa7n/miniVNA/blob/master/frmmain.pas) opens the serial connection at 115200 baud, 8 data bits, no parity and one stop bit. It sends four carriage-return-terminated ASCII values for each sweep: mode `0`, the start DDS tuning word, point count and step tuning word. The frequency word is based on the DDS phase accumulator (`2^32 / DDS clock`). The client reads four response bytes per sample.
- The local `analyser306b.exe` also stores 115200 as a little-endian 32-bit integer (`00 C2 01 00`) at file offset `0xFC32`, inside the persisted `MSComm1` control data starting at `0xFBE0`. This supports the configured baud rate independently of the PA7N client. A flashing USB TX LED when typing at 9600 baud confirms outgoing USB traffic, but does not establish that the firmware decoded a command or returned measurement data.
- `VNA_type` and `SVNA` are distinct INI flags. In the original initialization routine, `004168b4` reads key `VNA_type` into `0043b178`; `0041698c` reads key `SVNA` into `0043b17a`. The receive routine `VB6_0041cf8d` selects six bytes only for `0043b17a == 1`. The supplied INI has `VNA_type=1` and no `SVNA` key, so the correct frame is four bytes. The initial implementation incorrectly used VNA_type to select six bytes; this was corrected after physical testing received 64 rather than 96 bytes for 16 points. The first four bytes encode two little-endian 10-bit ADC channels. Any additional SVNA channel remains unverified on hardware.
- The local hardware configuration and Ghidra output remain outside this project in the sibling `../reference/` and `../decompiled/` directories, as requested.

The driver chooses the 4- or 6-byte frame from `PORTSETTING:SVNA` (default 0, four bytes); the bundled 65 MHz profile explicitly uses `SVNA=0` and retains `VNA_type=1`. It derives each point's displayed frequency from the DDS tuning word actually sent. The legacy scan step is `(stop-start)/pointCount`, so the last sample is below the requested stop frequency, matching the reference client. `IVnaDevice.GetSampleFrequencyHz` lets the view model validate each source against its own declared grid; this avoids rejecting a complete hardware scan because the requested stop is not an emitted DDS sample.

## Conversion

For the two 10-bit channels, the driver uses `low + high × 256`, where `high` must be 0–3. The first channel is phase and the second magnitude. The initial implementation incorrectly used a universal magnitude −20 dB base and ignored VNA_type polarity, yielding positive signed S11 and missing SWR. The recovered RF exponent in `0041ad14–0041ad89`, together with the VNA_type setup in `0042c878–0042c8ba`, establishes:

```text
M = magnitudeADC * Adc_magres * RL_a + RL_b
signedS11Db = (M - 30 - typeOffset) * polarity - RL_OFFSET_vna
type 0: typeOffset = 0,     polarity = +1
type 1: typeOffset = -9.84, polarity = -1
```

The affine coefficients precede reference subtraction and polarity. For the supplied type-1 profile and zero offsets, this reduces to `signedS11Db = 20.16 - magnitudeADC * Adc_magres`. The original subsequently stores inverse reflection amplitude and takes absolute values of R/X/SWR; using conventional reflection amplitude reproduces R, |X|, |Z| and SWR for the default profile. VNA Studio applies phase as `phaseADC * Adc_phres * PH_a + PH_b` in degrees. Nonzero PH_b differs from the original RF path's placement in radians; this case and the physical sign of reactance remain unverified. The original CSV and RF paths also differ in nonzero RL-offset placement; VNA Studio follows the RF magnitude path.

The UI's separate additional RL offset is applied to a derived display copy and recalculates SWR/impedance; session sample data remains untouched. Missing or malformed frames fail the sweep; a timeout or cancellation closes the serial port so leftover bytes cannot be mistaken for the next sweep.

The transport path remains an opaque user setting (`COM5`, `/dev/ttyUSB0`, and similar). Line settings, timeout and frame selection are typed options/profile data. The configured `STEP_PAUSE` is rejected when nonzero because this recovered serial sweep protocol has no verified way to apply a delay between DDS points.

## Recovered legacy mode-switch behavior

A targeted Ghidra recovery of Analyser 306b procedure `VB6_00425e1d` identifies the six analysis-mode names and contains the message `Calibration RL OFF - Mode change`. Its control flow clears the active calibration state when switching away from a calibrated mode. This confirms a user-facing mode-change rule, but does not recover the calibration formula, reference acquisition, or device protocol; VNA Studio keeps calibration processing unavailable until those are established.

A second targeted recovery, `VB6_0043094f`, contains a user prompt to connect a LOOP cable, then iterates the current sample count while collecting working-array values and reports `Calibration 0dB done.` It also contains `Calibration RL OFF.` and `Auto calibration OFF.` paths, and updates plot labels for 0/20/40/60 dB or 0/10/20/30 dB scales. This is evidence for a loop-reference calibration action and two return-loss display ranges. The recovered branch sets the corresponding graph range labels and limits; the checkbox-to-branch mapping is not symbolized, but the source screenshot labels the option RL×2. VNA Studio implements that option as a display-only 0–60 dB axis choice. The decompiler did not establish the meaning/units of the calibration arrays or the correction formula. VNA Studio must not subtract the stored values from measurements or claim a calibrated RL until that relation is recovered and checked against a known load and the original application.

## Verification status

Close serial terminals such as PuTTY and other analyser applications before connecting: an open terminal can occupy the port. Port enumeration and registry mappings do not prove that a second application can open the port. A failed `mode COM9` query alone does not establish a missing driver or disconnected device. VNA Studio reports opening the serial port separately from receiving a valid measurement frame.

Physical tests on COM9 at 115200/8N1:

- Before the fix, a 16-point scan at 1–2 MHz received 64 bytes and timed out waiting for 96.
- After the fix, the same scan completed with 16 points and 64 bytes.
- A scan at 1–65 MHz through `MainWindowViewModel.StartCommand` completed with 649 points and 2596 bytes, and produced a session with 649 samples.
- `MainWindowViewModel.StopCommand` cancelled an in-progress serial read, closed the connection and committed no incomplete samples; a subsequent scan completed with 649 points and 2596 bytes.

Raw captures and diagnostic projects are archived outside the public source tree. This document retains the summarized hardware evidence. Automated tests additionally check framing independent of VNA_type, INI binding, command bytes, DDS frequencies and lifecycle behavior. These checks establish acquisition and application flow, not RF accuracy or compatibility of the six-byte SVNA variant.
