namespace Vna.Core;

/// <summary>Major/minor version of the host/plugin contract, independent of plugin release version.</summary>
public readonly record struct DevicePluginApiVersion(int Major, int Minor);

/// <summary>Declarative identity for a device model profile supplied by a plugin.</summary>
public sealed record DeviceProfileDescriptor(
    string Id,
    string DisplayName,
    int SchemaVersion,
    long MinimumFrequencyHz,
    long MaximumFrequencyHz,
    int DefaultPointCount,
    double? ReferenceImpedanceOhms,
    IReadOnlyList<DeviceCapabilityDescriptor> Capabilities,
    IReadOnlyList<DeviceConnectorDescriptor> Connectors)
{
    /// <summary>Optional neutral frequency presets exposed by this profile for UI workflows.</summary>
    public IReadOnlyList<long> FrequencyPresetsHz { get; init; } = Array.Empty<long>();

    /// <summary>Neutral display and analysis hints; these do not define protocol behavior.</summary>
    public DeviceProfilePresentationDescriptor Presentation { get; init; } =
        DeviceProfilePresentationDescriptor.Empty;
}

/// <summary>Public metadata and supported profiles exposed by one statically registered plugin.</summary>
public sealed record DevicePluginDescriptor(
    string Id,
    string DisplayName,
    DevicePluginApiVersion ApiVersion,
    IReadOnlyList<DeviceProfileDescriptor> Profiles);

/// <summary>
/// Neutral connection input. A system port path, when supplied, is passed to the plugin unchanged;
/// other transports can use Endpoint without introducing transport types into Core.
/// </summary>
public sealed record DeviceConnectionRequest(string? PortName = null, Uri? Endpoint = null);

/// <summary>Logical acquisition path requested by the application, independent of device commands.</summary>
public enum MeasurementAcquisitionMode
{
    Reflection,
    ScalarTransmission,
}

/// <summary>
/// Stable host-facing source contract. Implementations own protocol details and map these neutral
/// requests to their transport; all potentially blocking operations are asynchronous/cancellable.
/// </summary>
public interface IMeasurementSource : IAsyncDisposable
{
    DeviceProfileDescriptor Profile { get; }
    string SourceId { get; }
    /// <summary>Actual configured reflection processing, when the plugin can report it; null means unknown.</summary>
    MeasurementAdjustmentSnapshot? AppliedReflectionCalibration => null;

    long GetSampleFrequencyHz(SweepSettings settings, int index) => settings.FrequencyAt(index);

    ValueTask ConnectAsync(DeviceConnectionRequest request, CancellationToken cancellationToken = default);

    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        MeasurementAcquisitionMode mode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A statically registered device plugin creates sources for its declared profiles. The factory
/// receives no serial-port or transport type; those remain internal to the plugin implementation.
/// </summary>
public interface IDevicePlugin
{
    DevicePluginDescriptor Descriptor { get; }

    IMeasurementSource CreateSource(DeviceProfileDescriptor profile);
}

public static class DevicePluginContract
{
    public static DevicePluginApiVersion CurrentVersion { get; } = new(1, 0);

    /// <summary>
    /// Accepts the same major contract and a plugin minor version no newer than the host's.
    /// Invalid or negative versions are rejected.
    /// </summary>
    public static bool IsCompatible(DevicePluginApiVersion host, DevicePluginApiVersion plugin) =>
        host.Major > 0 && host.Minor >= 0 && plugin.Major == host.Major &&
        plugin.Minor >= 0 && plugin.Minor <= host.Minor;
}
