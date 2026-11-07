using Microsoft.Extensions.Options;
using Avalonia.Headless.XUnit;
using System.IO.Ports;
using System.Text;
using Vna.App.Infrastructure;
using Vna.Core;
using Vna.Devices;
using Vna.Devices.Transport;
using Xunit;

namespace Vna.Tests;

public sealed class Vna65MHzMeasurementSourceTests
{
    [Fact]
    public async Task SvnaUsesSixByteSamplesAndLegacyDdsCommands()
    {
        var transport = new BufferTransport(CreatePayload(frameBytes: 6, points: 3));
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default with { LegacySvna = true }),
            transport,
            Options.Create(new Vna65MHzSerialOptions()));

        await device.ConnectAsync(new DeviceConnectionRequest("/dev/serial/by-id/vna"), TestContext.Current.CancellationToken);
        var settings = new SweepSettings(1_000_000, 4_000_000, 3);
        var points = await device.SweepAsync(settings, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 1_000_000L, 2_000_000L, 3_000_000L }, points.Select(point => point.FrequencyHz));
        Assert.Equal(points.Select(point => point.FrequencyHz),
            Enumerable.Range(0, settings.PointCount).Select(index => device.GetSampleFrequencyHz(settings, index)));
        Assert.NotEqual(settings.FrequencyAt(settings.PointCount - 1), points[^1].FrequencyHz);
        Assert.All(points, point => Assert.Equal(20.16 - 256 * Vna65MHzDeviceProfile.Default.MagnitudeAdcDbPerCount,
            point.ReturnLossDb!.Value, 6));
        Assert.Equal(new[] { "0\r", "22370411\r", "3\r", "22370411\r" },
            transport.Writes.Select(bytes => Encoding.ASCII.GetString(bytes)));
        Assert.Equal(6 * 3, transport.BytesRequested);
        Assert.Equal(new SerialConnectionSettings("/dev/serial/by-id/vna", 115_200, 8,
            Parity.None, StopBits.One, Handshake.None), transport.OpenSettings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task StandardVnaUsesFourByteSamplesForBothVnaTypes(int vnaType)
    {
        var transport = new BufferTransport(CreatePayload(frameBytes: 4, points: 2));
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default with { LegacyVnaType = vnaType }),
            transport,
            Options.Create(new Vna65MHzSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionRequest("COM12"), TestContext.Current.CancellationToken);

        var points = await device.SweepAsync(new SweepSettings(2_000_000, 4_000_000, 2), TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, points.Count);
        Assert.Equal(8, transport.BytesRequested);
    }

    [Fact]
    public async Task Captured50OhmFrameIsDecodedAndConvertedThroughTheSerialSource()
    {
        // Offline COM9 capture at 1 MHz with a 50-ohm termination: phase ADC 175, magnitude ADC 961.
        // The second point is only present to satisfy the sweep contract; assertions use the captured first point.
        var payload = new byte[] { 0xAF, 0x00, 0xC1, 0x03, 0x00, 0x00, 0x00, 0x00 };
        var transport = new BufferTransport(payload);
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default),
            transport,
            Options.Create(new Vna65MHzSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionRequest("COM9"), TestContext.Current.CancellationToken);

        var settings = new SweepSettings(1_000_000, 3_000_000, 2);
        var points = await device.SweepAsync(settings, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 1_000_000L, 2_000_000L }, points.Select(point => point.FrequencyHz));
        Assert.Equal(-36.203635986, points[0].ReturnLossDb!.Value, 8);
        Assert.InRange(points[0].Swr!.Value, 1.031, 1.032);
        Assert.InRange(points[0].ResistanceOhms!.Value, 51, 52);
        Assert.InRange(Math.Abs(points[0].ReactanceOhms!.Value), 0, 1);
        Assert.Equal(PhaseSignConfidence.Ambiguous, points[0].PhaseSignConfidence);
        Assert.Equal(new[] { "0\r", "22370411\r", "2\r", "22370411\r" },
            transport.Writes.Select(bytes => Encoding.ASCII.GetString(bytes)));
        Assert.Equal(payload.Length, transport.BytesRequested);
    }

    [Fact]
    public async Task ScalarTransmissionIsRejectedWithoutSendingUnverifiedCommand()
    {
        var transport = new BufferTransport(CreatePayload(frameBytes: 4, points: 2));
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default with { LegacyVnaType = 1 }),
            transport,
            Options.Create(new Vna65MHzSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionRequest("COM9"), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await device.SweepAsync(new SweepSettings(1_000_000, 3_000_000, 2),
                    MeasurementAcquisitionMode.ScalarTransmission, TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));

        Assert.Empty(transport.Writes);
        Assert.Equal(0, transport.BytesRequested);
    }

    [Fact]
    public async Task RepeatedConnectFailureKeepsExistingTransportOpen()
    {
        var transport = new BufferTransport(Array.Empty<byte>());
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default with { LegacyVnaType = 1 }),
            transport,
            Options.Create(new Vna65MHzSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionRequest("COM3"), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await device.ConnectAsync(new DeviceConnectionRequest("COM4"), TestContext.Current.CancellationToken));

        Assert.True(transport.IsOpen);
        Assert.Equal(0, transport.CloseCount);
        Assert.Equal("COM3", transport.OpenSettings?.PortName);
    }

    [AvaloniaFact]
    public async Task ViewModelAcceptsHardwareDdsGridWhoseLastPointIsBelowStop()
    {
        var profile = Vna65MHzDeviceProfile.Default with { LegacyVnaType = 1 };
        var transport = new BufferTransport(CreatePayload(frameBytes: 4, points: 3));
        var router = new VnaDeviceRouter(
            new DemoVnaDevice(profile),
            new MeasurementSourceDeviceAdapter(profile,
                new Vna65MHzMeasurementSource(Vna65MHzProtocolSettings.FromLegacyProfile(profile), transport, Options.Create(new Vna65MHzSerialOptions())),
                DevicePluginIds.Vna65MHz),
            DeviceSourceMode.Hardware);
        await using var viewModel = TestData.ViewModel(new Vna.IO.AppSettings
        {
            PortName = "COM17",
            MeasurementSource = "hardware",
        }, router);
        viewModel.StartMHz = "1";
        viewModel.StopMHz = "4";
        viewModel.PointCount = "3";

        await ((AsyncRelayCommand)viewModel.StartCommand).ExecuteAsync();

        Assert.False(viewModel.HasError, viewModel.Error);
        Assert.Equal(new[] { 1_000_000L, 2_000_000L, 3_000_000L }, viewModel.Points.Select(point => point.FrequencyHz));
        Assert.True(viewModel.CanSaveSession);
    }

    [Fact]
    public async Task CancellationDuringReadClosesConnectionSoPartialFrameCannotLeakIntoNextSweep()
    {
        var transport = new BufferTransport(Array.Empty<byte>(), blockRead: true);
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default with { LegacyVnaType = 1 }),
            transport,
            Options.Create(new Vna65MHzSerialOptions { ReadTimeout = TimeSpan.FromMinutes(1) }));
        await device.ConnectAsync(new DeviceConnectionRequest("COM3"), TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await device.SweepAsync(new SweepSettings(1_000_000, 2_000_000, 2), cancellation.Token)
                .ToListAsync(cancellation.Token));

        Assert.True(transport.CloseCount >= 1);
        Assert.False(transport.IsOpen);
    }

    [Fact]
    public async Task CancellationAfterFirstYieldAlsoClosesConnection()
    {
        var transport = new BufferTransport(CreatePayload(frameBytes: 4, points: 3));
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default with { LegacyVnaType = 1 }),
            transport,
            Options.Create(new Vna65MHzSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionRequest("COM3"), TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        await using var enumerator = device.SweepAsync(
            new SweepSettings(1_000_000, 4_000_000, 3), cancellation.Token).GetAsyncEnumerator(cancellation.Token);

        Assert.True(await enumerator.MoveNextAsync());
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());

        Assert.False(transport.IsOpen);
        Assert.True(transport.CloseCount >= 1);
    }

    [Fact]
    public async Task TimeoutClosesPartialSweepAndAllowsASeparateReconnect()
    {
        var transport = new BufferTransport(
            CreatePayload(frameBytes: 4, points: 2),
            readFailure: new TimeoutException("Simulated serial timeout."),
            partialReadBytes: 2);
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default with { LegacyVnaType = 1 }),
            transport,
            Options.Create(new Vna65MHzSerialOptions()));
        var settings = new SweepSettings(1_000_000, 3_000_000, 2);
        await device.ConnectAsync(new DeviceConnectionRequest("COM3"), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<TimeoutException>(async () =>
            await device.SweepAsync(settings, TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));

        Assert.False(transport.IsOpen);
        Assert.Equal(2, transport.BytesCopiedBeforeInjectedFailure);
        Assert.Equal(4, transport.Writes.Count);
        await device.ConnectAsync(new DeviceConnectionRequest("COM3"), TestContext.Current.CancellationToken);
        var points = await device.SweepAsync(settings, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, points.Count);
        Assert.Equal(8, transport.Writes.Count);
        Assert.True(transport.IsOpen);
    }

    [Fact]
    public async Task TruncatedFrameClosesConnectionInsteadOfYieldingPartialSamples()
    {
        var transport = new BufferTransport([0xAF, 0x00, 0xC1, 0x03, 0x10, 0x00]);
        await using var device = new Vna65MHzMeasurementSource(
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default with { LegacyVnaType = 1 }),
            transport,
            Options.Create(new Vna65MHzSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionRequest("COM3"), TestContext.Current.CancellationToken);
        await using var enumerator = device.SweepAsync(
            new SweepSettings(1_000_000, 3_000_000, 2), TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<EndOfStreamException>(async () => await enumerator.MoveNextAsync());

        Assert.False(transport.IsOpen);
        Assert.Equal(6, transport.BytesCopiedBeforeEndOfStream);
        Assert.Equal(8, transport.BytesRequested); // MoveNext fails before yielding the first sample.
    }

    private static byte[] CreatePayload(int frameBytes, int points)
    {
        var payload = new byte[frameBytes * points];
        for (var index = 0; index < points; index++)
        {
            var offset = index * frameBytes;
            payload[offset + 1] = 2; // phase ADC = 512
            payload[offset + 3] = 1; // magnitude ADC = 256
        }
        return payload;
    }

    private sealed class BufferTransport : ISerialTransport
    {
        private readonly byte[] _payload;
        private readonly bool _blockRead;
        private readonly int _partialReadBytes;
        private Exception? _readFailure;
        private int _offset;

        public BufferTransport(
            byte[] payload,
            bool blockRead = false,
            Exception? readFailure = null,
            int partialReadBytes = 0)
        {
            _payload = payload;
            _blockRead = blockRead;
            _readFailure = readFailure;
            _partialReadBytes = partialReadBytes;
        }

        public bool IsOpen { get; private set; }
        public int BytesRequested { get; private set; }
        public int BytesCopiedBeforeInjectedFailure { get; private set; }
        public int BytesCopiedBeforeEndOfStream { get; private set; }
        public int CloseCount { get; private set; }
        public SerialConnectionSettings? OpenSettings { get; private set; }
        public List<byte[]> Writes { get; } = [];

        public ValueTask OpenAsync(SerialConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenSettings = settings;
            IsOpen = true;
            _offset = 0;
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Writes.Add(bytes.ToArray());
            return ValueTask.CompletedTask;
        }

        public async ValueTask ReadExactlyAsync(Memory<byte> destination, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            BytesRequested += destination.Length;
            if (_blockRead)
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            if (_readFailure is { } readFailure)
            {
                _readFailure = null;
                var copied = Math.Min(Math.Min(_partialReadBytes, destination.Length), _payload.Length - _offset);
                if (copied > 0)
                {
                    _payload.AsMemory(_offset, copied).CopyTo(destination);
                    _offset += copied;
                    BytesCopiedBeforeInjectedFailure += copied;
                }
                throw readFailure;
            }
            if (_payload.Length - _offset < destination.Length)
            {
                var copied = Math.Max(0, _payload.Length - _offset);
                if (copied > 0)
                {
                    _payload.AsMemory(_offset, copied).CopyTo(destination);
                    _offset += copied;
                    BytesCopiedBeforeEndOfStream += copied;
                }
                throw new EndOfStreamException();
            }
            _payload.AsMemory(_offset, destination.Length).CopyTo(destination);
            _offset += destination.Length;
        }

        public ValueTask CloseAsync()
        {
            CloseCount++;
            IsOpen = false;
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync() => CloseAsync();
    }
}
