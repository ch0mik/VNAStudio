# Cable length estimate

The manual describes measuring an open- or short-ended cable and placing markers on adjacent peaks or adjacent dips of the same type. It warns that measurements are poor when the resonance is at or below 60 MHz and recommends resonances above 60 MHz. The supplied instrument profile ends at 65 MHz, so this leaves a narrow recommended region and is a real limitation of this setup. The manual says to determine VF experimentally from a known cable or use the cable's specification; the supplied INI defaults to 0.66. Its examples report about 0.63 for LAN cable and 0.50 for an outdoor extension cord.

VNA Studio implements the estimate:

    length = VF × c / (2 × |f2 − f1|)

where c is the speed of light and the factor of two accounts for the reflected signal's round trip. The manual explains the measurement setup and VF requirement but does not print this equation; the equation is derived from the round-trip phase change between adjacent same-type resonances.

To use it, set M1 and M2 on adjacent resonances in the Antenna plot, choose **Cable Length**, and check or edit the VF. The VF is saved in application settings. The 60 MHz note is guidance recovered from the manual, not an application-enforced cutoff. This is not a cable-loss measurement and does not verify the user's hardware, calibration, or marker selection.

The app validates the selected pair against the current measurement: both markers must be within two sweep samples of an SWR peak or dip, both must select the same kind of extremum, and no other extremum of that kind may lie between them. A three-sample moving average is used only to identify resonance positions; the reported length uses the marker frequencies and is not based on interpolated values. Noise can still create local extrema, so the result remains an estimate and should be checked against a known cable.

Source: the supplied Polish measurement manual, pages 5–6. Its OCR text discusses open/short cable measurement, matching peaks/dips, and determining VF experimentally. The original manual files remain outside this repository with the other user-provided references.
