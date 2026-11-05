namespace Vna.Core;

/// <summary>Chart color values copied from the legacy INI as Windows COLORREF integers.</summary>
public sealed record LegacyPlotColorSettings(
    uint Phase,
    uint ReturnLoss,
    uint Reactance,
    uint Resistance,
    uint Impedance,
    uint Swr,
    uint Background)
{
    public static LegacyPlotColorSettings Default { get; } = new(
        Phase: 165335,
        ReturnLoss: 16711680,
        Reactance: 32768,
        Resistance: 65305,
        Impedance: 16521960,
        Swr: 255,
        Background: 15790320);
}
