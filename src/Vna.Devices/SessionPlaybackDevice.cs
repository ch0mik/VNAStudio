using System.Runtime.CompilerServices;
using Vna.Core;

namespace Vna.Devices;

/// <summary>Replays an exact saved sweep without interpolation or hardware access.</summary>
public sealed class SessionPlaybackDevice(DeviceProfile profile) : IVnaDevice, ISessionPlaybackSource
{
    private MeasurementSession? _session;
    private DeviceProfileDescriptor? _sessionProfileDescriptor;
    private bool _connected;
    private bool _disposed;

    public DeviceProfile Profile { get; } = profile ?? throw new ArgumentNullException(nameof(profile));
    public string DataSourceName => "playback";
    public string? DevicePluginId => _session?.PluginId;

    public void SetSession(MeasurementSession session)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(session);
        if (session.ProfileSnapshot is { } savedLegacySnapshot &&
            !string.Equals(session.ProfileId, savedLegacySnapshot.Id, StringComparison.Ordinal))
            throw new ArgumentException("Playback session profile snapshot does not match its profile ID.", nameof(session));
        var sessionDescriptor = session.ProfileDescriptorSnapshot;
        if (sessionDescriptor is not null)
        {
            if (!string.Equals(session.ProfileId, sessionDescriptor.Id, StringComparison.Ordinal))
                throw new ArgumentException("Playback session profile descriptor does not match its profile ID.", nameof(session));
            session.Sweep.ValidateAgainst(sessionDescriptor);
        }
        else if (session.ProfileSnapshot is { } legacySnapshot)
        {
            session.Sweep.ValidateAgainst(legacySnapshot);
        }
        else
        {
            if (!string.Equals(session.ProfileId, Profile.Id, StringComparison.Ordinal))
                throw new ArgumentException("Playback session requires a profile descriptor for offline playback.", nameof(session));
            session.Sweep.ValidateAgainst(Profile);
        }
        if (session.Points.Count != session.Sweep.PointCount)
            throw new ArgumentException("Playback session sample count does not match its sweep settings.", nameof(session));
        var previousFrequencyHz = -1L;
        foreach (var point in session.Points)
        {
            if (point.FrequencyHz <= previousFrequencyHz || point.FrequencyHz < session.Sweep.StartFrequencyHz ||
                point.FrequencyHz > session.Sweep.StopFrequencyHz)
                throw new ArgumentException("Playback session frequencies must be strictly increasing within the saved sweep.", nameof(session));
            previousFrequencyHz = point.FrequencyHz;
        }
        _session = session;
        _sessionProfileDescriptor = sessionDescriptor;
    }

    public long GetSampleFrequencyHz(SweepSettings settings, int index)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if ((uint)index >= (uint)settings.PointCount) throw new ArgumentOutOfRangeException(nameof(index));
        var session = _session ?? throw new InvalidOperationException("No measurement session is loaded for playback.");
        if (session.Sweep != settings)
            throw new InvalidOperationException("Playback requires the exact frequency range and point count saved in the session.");
        return session.Points[index].FrequencyHz;
    }

    public ValueTask ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (_session is null)
            throw new InvalidOperationException("Open a compatible measurement session before selecting playback.");
        if (_connected)
            throw new InvalidOperationException("Playback is already connected.");
        _connected = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _connected = false;
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(settings);
        if (!_connected)
            throw new InvalidOperationException("Connect the playback source before scanning.");
        var session = _session ?? throw new InvalidOperationException("No measurement session is loaded for playback.");
        if (session.Sweep != settings)
            throw new InvalidOperationException("Playback requires the exact frequency range and point count saved in the session.");
        if (_sessionProfileDescriptor is { } descriptor)
            settings.ValidateAgainst(descriptor);
        else if (session.ProfileSnapshot is { } legacySnapshot)
            settings.ValidateAgainst(legacySnapshot);
        else
            settings.ValidateAgainst(Profile);

        foreach (var point in session.Points)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return point;
            await Task.Yield();
        }
    }

    public IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        DeviceSweepMode mode,
        CancellationToken cancellationToken = default)
    {
        var session = _session ?? throw new InvalidOperationException("No measurement session is loaded for playback.");
        var requestedMode = session.MeasurementDataKind switch
        {
            MeasurementDataKind.ScalarTransmissionMagnitude => DeviceSweepMode.ScalarTransmission,
            MeasurementDataKind.ReflectionDerived => DeviceSweepMode.Reflection,
            _ => session.AnalysisMode == SessionAnalysisMode.Filter
                ? DeviceSweepMode.ScalarTransmission
                : DeviceSweepMode.Reflection,
        };
        if (mode != requestedMode)
            throw new InvalidOperationException("Playback cannot change the measurement mode saved in the session.");
        return SweepAsync(settings, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _connected = false;
        _session = null;
        _sessionProfileDescriptor = null;
        return ValueTask.CompletedTask;
    }
}
