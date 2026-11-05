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

public sealed class MiniVnaSerialDeviceTests
{
    [Fact]
    public async Task SvnaUsesSixByteSamplesAndLegacyDdsCommands()
    {
        var transport = new BufferTransport(CreatePayload(frameBytes: 6, points: 3));
        await using var device = new MiniVnaSerialDevice(
            DeviceProfile.Vna65MHz with { LegacySvna = true },
            transport,
            Options.Create(new MiniVnaSerialOptions()));

        await device.ConnectAsync(new DeviceConnectionOptions("/dev/serial/by-id/vna"), TestContext.Current.CancellationToken);
        var settings = new SweepSettings(1_000_000, 4_000_000, 3);
        var points = await device.SweepAsync(settings, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 1_000_000L, 2_000_000L, 3_000_000L }, points.Select(point => point.FrequencyHz));
        Assert.Equal(points.Select(point => point.FrequencyHz),
            Enumerable.Range(0, settings.PointCount).Select(index => device.GetSampleFrequencyHz(settings, index)));
        Assert.NotEqual(settings.FrequencyAt(settings.PointCount - 1), points[^1].FrequencyHz);
        Assert.All(points, point => Assert.Equal(20.16 - 256 * DeviceProfile.Vna65MHz.MagnitudeAdcDbPerCount,
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
        await using var device = new MiniVnaSerialDevice(
            DeviceProfile.Vna65MHz with { LegacyVnaType = vnaType },
            transport,
            Options.Create(new MiniVnaSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionOptions("COM12"), TestContext.Current.CancellationToken);

        var points = await device.SweepAsync(new SweepSettings(2_000_000, 4_000_000, 2), TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, points.Count);
        Assert.Equal(8, transport.BytesRequested);
    }

    [Fact]
    public async Task ScalarTransmissionIsRejectedWithoutSendingUnverifiedCommand()
    {
        var transport = new BufferTransport(CreatePayload(frameBytes: 4, points: 2));
        await using var device = new MiniVnaSerialDevice(
            DeviceProfile.Vna65MHz with { LegacyVnaType = 1 },
            transport,
            Options.Create(new MiniVnaSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionOptions("COM9"), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await device.SweepAsync(new SweepSettings(1_000_000, 3_000_000, 2),
                    DeviceSweepMode.ScalarTransmission, TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));

        Assert.Empty(transport.Writes);
        Assert.Equal(0, transport.BytesRequested);
    }

    [Fact]
    public async Task RepeatedConnectFailureKeepsExistingTransportOpen()
    {
        var transport = new BufferTransport(Array.Empty<byte>());
        await using var device = new MiniVnaSerialDevice(
            DeviceProfile.Vna65MHz with { LegacyVnaType = 1 },
            transport,
            Options.Create(new MiniVnaSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionOptions("COM3"), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await device.ConnectAsync(new DeviceConnectionOptions("COM4"), TestContext.Current.CancellationToken));

        Assert.True(transport.IsOpen);
        Assert.Equal(0, transport.CloseCount);
        Assert.Equal("COM3", transport.OpenSettings?.PortName);
    }

    [AvaloniaFact]
    public async Task ViewModelAcceptsHardwareDdsGridWhoseLastPointIsBelowStop()
    {
        var profile = DeviceProfile.Vna65MHz with { LegacyVnaType = 1 };
        var transport = new BufferTransport(CreatePayload(frameBytes: 4, points: 3));
        var router = new VnaDeviceRouter(
            new DemoVnaDevice(profile),
            new MiniVnaSerialDevice(profile, transport, Options.Create(new MiniVnaSerialOptions())),
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
        await using var device = new MiniVnaSerialDevice(
            DeviceProfile.Vna65MHz with { LegacyVnaType = 1 },
            transport,
            Options.Create(new MiniVnaSerialOptions { ReadTimeout = TimeSpan.FromMinutes(1) }));
        await device.ConnectAsync(new DeviceConnectionOptions("COM3"), TestContext.Current.CancellationToken);
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
        await using var device = new MiniVnaSerialDevice(
            DeviceProfile.Vna65MHz with { LegacyVnaType = 1 },
            transport,
            Options.Create(new MiniVnaSerialOptions()));
        await device.ConnectAsync(new DeviceConnectionOptions("COM3"), TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        await using var enumerator = device.SweepAsync(
            new SweepSettings(1_000_000, 4_000_000, 3), cancellation.Token).GetAsyncEnumerator(cancellation.Token);

        Assert.True(await enumerator.MoveNextAsync());
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());

        Assert.False(transport.IsOpen);
        Assert.True(transport.CloseCount >= 1);
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

    private sealed class BufferTransport(byte[] payload, bool blockRead = false) : ISerialTransport
    {
        private int _offset;
        public bool IsOpen { get; private set; }
        public int BytesRequested { get; private set; }
        public int CloseCount { get; private set; }
        public SerialConnectionSettings? OpenSettings { get; private set; }
        public List<byte[]> Writes { get; } = [];

        public ValueTask OpenAsync(SerialConnectionSettings settings, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenSettings = settings;
            IsOpen = true;
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
            if (blockRead)
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            if (payload.Length - _offset < destination.Length)
                throw new EndOfStreamException();
            payload.AsMemory(_offset, destination.Length).CopyTo(destination);
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
