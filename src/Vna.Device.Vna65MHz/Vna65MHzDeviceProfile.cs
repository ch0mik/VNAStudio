using Vna.Core;

namespace Vna.Device.Vna65MHz;

/// <summary>Built-in compatibility profile data owned by the VNA 1–65 MHz plugin.</summary>
public static class Vna65MHzDeviceProfile
{
    public static DeviceProfile Default { get; } = new(
        Id: LegacyDevicePluginIds.Vna65MHzProfile,
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
        Capabilities =
        [
            new("mode.antenna", DeviceCapabilityKind.AnalysisMode, DeviceCapabilityAvailability.Available,
                DeviceEvidenceLevel.SourceCodeInspected, "Original VNA reflection acquisition path inspected."),
            new("mode.cable-loss", DeviceCapabilityKind.AnalysisMode, DeviceCapabilityAvailability.Partial,
                DeviceEvidenceLevel.ApplicationLogicPresent, "Fixture/calibration gate remains; cable-loss result is not reference-validated."),
            new("tool.cable-length", DeviceCapabilityKind.ApplicationTool, DeviceCapabilityAvailability.Partial,
                DeviceEvidenceLevel.ApplicationLogicPresent, "Marker-based estimate exists; no physical cable validation recorded."),
            new("tool.manual-quartz-q", DeviceCapabilityKind.ApplicationTool, DeviceCapabilityAvailability.Partial,
                DeviceEvidenceLevel.ApplicationLogicPresent, "Manual helper only; automatic quartz analysis is not implemented."),
            new("mode.filter", DeviceCapabilityKind.AnalysisMode, DeviceCapabilityAvailability.Blocked,
                DeviceEvidenceLevel.SourceCodeInspected, "Exact-variant FILTER command, fixture, and independent validation are missing."),
            new("command.filter-sweep", DeviceCapabilityKind.DeviceCommand, DeviceCapabilityAvailability.Blocked,
                DeviceEvidenceLevel.SourceCodeInspected, "Hardware FILTER command is intentionally not sent."),
            new("mode.generator", DeviceCapabilityKind.AnalysisMode, DeviceCapabilityAvailability.Blocked,
                DeviceEvidenceLevel.ProfileDataOnly, "Configured frequency memories do not establish output support."),
            new("command.generator-output", DeviceCapabilityKind.DeviceCommand, DeviceCapabilityAvailability.Blocked,
                DeviceEvidenceLevel.SourceCodeInspected, "Generator output command is disabled."),
            new("tool.scan-cancellation", DeviceCapabilityKind.ApplicationTool, DeviceCapabilityAvailability.Available,
                DeviceEvidenceLevel.ApplicationLogicPresent, "Host-side async cancellation uses CancellationToken; no device cancel command is sent."),
        ],
        Connectors =
        [
            new("connector.vna", "VNA", DeviceConnectorRole.Unknown, DeviceEvidenceLevel.LabelObserved, DeviceEvidenceLevel.Unknown),
            new("connector.wob", "WOB", DeviceConnectorRole.Unknown, DeviceEvidenceLevel.LabelObserved, DeviceEvidenceLevel.Unknown),
        ],
        GeneratorMemoryFrequenciesHz =
        [
            3_600_000, 7_050_000, 10_500_000, 14_100_000, 18_050_000,
            21_050_000, 24_550_000, 27_195_000, 28_050_000, 29_050_000,
        ],
    };

    public static DeviceProfileDescriptor Describe(DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new DeviceProfileDescriptor(
            profile.Id,
            profile.DisplayName,
            SchemaVersion: 1,
            profile.MinimumFrequencyHz,
            profile.MaximumFrequencyHz,
            profile.DefaultPointCount,
            profile.ReferenceImpedanceOhms,
            Array.AsReadOnly(profile.Capabilities.ToArray()),
            Array.AsReadOnly(profile.Connectors.ToArray()))
        {
            FrequencyPresetsHz = Array.AsReadOnly(profile.GeneratorMemoryFrequenciesHz.ToArray()),
            Presentation = new DeviceProfilePresentationDescriptor(
                Array.AsReadOnly(profile.LegacyBandPresets.ToArray()),
                new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Phase"] = ToRgbHex(profile.PlotColors.Phase),
                        ["ReturnLoss"] = ToRgbHex(profile.PlotColors.ReturnLoss),
                        ["Reactance"] = ToRgbHex(profile.PlotColors.Reactance),
                        ["Resistance"] = ToRgbHex(profile.PlotColors.Resistance),
                        ["Impedance"] = ToRgbHex(profile.PlotColors.Impedance),
                        ["Swr"] = ToRgbHex(profile.PlotColors.Swr),
                        ["Background"] = ToRgbHex(profile.PlotColors.Background),
                    }),
                profile.LegacyCalibration.InputVelocityFactor),
        };
    }

    private static string ToRgbHex(uint colorRef) =>
        $"#{(byte)colorRef:X2}{(byte)(colorRef >> 8):X2}{(byte)(colorRef >> 16):X2}";
}
