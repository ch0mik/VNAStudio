# Manual resonance Q

The Xtal mode provides a manual calculator using the method described in the supplied resonance manual:

    Q = Fres / (Fh − Fl)

For the parallel-resonance procedure, the manual says to set the lower and upper markers at 45° phase on either side of resonance. Enter Fres and the corresponding lower/upper edge frequencies in MHz. The calculator checks positive ordered edges and keeps all three frequencies within the 65 MHz profile. It does not search the plot or certify that the entered edges meet the phase criterion.

The supplied manual's example, Fres = 14.400 MHz, Fl = 14.312 MHz, Fh = 14.4816 MHz, yields Q ≈ 84.9. This physical-method calculator is separate from the marker-based Q hint inferred from the screenshots.

Source: the user-provided resonance measurement manual, page 2. The old manual also says the XTAL automation tab itself was not programmed; VNA Studio therefore presents this as a manual calculator, not as automatic crystal equivalent-circuit extraction. The original manuals are kept outside this repository.
