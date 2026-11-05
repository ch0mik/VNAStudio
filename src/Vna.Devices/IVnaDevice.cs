using Vna.Core;

namespace Vna.Devices;

public enum DeviceSweepMode
{
    Reflection,
    ScalarTransmission,
}

/// <summary>Transport-independent contract for hardware, demonstration and playback measurement sources.</summary>
public interface IVnaDevice : IAsyncDisposable
{
    DeviceProfile Profile { get; }
    string DataSourceName { get; }
    /// <summary>Returns the frequency grid that this source is expected to emit for a sweep.</summary>
    long GetSampleFrequencyHz(SweepSettings settings, int index) => settings.FrequencyAt(index);
    ValueTask ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken = default);
    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        DeviceSweepMode mode,
        CancellationToken cancellationToken = default) =>
        mode == DeviceSweepMode.Reflection
            ? SweepAsync(settings, cancellationToken)
            : throw new NotSupportedException("The selected device does not support scalar transmission sweeps.");
}
