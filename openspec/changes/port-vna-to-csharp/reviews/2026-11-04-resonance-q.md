# Review — manual resonance Q calculator

Date: 2026-11-04.

- Added a separately named calculator for Q = Fres / (Fh − Fl) with ordered lower/upper edges and a resonance frequency within those edges.
- Xtal mode gives the source method: for the parallel resonance procedure, enter the lower and upper phase crossings at −45°/+45° around resonance. Inputs are MHz and are limited to the 65 MHz profile.
- The formula and example reproduce the supplied resonance manual's 14.4 MHz example (Q 84.9). The app requires the operator to enter the resonance and edge frequencies; it does not claim to detect phase crossings automatically.
- This is a manual analysis tool. The separate marker-based Q estimate remains labelled as a screenshot-derived estimate.
- Additional procedure: docs/RESONANCE-Q.md.
- Verification: dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts= — 94 passed, 0 failed.
- The supplied measurement manual explicitly says the XTAL automation tab was not programmed. No automated crystal equivalent-circuit extraction was added.
