namespace Vna.Core;

public enum SessionAnalysisMode { Antenna, CableLoss, CableLength, Xtal, Filter, Generator }

/// <summary>How the saved sample labels relate to the requested DDS sweep. Unknown is the safe default.</summary>
public enum FrequencyGridConvention
{
    Unknown,
    StartInclusive,
    StartPlusStep,
}

/// <summary>Physical data meaning; this is independent of the UI analysis mode.</summary>
public enum MeasurementDataKind
{
    Unknown,
    ReflectionDerived,
    ScalarTransmissionMagnitude,
}

public sealed record MeasurementSession(
    int FormatVersion,
    string ProfileId,
    string DataSource,
    DateTimeOffset CapturedAtUtc,
    SweepSettings Sweep,
    IReadOnlyList<MeasurementPoint> Points)
{
    /// <summary>Optional source plugin identity. Null is retained for synthetic or unidentified data.</summary>
    public string? PluginId { get; init; }
    /// <summary>Neutral profile metadata used to render historical data without the source plugin.</summary>
    public DeviceProfileDescriptor? ProfileDescriptorSnapshot { get; init; }
    /// <summary>Legacy profile shape retained only for reading and preserving older session records.</summary>
    public DeviceProfile? ProfileSnapshot { get; init; }
    public SessionAnalysisMode AnalysisMode { get; init; } = SessionAnalysisMode.Antenna;

    /// <summary>
    /// Records the interpretation of device-generated sample labels. This never changes the
    /// individual FrequencyHz values stored in Points; legacy sessions default to Unknown.
    /// </summary>
    public FrequencyGridConvention FrequencyGridConvention { get; init; } = FrequencyGridConvention.Unknown;

    /// <summary>Distinguishes reflection-derived data from scalar transmission magnitude.</summary>
    public MeasurementDataKind MeasurementDataKind { get; init; } = MeasurementDataKind.Unknown;

    /// <summary>Processing provenance for this sweep; absent in legacy/imported sessions.</summary>
    public MeasurementCalibrationSnapshot? CalibrationSnapshot { get; init; }

    /// <summary>
    /// Whether this session has evidence that its samples represent reflection data suitable
    /// for reflection and Smith-chart comparison. Legacy FILTER sessions with unknown kind
    /// remain excluded because older files do not identify scalar transmission explicitly.
    /// </summary>
    public bool IsReflectionComparisonCompatible =>
        this.MeasurementDataKind != global::Vna.Core.MeasurementDataKind.ScalarTransmissionMagnitude &&
        !(this.MeasurementDataKind == global::Vna.Core.MeasurementDataKind.Unknown &&
          AnalysisMode == SessionAnalysisMode.Filter);
}
