using System.Globalization;
using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Options;
using Vna.Core;
using Vna.Devices.Transport;
using Vna.Localization;

namespace Vna.Devices;

/// <summary>
/// Serial acquisition for the 306b/miniVNA text-command protocol. The port path is passed through
/// unchanged. Standard VNA frames contain four bytes per point. The separate legacy SVNA flag
/// selects six, of which the reflection calculation consumes the first two 10-bit channels.
/// </summary>
public sealed class MiniVnaSerialDevice : IVnaDevice
{
    private const double DdsWordScale = 4_294_967_296d;
    private static readonly Encoding Ascii = Encoding.ASCII;
    private readonly DeviceProfile _profile;
    private readonly ISerialTransport _transport;
    private readonly MiniVnaSerialOptions _options;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private bool _connected;
    private bool _disposed;

    public MiniVnaSerialDevice(
        DeviceProfile profile,
        ISerialTransport transport,
        IOptions<MiniVnaSerialOptions> options)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (_options.BaudRate <= 0 || _options.DataBits is < 5 or > 8 ||
            _options.ReadTimeout <= TimeSpan.Zero || _options.ReadTimeout > TimeSpan.FromDays(24))
            throw new ArgumentOutOfRangeException(nameof(options), "Serial protocol settings must be positive and supported.");
        if (_profile.LegacyVnaType is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(profile), "The configured legacy VNA type is unsupported.");
    }

    public DeviceProfile Profile => _profile;
    public string DataSourceName => "hardware";

    public long GetSampleFrequencyHz(SweepSettings settings, int index)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if ((uint)index >= (uint)settings.PointCount) throw new ArgumentOutOfRangeException(nameof(index));
        var (startWord, stepWord) = CreateDdsWords(settings, _profile.DdsClockHz);
        return GetSampleFrequencyHz(startWord, stepWord, index);
    }

    public async ValueTask ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connected)
                throw LocalizedErrors.InvalidOperation("DeviceAlreadyConnected");

            try
            {
                await _transport.OpenAsync(new SerialConnectionSettings(
                    options.PortName,
                    _options.BaudRate,
                    _options.DataBits,
                    Parity.None,
                    StopBits.One,
                    Handshake.None), cancellationToken).ConfigureAwait(false);
                _connected = true;
            }
            catch
            {
                await CloseTransportAsync().ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await CloseTransportAsync().ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        CancellationToken cancellationToken = default) =>
        SweepAsync(settings, DeviceSweepMode.Reflection, cancellationToken);

    public async IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        DeviceSweepMode mode,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (mode == DeviceSweepMode.ScalarTransmission)
            throw LocalizedErrors.InvalidOperation("SerialFilterModeUnverified");
        ObjectDisposedException.ThrowIf(_disposed, this);
        settings.ValidateAgainst(_profile);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var completed = false;
        try
        {
            if (!_connected)
                throw LocalizedErrors.InvalidOperation("DeviceConnectFirst");
            if (_profile.StepPauseMilliseconds > 0)
                throw LocalizedErrors.InvalidOperation("SerialStepPauseUnsupported");

            var frameBytesPerPoint = _profile.LegacySvna ? 6 : 4;
            var frame = new byte[checked(settings.PointCount * frameBytesPerPoint)];
            var (startWord, stepWord) = CreateDdsWords(settings, _profile.DdsClockHz);

            await WriteCommandAsync("0", cancellationToken).ConfigureAwait(false);
            await WriteCommandAsync(startWord.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
            await WriteCommandAsync(settings.PointCount.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
            await WriteCommandAsync(stepWord.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);

            if (_profile.StartPauseMilliseconds > 0)
                await Task.Delay(_profile.StartPauseMilliseconds, cancellationToken).ConfigureAwait(false);
            await _transport.ReadExactlyAsync(frame, _options.ReadTimeout, cancellationToken).ConfigureAwait(false);

            for (var index = 0; index < settings.PointCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var offset = index * frameBytesPerPoint;
                var phaseAdc = Decode10Bit(frame[offset], frame[offset + 1]);
                var magnitudeAdc = Decode10Bit(frame[offset + 2], frame[offset + 3]);
                var frequencyHz = GetSampleFrequencyHz(startWord, stepWord, index);
                yield return RfSampleCalculator.FromLegacyAdcReadings(frequencyHz, phaseAdc, magnitudeAdc, _profile) with
                {
                    PhaseSignConfidence = PhaseSignConfidence.Ambiguous,
                    PhaseSignProvenance = PhaseSignProvenance.LegacyVna65Mhz,
                };
            }
            completed = true;
        }
        finally
        {
            try
            {
                if (!completed) await CloseTransportAsync().ConfigureAwait(false);
            }
            finally
            {
                _operationGate.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            await DisconnectAsync().ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Dispose();
            await _transport.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static (uint StartWord, uint StepWord) CreateDdsWords(SweepSettings settings, long ddsClockHz)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (ddsClockHz <= 0)
            throw new ArgumentOutOfRangeException(nameof(ddsClockHz));

        var start = Math.Round(settings.StartFrequencyHz * DdsWordScale / ddsClockHz, MidpointRounding.AwayFromZero);
        var step = Math.Round((settings.StopFrequencyHz - settings.StartFrequencyHz) * DdsWordScale /
                              (ddsClockHz * (double)settings.PointCount), MidpointRounding.AwayFromZero);
        if (start is < 0 or > uint.MaxValue || step is <= 0 or > uint.MaxValue)
            throw LocalizedErrors.InvalidOperation("DdsSweepWordsOutOfRange");
        if (start + step * (settings.PointCount - 1d) > uint.MaxValue)
            throw LocalizedErrors.InvalidOperation("DdsSweepWordsOutOfRange");

        return ((uint)start, (uint)step);
    }

    private async ValueTask WriteCommandAsync(string command, CancellationToken cancellationToken)
    {
        var bytes = Ascii.GetBytes(command + "\r");
        await _transport.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    private static int Decode10Bit(byte lowByte, byte highByte)
    {
        if (highByte > 3)
            throw new InvalidDataException("A VNA ADC high byte is outside the 10-bit sample encoding.");
        return lowByte + (highByte << 8);
    }

    private long GetSampleFrequencyHz(uint startWord, uint stepWord, int index)
    {
        var tuningWord = (ulong)startWord + (ulong)stepWord * (uint)index;
        return (long)Math.Round((double)tuningWord * _profile.DdsClockHz / DdsWordScale,
            MidpointRounding.AwayFromZero);
    }

    private async ValueTask CloseTransportAsync()
    {
        _connected = false;
        await _transport.CloseAsync().ConfigureAwait(false);
    }
}
