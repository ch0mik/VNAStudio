using Vna.Core;

namespace Vna.Devices;

/// <summary>Explicitly routes scans to the selected source and never falls back after an error.</summary>
public sealed class VnaDeviceRouter : IVnaDevice, IDeviceSourceSelector, ISessionPlaybackSource
{
    private readonly IVnaDevice _demo;
    private readonly IVnaDevice _hardware;
    private readonly IVnaDevice? _playback;
    private IVnaDevice? _connectedDevice;
    private bool _disposed;

    public VnaDeviceRouter(IVnaDevice demo, IVnaDevice hardware, DeviceSourceMode initialMode = DeviceSourceMode.Demo,
        IVnaDevice? playback = null)
    {
        _demo = demo ?? throw new ArgumentNullException(nameof(demo));
        _hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
        _playback = playback;
        if (!string.Equals(_demo.Profile.Id, _hardware.Profile.Id, StringComparison.Ordinal))
            throw new ArgumentException("All routed VNA sources must use the same device profile.");
        if (_playback is not null && !string.Equals(_demo.Profile.Id, _playback.Profile.Id, StringComparison.Ordinal))
            throw new ArgumentException("All routed VNA sources must use the same device profile.");
        if (!Enum.IsDefined(initialMode) || initialMode == DeviceSourceMode.Playback && _playback is null)
            throw new ArgumentOutOfRangeException(nameof(initialMode));
        SourceMode = initialMode;
    }

    public DeviceSourceMode SourceMode { get; private set; }
    public DeviceProfile Profile => CurrentDevice.Profile;
    public string DataSourceName => CurrentDevice.DataSourceName;
    public long GetSampleFrequencyHz(SweepSettings settings, int index) =>
        (_connectedDevice ?? CurrentDevice).GetSampleFrequencyHz(settings, index);

    public void SetSession(MeasurementSession session)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_playback is ISessionPlaybackSource playback)
            playback.SetSession(session);
    }

    public bool SelectSource(DeviceSourceMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Enum.IsDefined(mode) || mode == DeviceSourceMode.Playback && _playback is null || _connectedDevice is not null) return false;
        SourceMode = mode;
        return true;
    }

    public async ValueTask ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_connectedDevice is not null)
            throw new InvalidOperationException("A VNA source is already connected.");
        var selected = CurrentDevice;
        await selected.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
        _connectedDevice = selected;
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        var connected = _connectedDevice;
        if (connected is not null)
        {
            await connected.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            _connectedDevice = null;
        }
    }

    public IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        CancellationToken cancellationToken = default) =>
        (_connectedDevice ?? throw new InvalidOperationException("Connect the selected source before scanning."))
        .SweepAsync(settings, cancellationToken);

    public IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        DeviceSweepMode mode,
        CancellationToken cancellationToken = default) =>
        (_connectedDevice ?? throw new InvalidOperationException("Connect the selected source before scanning."))
        .SweepAsync(settings, mode, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        List<Exception>? failures = null;
        await DisposePartAsync(DisconnectAsync().AsTask()).ConfigureAwait(false);
        await DisposePartAsync(_hardware.DisposeAsync().AsTask()).ConfigureAwait(false);
        await DisposePartAsync(_demo.DisposeAsync().AsTask()).ConfigureAwait(false);
        if (_playback is not null) await DisposePartAsync(_playback.DisposeAsync().AsTask()).ConfigureAwait(false);
        if (failures is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 }) throw new AggregateException("One or more VNA sources failed to dispose.", failures);

        async Task DisposePartAsync(Task operation)
        {
            try { await operation.ConfigureAwait(false); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
    }

    private IVnaDevice CurrentDevice => SourceMode switch
    {
        DeviceSourceMode.Demo => _demo,
        DeviceSourceMode.Hardware => _hardware,
        DeviceSourceMode.Playback => _playback ?? throw new InvalidOperationException("Playback is not configured."),
        _ => throw new ArgumentOutOfRangeException(nameof(SourceMode)),
    };
}
