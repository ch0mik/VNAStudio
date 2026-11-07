using Vna.Core;

namespace Vna.Devices;

/// <summary>Compatibility bridge while the UI migrates from IVnaDevice to IMeasurementSource.</summary>
public sealed class MeasurementSourceDeviceAdapter : IVnaDevice
{
    private readonly IMeasurementSource _source;
    private readonly string _pluginId;

    public MeasurementSourceDeviceAdapter(DeviceProfile profile, IMeasurementSource source, string pluginId)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (!string.Equals(Profile.Id, source.Profile.Id, StringComparison.Ordinal))
            throw new ArgumentException("The source descriptor does not match the compatibility profile.", nameof(source));
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new ArgumentException("A plugin ID is required.", nameof(pluginId));
        _pluginId = pluginId;
    }

    public DeviceProfile Profile { get; }
    public string DataSourceName => _source.SourceId;
    public MeasurementAdjustmentSnapshot? AppliedReflectionCalibration => _source.AppliedReflectionCalibration;
    public string? DevicePluginId => _pluginId;

    public long GetSampleFrequencyHz(SweepSettings settings, int index) =>
        _source.GetSampleFrequencyHz(settings, index);

    public ValueTask ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return _source.ConnectAsync(new DeviceConnectionRequest(options.PortName), cancellationToken);
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default) =>
        _source.DisconnectAsync(cancellationToken);

    public IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        CancellationToken cancellationToken = default) =>
        _source.SweepAsync(settings, MeasurementAcquisitionMode.Reflection, cancellationToken);

    public IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        DeviceSweepMode mode,
        CancellationToken cancellationToken = default) =>
        _source.SweepAsync(settings, mode switch
        {
            DeviceSweepMode.Reflection => MeasurementAcquisitionMode.Reflection,
            DeviceSweepMode.ScalarTransmission => MeasurementAcquisitionMode.ScalarTransmission,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        }, cancellationToken);

    public ValueTask DisposeAsync() => _source.DisposeAsync();
}
