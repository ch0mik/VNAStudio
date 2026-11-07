using System.Runtime.CompilerServices;
using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class DevicePluginContractTests
{
    [Fact]
    public async Task RegisteredPluginCreatesNeutralAsyncMeasurementSourceWithoutHardware()
    {
        var plugin = new ContractTestPlugin();
        var catalog = new DevicePluginCatalog([plugin]);

        Assert.True(catalog.TryGetPlugin("test.loopback", out var registeredPlugin));
        Assert.Same(plugin, registeredPlugin);

        var profile = catalog.GetProfile("test.loopback", "test.profile");
        var source = registeredPlugin!.CreateSource(profile);
        await using (source)
        {
            Assert.Same(profile, source.Profile);
            Assert.Equal("test.loopback:test.profile", source.SourceId);

            var connection = new DeviceConnectionRequest(PortName: "/dev/ttyUSB-contract-test");
            using var cancellation = new CancellationTokenSource();
            await source.ConnectAsync(connection, cancellation.Token);

            Assert.Same(connection, plugin.LastSource!.LastConnectionRequest);
            Assert.Equal(cancellation.Token, plugin.LastSource.LastConnectionToken);

            var settings = new SweepSettings(1_000_000, 2_000_000, 2);
            var samples = new List<MeasurementPoint>();
            await foreach (var sample in source.SweepAsync(
                               settings,
                               MeasurementAcquisitionMode.Reflection,
                               cancellation.Token))
                samples.Add(sample);

            Assert.Equal([1_000_000L, 2_000_000L], samples.Select(sample => sample.FrequencyHz));
            Assert.Equal(2, plugin.LastSource.SweepCount);
            Assert.Equal(cancellation.Token, plugin.LastSource.LastSweepToken);

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in source.SweepAsync(
                                   settings,
                                   MeasurementAcquisitionMode.Reflection,
                                   cancelled.Token))
                {
                }
            });
            Assert.Equal(2, plugin.LastSource.SweepCount);

            await source.DisconnectAsync(cancellation.Token);
            Assert.Equal(cancellation.Token, plugin.LastSource.LastDisconnectToken);
        }

        Assert.True(plugin.LastSource!.WasDisposed);
    }

    private sealed class ContractTestPlugin : IDevicePlugin
    {
        private static readonly DeviceProfileDescriptor TestProfile = new(
            "test.profile",
            "Loopback VNA",
            SchemaVersion: 1,
            MinimumFrequencyHz: 1_000_000,
            MaximumFrequencyHz: 2_000_000,
            DefaultPointCount: 2,
            ReferenceImpedanceOhms: 50,
            Capabilities: Array.Empty<DeviceCapabilityDescriptor>(),
            Connectors: Array.Empty<DeviceConnectorDescriptor>());

        public ContractTestPlugin() => Descriptor = new DevicePluginDescriptor(
            "test.loopback",
            "Test loopback plugin",
            DevicePluginContract.CurrentVersion,
            [TestProfile]);

        public DevicePluginDescriptor Descriptor { get; }

        public ContractTestSource? LastSource { get; private set; }

        public IMeasurementSource CreateSource(DeviceProfileDescriptor profile)
        {
            if (!string.Equals(profile.Id, TestProfile.Id, StringComparison.Ordinal))
                throw new ArgumentException("Unknown profile.", nameof(profile));

            LastSource = new ContractTestSource(profile);
            return LastSource;
        }
    }

    private sealed class ContractTestSource(DeviceProfileDescriptor profile) : IMeasurementSource
    {
        public DeviceProfileDescriptor Profile { get; } = profile;

        public string SourceId => "test.loopback:test.profile";

        public DeviceConnectionRequest? LastConnectionRequest { get; private set; }

        public CancellationToken LastConnectionToken { get; private set; }

        public CancellationToken LastDisconnectToken { get; private set; }

        public CancellationToken LastSweepToken { get; private set; }

        public int SweepCount { get; private set; }

        public bool WasDisposed { get; private set; }

        public ValueTask ConnectAsync(DeviceConnectionRequest request, CancellationToken cancellationToken = default)
        {
            LastConnectionRequest = request;
            LastConnectionToken = cancellationToken;
            return ValueTask.CompletedTask;
        }

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            LastDisconnectToken = cancellationToken;
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<MeasurementPoint> SweepAsync(
            SweepSettings settings,
            MeasurementAcquisitionMode mode,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Assert.Equal(MeasurementAcquisitionMode.Reflection, mode);
            LastSweepToken = cancellationToken;

            for (var index = 0; index < settings.PointCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SweepCount++;
                await Task.Yield();
                yield return new MeasurementPoint(settings.FrequencyAt(index), 1, 20, 0, 50, 50, 0);
            }
        }

        public ValueTask DisposeAsync()
        {
            WasDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
