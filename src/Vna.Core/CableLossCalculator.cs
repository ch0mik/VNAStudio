namespace Vna.Core;

/// <summary>Calculates one-way loss from a reflected signal with an open or short at the cable far end.</summary>
public static class CableLossCalculator
{
    /// <summary>
    /// A fully reflected wave travels through the cable twice, so one-way loss is half the magnitude
    /// of signed S11 in dB. The caller must confirm the open/short fixture and use calibrated data.
    /// </summary>
    public static double? CalculateOneWayDb(double? signedReturnLossDb)
    {
        if (signedReturnLossDb is not { } value || !double.IsFinite(value) || value > 0) return null;
        return -value / 2;
    }
}
