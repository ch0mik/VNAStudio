# Amateur band sweep presets

The band selector is a convenience for setting the sweep start and stop. It does not change the instrument's supported frequency range and it does not enable or authorize transmission.

The bundled preset list uses the Polish amateur allocations published by the Office of Electronic Communications (UKE), for category 3: 160 m 1.810–2.000 MHz, 80 m 3.500–3.800 MHz, 40 m 7.000–7.200 MHz, 30 m 10.100–10.150 MHz, 20 m 14.000–14.350 MHz, 17 m 18.068–18.168 MHz, 15 m 21.000–21.450 MHz, 12 m 24.890–24.990 MHz, 10 m 28.000–29.700 MHz, and 2 m 144–146 MHz.

The UI filters presets against the active device profile before showing them. The supplied 65 MHz profile therefore offers the nine HF presets and excludes 2 m. Presets are region-specific; users should confirm current local rules and their license conditions before transmitting.

## Legacy `analyz.ini` ranges

At startup, VNA Studio loads an `analyz.ini` placed next to the application before using the bundled 65 MHz profile. If that file contains `[BANDS_LIMIT]`, its configured ranges replace the bundled UKE shortcuts. Values are in Hz and each range must provide both endpoints:

```ini
[BANDS_LIMIT]
F_M160s=...
F_M160e=...
F_M80s=...
F_M80e=...
```

The same paired `s` (start) and `e` (end) keys are recognized for 60, 40, 30, 20, 17, 15, 12, 10, 6 and 2 m, plus `F_M70cs`/`F_M70ce` for 70 cm. Incomplete or reversed pairs are rejected; valid ranges outside the device profile are omitted from the selector. The supplied `reference/software/analyz.ini` does not include `[BANDS_LIMIT]`, so no missing legacy values are filled in by the application.

Source: [UKE, Amateur radiocommunication service permits, category 3 frequency allocations](https://bip.uke.gov.pl/jak-uzyskac-rezerwacje--pozwolenie--zezwolenie-tresc/pozwolenia-w-sluzbie-radiokomunikacjnej-amatorskiej,6,74.html).
