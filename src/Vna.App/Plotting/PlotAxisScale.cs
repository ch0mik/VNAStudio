namespace Vna.App.Plotting;

public sealed record PlotAxisTick(double Value, string? Label = null);

/// <summary>Linear scale shared by curve coordinates and tick labels.</summary>
public sealed class PlotAxisScale
{
    public PlotAxisScale(string label, double minimum, double maximum, bool maximumAtTop, string tickFormat,
        IReadOnlyList<PlotAxisTick>? ticks = null)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) ||
            maximum <= minimum || !double.IsFinite(maximum - minimum))
            throw new ArgumentOutOfRangeException(nameof(maximum));
        Label = label;
        Minimum = minimum;
        Maximum = maximum;
        MaximumAtTop = maximumAtTop;
        TickFormat = tickFormat;
        Ticks = ticks ?? Enumerable.Range(0, 6)
            .Select(index => new PlotAxisTick(ValueAtFractionFromTop(index / 5d)))
            .ToArray();
    }

    public string Label { get; }
    public double Minimum { get; }
    public double Maximum { get; }
    public bool MaximumAtTop { get; }
    public string TickFormat { get; }
    public IReadOnlyList<PlotAxisTick> Ticks { get; }

    public double FractionFromTop(double value)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        var fraction = Math.Clamp((value - Minimum) / (Maximum - Minimum), 0, 1);
        return MaximumAtTop ? 1 - fraction : fraction;
    }

    public double ValueAtFractionFromTop(double fraction)
    {
        if (!double.IsFinite(fraction)) throw new ArgumentOutOfRangeException(nameof(fraction));
        fraction = Math.Clamp(fraction, 0, 1);
        return Minimum + (Maximum - Minimum) * (MaximumAtTop ? 1 - fraction : fraction);
    }
}
