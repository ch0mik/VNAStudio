# Axis tick implementation review

- Implemented independent value ticks for phase, impedance and return loss rather than forcing every axis to share six labels.
- The supplied screenshots show phase ticks at 0/45/90/135/180 degrees, impedance ticks at 0/75/150/225/300 ohms and return-loss magnitude labels 0/10/20/30 dB while measurement values remain signed and negative.
- Tick y positions use `PlotAxisScale.FractionFromTop`, the same transform used for trace geometry. Tests assert values, orientation and representative pixel positions.
- The grid retains six equal subdivisions. SWR tick mapping is still provisional because the screenshots do not establish the transfer function; Full and RL×2 remain unresolved pending original event behavior.
- Verification: `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=`.
