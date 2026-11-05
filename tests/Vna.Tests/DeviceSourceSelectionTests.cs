using System.Runtime.CompilerServices;
using Avalonia.Headless.XUnit;
using Vna.App.Infrastructure;
using Vna.Core;
using Vna.Devices;
using Vna.IO;
using Xunit;

namespace Vna.Tests;

public sealed class DeviceSourceSelectionTests
{
    [Fact]
    public async Task HardwareConnectionFailureDoesNotFallBackToDemo()
    {
        var demo = new FakeDevice("demo");
        var hardware = new FakeDevice("hardware", failConnect: true);
        await using var router = new VnaDeviceRouter(demo, hardware, DeviceSourceMode.Hardware);

        await Assert.ThrowsAsync<IOException>(async () =>
            await router.ConnectAsync(new DeviceConnectionOptions("COM17"), TestContext.Current.CancellationToken));

        Assert.Equal(DeviceSourceMode.Hardware, router.SourceMode);
        Assert.Equal("hardware", router.DataSourceName);
        Assert.Equal(0, demo.ConnectCount);
        Assert.Throws<InvalidOperationException>(() => router.SweepAsync(
            new SweepSettings(1_000_000, 2_000_000, 2), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HardwareSweepFailureDoesNotInvokeDemoSource()
    {
        var demo = new FakeDevice("demo");
        var hardware = new FakeDevice("hardware", failSweep: true);
        await using var router = new VnaDeviceRouter(demo, hardware, DeviceSourceMode.Hardware);
        await router.ConnectAsync(new DeviceConnectionOptions("COM17"), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<IOException>(async () =>
        {
            await foreach (var _ in router.SweepAsync(
                               new SweepSettings(1_000_000, 2_000_000, 2), TestContext.Current.CancellationToken))
            {
            }
        });

        Assert.Equal(1, hardware.SweepCount);
        Assert.Equal(0, demo.SweepCount);
        Assert.Equal(DeviceSourceMode.Hardware, router.SourceMode);
        Assert.Equal("hardware", router.DataSourceName);
    }

    [AvaloniaFact]
    public async Task ManualHardwareConnectionSurvivesCompleteScanUntilDisconnected()
    {
        var demo = new FakeDevice("demo");
        var hardware = new FakeDevice("hardware");
        var router = new VnaDeviceRouter(demo, hardware);
        await using var vm = TestData.ViewModel(new Vna.IO.AppSettings
        {
            PortName = "COM17",
            MeasurementSource = "hardware",
        }, router);
        vm.StartMHz = "1";
        vm.StopMHz = "2";
        vm.PointCount = "3";

        await vm.ConnectDeviceCommand.ExecuteAsync();
        Assert.True(vm.IsDeviceConnected, $"{vm.Status}; {vm.Error}");
        Assert.Equal("COM17", hardware.LastPortName);
        Assert.False(vm.CanChangePort);
        Assert.False(vm.CanSelectMeasurementSource);

        await ((AsyncRelayCommand)vm.StartCommand).ExecuteAsync();

        Assert.False(vm.IsScanning);
        Assert.True(vm.IsDeviceConnected, $"{vm.Status}; {vm.Error}");
        Assert.Equal(1, hardware.ConnectCount);
        Assert.Equal(0, hardware.DisconnectCount);
        Assert.Equal(3, vm.Points.Count);
        Assert.Equal(vm.Localizer["HardwareDevice"], vm.DataSourceDisplay);

        await vm.DisconnectDeviceCommand.ExecuteAsync();
        Assert.False(vm.IsDeviceConnected);
        Assert.Equal(1, hardware.DisconnectCount);
        Assert.True(vm.CanSelectMeasurementSource);
    }

    [AvaloniaFact]
    public async Task ViewModelPersistsSelectedSourceWithoutChangingPortName()
    {
        var router = new VnaDeviceRouter(new FakeDevice("demo"), new FakeDevice("hardware"), DeviceSourceMode.Demo);
        await using var vm = TestData.ViewModel(new Vna.IO.AppSettings
        {
            PortName = "/dev/serial/by-id/my-vna",
            MeasurementSource = "hardware",
        }, router);

        Assert.True(vm.UseHardwareSource);
        Assert.Equal("hardware", router.DataSourceName);
        var settings = vm.CreateAppSettings();
        Assert.Equal("hardware", settings.MeasurementSource);
        Assert.Equal("/dev/serial/by-id/my-vna", settings.PortName);

        vm.UseHardwareSource = false;
        Assert.Equal(DeviceSourceMode.Demo, router.SourceMode);
        Assert.Equal("demo", vm.CreateAppSettings().MeasurementSource);
    }

    [AvaloniaFact]
    public async Task PlaybackReplaysOnlyTheLoadedSweepAndPersistsItsSource()
    {
        var playback = new SessionPlaybackDevice(DeviceProfile.Vna65MHz);
        var router = new VnaDeviceRouter(new FakeDevice("demo"), new FakeDevice("hardware"), playback: playback);
        await using var vm = TestData.ViewModel(new Vna.IO.AppSettings(), router);
        var session = TestData.Session();
        vm.LoadSession(session);

        vm.SelectedMeasurementSource = vm.MeasurementSources.Single(source => source.Mode == DeviceSourceMode.Playback);
        Assert.True(vm.CanConnectDevice);
        Assert.Equal("playback", vm.CreateAppSettings().MeasurementSource);
        await vm.ConnectDeviceCommand.ExecuteAsync();
        await ((AsyncRelayCommand)vm.StartCommand).ExecuteAsync();

        Assert.Equal(session.Points, vm.Points);
        Assert.Equal("playback", router.DataSourceName);
        Assert.Equal(vm.Localizer["PlaybackDevice"], vm.DataSourceDisplay);
        await vm.DisconnectDeviceCommand.ExecuteAsync();
    }

    [Fact]
    public async Task PlaybackRejectsDifferentRangeWithoutInterpolating()
    {
        var playback = new SessionPlaybackDevice(DeviceProfile.Vna65MHz);
        playback.SetSession(TestData.Session());
        await playback.ConnectAsync(new DeviceConnectionOptions(string.Empty), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in playback.SweepAsync(
                               new SweepSettings(1_000_000, 4_000_000, 5), TestContext.Current.CancellationToken))
            {
            }
        });
        await playback.DisposeAsync();
    }

    [Fact]
    public async Task FilterPlaybackRequiresSavedModeAndReplaysScalarTransmissionSamples()
    {
        var sourceSession = TestData.Session() with
        {
            AnalysisMode = SessionAnalysisMode.Filter,
            Points = TestData.Session().Points.Select((point, index) => point with
            {
                Swr = null,
                ReturnLossDb = null,
                PhaseDegrees = null,
                PhaseSignConfidence = PhaseSignConfidence.Missing,
                PhaseSignProvenance = PhaseSignProvenance.Unknown,
                ImpedanceOhms = null,
                ResistanceOhms = null,
                ReactanceOhms = null,
                TransmissionDb = -2.0 - index,
            }).ToArray(),
        };
        var playback = new SessionPlaybackDevice(DeviceProfile.Vna65MHz);
        playback.SetSession(sourceSession);
        await playback.ConnectAsync(new DeviceConnectionOptions(string.Empty), TestContext.Current.CancellationToken);

        var samples = await playback.SweepAsync(sourceSession.Sweep, DeviceSweepMode.ScalarTransmission,
                TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(sourceSession.Points.Select(point => point.TransmissionDb), samples.Select(point => point.TransmissionDb));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await playback.SweepAsync(sourceSession.Sweep, DeviceSweepMode.Reflection,
                    TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));
        await playback.DisposeAsync();
    }

    [Fact]
    public async Task DemoFilterSweepProducesOnlyIllustrativeScalarTransmission()
    {
        await using var demo = new DemoVnaDevice(DeviceProfile.Vna65MHz);
        await demo.ConnectAsync(new DeviceConnectionOptions(string.Empty), TestContext.Current.CancellationToken);
        var points = await demo.SweepAsync(new SweepSettings(13_000_000, 15_000_000, 3),
                DeviceSweepMode.ScalarTransmission, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.All(points, point =>
        {
            Assert.InRange(point.TransmissionDb!.Value, -30, 0);
            Assert.Null(point.ReturnLossDb);
            Assert.Null(point.Swr);
            Assert.Null(point.PhaseDegrees);
            Assert.Null(point.ImpedanceOhms);
        });
    }

    [AvaloniaFact]
    public async Task PlaybackPreservesSavedHardwareDdsFrequencyGrid()
    {
        var sweep = new SweepSettings(1_000_000, 4_000_000, 3);
        var points = new[]
        {
            TestData.Point(1_000_000),
            TestData.Point(2_000_000),
            TestData.Point(3_000_000),
        };
        var session = new MeasurementSession(2, DeviceProfile.Vna65MHz.Id, "hardware", DateTimeOffset.UnixEpoch, sweep, points)
        {
            ProfileSnapshot = DeviceProfile.Vna65MHz,
        };
        using var serialized = new MemoryStream();
        await MeasurementSessionFile.SaveAsync(serialized, session, TestContext.Current.CancellationToken);
        serialized.Position = 0;
        var loaded = await MeasurementSessionFile.LoadAsync(serialized, TestContext.Current.CancellationToken);

        var router = new VnaDeviceRouter(new FakeDevice("demo"), new FakeDevice("hardware"),
            playback: new SessionPlaybackDevice(DeviceProfile.Vna65MHz));
        await using var vm = TestData.ViewModel(new Vna.IO.AppSettings(), router);
        vm.LoadSession(loaded);
        vm.SelectedMeasurementSource = vm.MeasurementSources.Single(source => source.Mode == DeviceSourceMode.Playback);
        await vm.ConnectDeviceCommand.ExecuteAsync();
        await ((AsyncRelayCommand)vm.StartCommand).ExecuteAsync();

        Assert.Equal(points, vm.Points);
        Assert.False(vm.HasError, vm.Error);
        Assert.Equal(3_000_000, vm.Points[^1].FrequencyHz);
        await vm.DisconnectDeviceCommand.ExecuteAsync();
    }

    private sealed class FakeDevice(string sourceName, bool failConnect = false, bool failSweep = false) : IVnaDevice
    {
        public DeviceProfile Profile => DeviceProfile.Vna65MHz;
        public string DataSourceName => sourceName;
        public int ConnectCount { get; private set; }
        public int DisconnectCount { get; private set; }
        public string? LastPortName { get; private set; }
        public int SweepCount { get; private set; }

        public ValueTask ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken = default)
        {
            ConnectCount++;
            LastPortName = options.PortName;
            cancellationToken.ThrowIfCancellationRequested();
            return failConnect ? ValueTask.FromException(new IOException("Fake connection failure.")) : ValueTask.CompletedTask;
        }

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            DisconnectCount++;
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<MeasurementPoint> SweepAsync(SweepSettings settings,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            SweepCount++;
            cancellationToken.ThrowIfCancellationRequested();
            if (failSweep) throw new IOException("Fake sweep failure.");
            for (var index = 0; index < settings.PointCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return TestData.Point(settings.FrequencyAt(index));
            }
            await Task.Yield();
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
