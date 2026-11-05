namespace Vna.App.Plotting;

[Flags]
public enum PlotTrace
{
    None = 0,
    Swr = 1 << 0,
    ReturnLoss = 1 << 1,
    Phase = 1 << 2,
    Impedance = 1 << 3,
    Resistance = 1 << 4,
    Reactance = 1 << 5,
    All = Swr | ReturnLoss | Phase | Impedance | Resistance | Reactance,
}
