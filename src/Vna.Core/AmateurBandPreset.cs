namespace Vna.Core;

public enum AmateurBandGroup { Hf, Vhf }

public sealed record AmateurBandPreset(string Name, AmateurBandGroup Group, long StartFrequencyHz, long StopFrequencyHz)
{
    public bool Fits(DeviceProfile profile) =>
        StartFrequencyHz >= profile.MinimumFrequencyHz && StopFrequencyHz <= profile.MaximumFrequencyHz;
}

/// <summary>Polish amateur category 3 allocations used only as sweep range shortcuts.</summary>
public static class PolishAmateurBandPresets
{
    public static IReadOnlyList<AmateurBandPreset> All { get; } =
    [
        new("160 m", AmateurBandGroup.Hf, 1_810_000, 2_000_000),
        new("80 m", AmateurBandGroup.Hf, 3_500_000, 3_800_000),
        new("40 m", AmateurBandGroup.Hf, 7_000_000, 7_200_000),
        new("30 m", AmateurBandGroup.Hf, 10_100_000, 10_150_000),
        new("20 m", AmateurBandGroup.Hf, 14_000_000, 14_350_000),
        new("17 m", AmateurBandGroup.Hf, 18_068_000, 18_168_000),
        new("15 m", AmateurBandGroup.Hf, 21_000_000, 21_450_000),
        new("12 m", AmateurBandGroup.Hf, 24_890_000, 24_990_000),
        new("10 m", AmateurBandGroup.Hf, 28_000_000, 29_700_000),
        new("2 m", AmateurBandGroup.Vhf, 144_000_000, 146_000_000),
    ];
}
