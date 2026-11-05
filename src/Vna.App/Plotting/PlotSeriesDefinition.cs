using Vna.Core;

namespace Vna.App.Plotting;

public sealed record PlotSeriesDefinition(
    PlotTrace Trace,
    PlotAxisScale Axis,
    Func<MeasurementPoint, double?> ReadValue)
{
    public bool TryGetValue(MeasurementPoint point, out double value)
    {
        value = 0;
        if (point.Quality != SampleQuality.Valid || ReadValue(point) is not { } candidate || !double.IsFinite(candidate))
            return false;
        value = candidate;
        return true;
    }
}

/// <summary>Provisional scales; their agreement with legacy hardware captures remains to be verified.</summary>
public static class PlotSeriesCatalog
{
    public static PlotAxisScale Swr { get; } = new("SWR", 1, 10, true, "0.0",
    [
        new(10, "10.0"), new(7.5, "7.5"), new(5, "5.0"), new(2.5, "2.5"), new(1, "1.0"),
    ]);
    public static PlotAxisScale ReturnLoss { get; } = new("RL dB", -30, 0, true, "0",
    [
        new(0, "0"), new(-10, "10"), new(-20, "20"), new(-30, "30"),
    ]);
    public static PlotAxisScale ReturnLossX2 { get; } = new("RL dB", -60, 0, true, "0",
    [
        new(0, "0"), new(-20, "20"), new(-40, "40"), new(-60, "60"),
    ]);
    public static PlotAxisScale CableLoss { get; } = new("Cable loss dB", 0, 30, false, "0.0",
    [
        new(0, "0"), new(5, "5"), new(10, "10"), new(15, "15"), new(20, "20"), new(25, "25"), new(30, "30"),
    ]);
    public static PlotAxisScale Transmission { get; } = new("Transmission dB", -30, 0, true, "0.0",
    [
        new(0, "0"), new(-5, "-5"), new(-10, "-10"), new(-15, "-15"), new(-20, "-20"), new(-25, "-25"), new(-30, "-30"),
    ]);
    public static PlotAxisScale Phase { get; } = new("PHASE", 0, 180, false, "0",
    [
        new(0), new(45), new(90), new(135), new(180),
    ]);
    public static PlotAxisScale Ohms { get; } = new("Ω", 0, 300, false, "0",
    [
        new(0), new(75), new(150), new(225), new(300),
    ]);

    public static IReadOnlyList<PlotSeriesDefinition> All { get; } = Array.AsReadOnly(new PlotSeriesDefinition[]
    {
        new(PlotTrace.Swr, Swr, p => p.Swr),
        new(PlotTrace.ReturnLoss, ReturnLoss, p => p.ReturnLossDb),
        new(PlotTrace.Phase, Phase, p => p.PhaseDegrees),
        new(PlotTrace.Impedance, Ohms, p => p.ImpedanceOhms),
        new(PlotTrace.Resistance, Ohms, p => p.ResistanceOhms),
        new(PlotTrace.Reactance, Ohms, p => p.ReactanceOhms is { } x ? Math.Abs(x) : null),
    });
}
