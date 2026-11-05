namespace Vna.Core;

/// <summary>Confirmed settings read from the user's analyz.ini. They are not a hardware probe.</summary>
public sealed record DeviceProfile(
    string Id,
    string DisplayName,
    long MinimumFrequencyHz,
    long MaximumFrequencyHz,
    int DefaultPointCount,
    long DdsClockHz,
    double ReferenceImpedanceOhms,
    double MagnitudeAdcDbPerCount,
    double PhaseAdcDegreesPerCount)
{
    /// <summary>Legacy VNA measurement variant; independent of serial frame length.</summary>
    public int LegacyVnaType { get; init; }
    /// <summary>The separate legacy SVNA flag selects six bytes instead of four per serial sample.</summary>
    public bool LegacySvna { get; init; }
    public IReadOnlyList<long> GeneratorMemoryFrequenciesHz { get; init; } = Array.Empty<long>();
    public IReadOnlyList<AmateurBandPreset> LegacyBandPresets { get; init; } = Array.Empty<AmateurBandPreset>();
    public int DdsPllPrescaler { get; init; } = 1;
    public int StartPauseMilliseconds { get; init; } = 10;
    public int StepPauseMilliseconds { get; init; }
    public LegacyCalibrationSettings LegacyCalibration { get; init; } = LegacyCalibrationSettings.Default;
    public LegacyPlotColorSettings PlotColors { get; init; } = LegacyPlotColorSettings.Default;

    public static DeviceProfile Vna65MHz { get; } = new(
        Id: "iw3hev-65mhz",
        DisplayName: "VNA 1-65 MHz",
        MinimumFrequencyHz: 1_000_000,
        MaximumFrequencyHz: 65_000_000,
        DefaultPointCount: 649,
        DdsClockHz: 191_993_220,
        ReferenceImpedanceOhms: 50,
        MagnitudeAdcDbPerCount: 0.058651026,
        PhaseAdcDegreesPerCount: 0.175953079)
    {
        LegacyVnaType = 1,
        DdsPllPrescaler = 1,
        StartPauseMilliseconds = 10,
        StepPauseMilliseconds = 0,
        LegacyCalibration = new LegacyCalibrationSettings(
            ReturnLossOffsetVnaDb: 0,
            ReturnLossOffsetFilterDb: 15.5,
            ReturnLossA: 1,
            ReturnLossB: 0,
            PhaseA: 1,
            PhaseB: 0,
            InputVelocityFactor: 0.66),
        PlotColors = LegacyPlotColorSettings.Default,
        GeneratorMemoryFrequenciesHz =
        [
            3_600_000, 7_050_000, 10_500_000, 14_100_000, 18_050_000,
            21_050_000, 24_550_000, 27_195_000, 28_050_000, 29_050_000,
        ],
    };
}
