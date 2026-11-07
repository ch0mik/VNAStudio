# Filter mode

Filter mode is modeled as a scalar response measurement with samples stored in `MeasurementPoint.TransmissionDb`. It does not populate SWR, return loss, phase, or impedance. The chart labels the result as scalar transmission dB rather than S21 because phase and full two-port calibration are unavailable in the recovered device data.

The UI describes the intended fixture: use a through cable between ANT and WOB as the reference, then replace the through connection with the filter. Do not start a hardware filter measurement with WOB left open or with a 50 Ω load attached only to ANT.

The current profile contains the legacy `ReturnLossOffsetFilterDb` from `analyz.ini`, and the conversion applies that offset to scalar ADC magnitude readings. A related SP3SWJ VNA3p2 description says this offset is adjusted so a direct OUT–IN cable reads 0 dB. This is family-level corroboration, not confirmation for every VNA 1–65 MHz hardware revision: [VNA3p2 documentation](https://sp2swj.sp-qrp.pl/VNA3p2/VNA3p2.htm). A related MAX4 manual documents control values `0=VNA` and `1=Wobuloscope`: [VNA MAX4 manual](https://sp2swj.sp-qrp.pl/SP3SWJ/VNA_MAX4_pliki/MANUAL_draft/VNA_MAX4_md_04.pdf). Because command `1` has not been confirmed for the user's VNA 1–65 MHz, the hardware driver rejects Filter sweeps before writing to COM. Demo and session playback remain available.

Consequently, this mode can be evaluated with demo/session data only until the hardware command is confirmed on the target device. For a profile that marks Filter hardware acquisition as blocked or unknown, VNA Studio disables Start scan before it can connect or send a command. Do not infer phase or complex S21 from this scalar response.
