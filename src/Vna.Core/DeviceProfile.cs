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
    public IReadOnlyList<DeviceCapabilityDescriptor> Capabilities { get; init; } = Array.Empty<DeviceCapabilityDescriptor>();
    public IReadOnlyList<DeviceConnectorDescriptor> Connectors { get; init; } = Array.Empty<DeviceConnectorDescriptor>();
}
