namespace Vna.App.ViewModels;

/// <summary>Display-only calculations derived from marker positions.</summary>
public static class MarkerCalculations
{
    /// <summary>
    /// Estimates the screenshot's marker Q hint as rounded midpoint frequency divided by marker bandwidth.
    /// This is not a resonance measurement and must not be presented as calibrated physical Q.
    /// </summary>
    public static int? EstimateMarkerQ(long? firstFrequencyHz, long? secondFrequencyHz)
    {
        if (firstFrequencyHz is not > 0 || secondFrequencyHz is not > 0 || firstFrequencyHz == secondFrequencyHz)
            return null;

        var first = (double)firstFrequencyHz.Value;
        var second = (double)secondFrequencyHz.Value;
        var bandwidth = Math.Abs(second - first);
        var estimate = ((first + second) / 2d) / bandwidth;
        if (!double.IsFinite(estimate) || estimate < 0 || estimate > int.MaxValue) return null;
        return (int)Math.Round(estimate, MidpointRounding.AwayFromZero);
    }
}
