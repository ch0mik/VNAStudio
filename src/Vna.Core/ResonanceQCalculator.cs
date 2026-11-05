namespace Vna.Core;

/// <summary>Calculates Q from resonance frequency and two bandwidth edge frequencies.</summary>
public static class ResonanceQCalculator
{
    public static double Calculate(long resonanceFrequencyHz, long lowerEdgeHz, long upperEdgeHz)
    {
        if (resonanceFrequencyHz <= 0) throw new ArgumentOutOfRangeException(nameof(resonanceFrequencyHz));
        if (lowerEdgeHz <= 0) throw new ArgumentOutOfRangeException(nameof(lowerEdgeHz));
        if (upperEdgeHz <= lowerEdgeHz) throw new ArgumentOutOfRangeException(nameof(upperEdgeHz));
        if (resonanceFrequencyHz < lowerEdgeHz || resonanceFrequencyHz > upperEdgeHz)
            throw new ArgumentException("The resonance frequency must lie between the bandwidth edges.", nameof(resonanceFrequencyHz));

        return (double)resonanceFrequencyHz / ((double)upperEdgeHz - lowerEdgeHz);
    }
}
