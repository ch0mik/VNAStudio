# COM9 physical scan and framing correction

Date: 2026-11-04 (Europe/Warsaw).

The user authorized testing a scan against the connected VNA. Tests used COM9, 115200/8N1 and the actual MiniVnaSerialDevice/SerialPortTransport. The full scan and Stop/restart additionally used MainWindowViewModel.StartCommand and StopCommand with the hardware router and default 65 MHz profile. No demo measurements were substituted.

## Root cause and source evidence

The initial driver treated VNA_type=1 as a six-byte sample. The physical VNA returned 64 bytes for 16 points and the driver timed out waiting for 96.

The original initialization ASM distinguishes the keys:

- `004168b4`: key at `00404914` (VNA_type), stored at `0043b178` by `004168e1`.
- `0041698c`: key at `0040492c` (SVNA), stored at `0043b17a` by `004169b9`.
- Receive procedure `VB6_0041cf8d` chooses a stride of six only if `0043b17a == 1`; otherwise it uses four.

The supplied INI has VNA_type=1 and omits SVNA. The missing flag defaults to zero. The corrected profile adds LegacySvna independently of LegacyVnaType, binds PORTSETTING:SVNA and defaults the bundled profile to SVNA=0. Existing session snapshots without the new field also default to false. No inference from VNA_type now changes frame length.

## Physical results

| Scenario | Result |
| --- | --- |
| Before fix: 1–2 MHz, 16 points | Received 64 bytes; timeout waiting for 96 |
| After fix: 1–2 MHz, 16 points | Completed, 64 bytes, 16 decoded samples |
| Application Start: 1–65 MHz, 649 points | Completed, 2596 bytes, 649 samples committed and present in session |
| Application Stop during serial read | Scan ended, port closed, zero incomplete samples committed, no reported error |
| Application Start immediately after Stop | Completed, 2596 bytes, 649 samples |

Local diagnostic projects and captured JSON/raw binary reports are archived outside the public source tree. Original binaries and decompilation remain outside VnaStudio.

## Review and limitations

An independent subagent reviewed the flag mapping, profile/snapshot default, INI binding, driver stride, regression tests and physical reports. No blocking issue was found in the framing correction.

Validation: the full Release suite passed 130/130 tests; strict OpenSpec validation passed. Regression coverage explicitly checks four-byte frames for both VNA_type values and the separate SVNA INI flag.

These results establish communication, frame acquisition, committing a scan, cancellation and reopening. They do not validate RF accuracy. The current conversion returns positive signed RL and null SWR for some captured samples, so ADC channel mapping, VNA_type-dependent offset/polarity and calibration remain an explicit follow-up. No known load was identified and no measurement comparison with the original application was made. Six-byte SVNA hardware was not tested.
