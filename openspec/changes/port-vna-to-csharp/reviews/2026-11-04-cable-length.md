# Review — cable length estimate

Date: 2026-11-04.

- Added a Core calculator using VF × c / (2 × Δf) for adjacent same-type resonances. It validates positive, distinct frequencies and VF in (0, 1].
- Added a Cable Length mode that uses M1/M2 from the Antenna plot, displays the active VF, and lets it be edited. The setting is saved through AppSettings; an invalid stored value falls back to the profile VF.
- The English and Polish UI says to use adjacent resonances of the same type. The documentation identifies the equation as a derivation from reflected round-trip phase, since the supplied manual describes the setup and empirical VF but does not print the equation.
- Added unit and Avalonia tests for order independence, the result, invalid VF, and returning from the mode. Verification: dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts= — 89 passed, 0 failed.
- Remaining: no physical reference cable was supplied, so measurement accuracy and cable-loss behavior remain unverified and unimplemented.
