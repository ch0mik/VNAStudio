using System.Collections.ObjectModel;

namespace Vna.Core;

/// <summary>
/// A user-named, immutable copy of a completed measurement session. The original session
/// metadata and frequency samples are retained; snapshots do not resample or transform data.
/// </summary>
public sealed record NamedMeasurementSnapshot
{
    private NamedMeasurementSnapshot(string name, MeasurementSession session, double displayReturnLossOffsetDb)
    {
        Name = name;
        Session = session;
        DisplayReturnLossOffsetDb = displayReturnLossOffsetDb;
    }

    public string Name { get; }
    public MeasurementSession Session { get; }
    /// <summary>Display-only return-loss adjustment captured for this chart view; it does not alter Session.Points.</summary>
    public double DisplayReturnLossOffsetDb { get; }

    public static NamedMeasurementSnapshot Create(string name, MeasurementSession session, double displayReturnLossOffsetDb = 0)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(session.Points);
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName) || normalizedName.Length > 120)
            throw new ArgumentException("Snapshot name must contain 1 to 120 characters.", nameof(name));
        if (!double.IsFinite(displayReturnLossOffsetDb))
            throw new ArgumentOutOfRangeException(nameof(displayReturnLossOffsetDb));

        return new NamedMeasurementSnapshot(normalizedName, Freeze(session), displayReturnLossOffsetDb);
    }

    private static MeasurementSession Freeze(MeasurementSession session) => session with
    {
        Points = Array.AsReadOnly(session.Points.ToArray()),
        ProfileDescriptorSnapshot = session.ProfileDescriptorSnapshot is { } descriptor
            ? Freeze(descriptor)
            : null,
        ProfileSnapshot = session.ProfileSnapshot is { } profile
            ? Freeze(profile)
            : null,
        CalibrationSnapshot = session.CalibrationSnapshot is { } calibration
            ? Freeze(calibration)
            : null,
    };

    private static DeviceProfileDescriptor Freeze(DeviceProfileDescriptor descriptor) => descriptor with
    {
        FrequencyPresetsHz = Array.AsReadOnly(descriptor.FrequencyPresetsHz.ToArray()),
        Capabilities = Array.AsReadOnly(descriptor.Capabilities.ToArray()),
        Connectors = Array.AsReadOnly(descriptor.Connectors.ToArray()),
        Presentation = descriptor.Presentation with
        {
            FrequencyBands = Array.AsReadOnly(descriptor.Presentation.FrequencyBands.ToArray()),
            TraceColors = new ReadOnlyDictionary<string, string>(
                descriptor.Presentation.TraceColors.ToDictionary(
                    pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)),
        },
    };

    private static DeviceProfile Freeze(DeviceProfile profile) => profile with
    {
        GeneratorMemoryFrequenciesHz = Array.AsReadOnly(profile.GeneratorMemoryFrequenciesHz.ToArray()),
        LegacyBandPresets = Array.AsReadOnly(profile.LegacyBandPresets.ToArray()),
        Capabilities = Array.AsReadOnly(profile.Capabilities.ToArray()),
        Connectors = Array.AsReadOnly(profile.Connectors.ToArray()),
    };

    private static MeasurementCalibrationSnapshot Freeze(MeasurementCalibrationSnapshot snapshot) => snapshot with
    {
        Adjustments = Array.AsReadOnly(snapshot.Adjustments.Select(adjustment => adjustment with
        {
            Parameters = Array.AsReadOnly(adjustment.Parameters.ToArray()),
        }).ToArray()),
    };
}
