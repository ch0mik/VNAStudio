# One port cable loss estimate

The Cable Loss view uses a reflection sweep on ANT. The cable is connected at ANT and its far end is left open or shorted. For each valid sample the displayed estimate is:

```text
one-way loss (dB) = − signed ReturnLossDb / 2
```

`ReturnLossDb` uses the signed reflection convention `20 log10(|Γ|)` and is zero or negative. A wave travels to the cable end and back, so its attenuation is twice the one-way cable attenuation. This relationship assumes the far end reflects nearly all incident power and the measurement reference is stable.

This is an estimate, not a calibrated insertion-loss measurement. Connector mismatch, fixture loss, radiation, cable-end quality, and frequency-dependent instrument errors remain in the result. A 50 Ω terminator at the cable end is not a valid fixture for this method: it absorbs the signal instead of reflecting it. The view is hidden until the operator confirms an open/short cable fixture. Demo samples and sessions without hardware Cable Loss provenance cannot be treated as cable data and will not enable the estimate.

The supplied hardware manual does not specify a Cable Loss calculation or calibration procedure. The external VB6 recovery finds the `fraCableLoss` frame and `CABLE LOSS dB` label, and the mode handler switches mode-specific controls, but the available pseudocode does not reveal a loss formula or calibration reference. Therefore `−ReturnLossDb / 2` is a VNA Studio one-port estimate, not a verified port of the original algorithm.

The half-return-loss relationship is a standard one-port cable-loss method (see [Anritsu MS2034A Measurement Guide](https://eetac.upc.edu/ca/lescola/Equipaments/laboratoris/manuals/analitzador-espectres/anritsu-ms2034a-measurement-guide-10580-00289f.pdf)). A physical comparison using a known cable remains pending.

For a direct calibrated transmission measurement, a two-port-capable setup can measure insertion loss from ANT through the cable to WOB; that is a different mode and fixture.
