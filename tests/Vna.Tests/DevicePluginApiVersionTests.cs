using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class DevicePluginApiVersionTests
{
    [Theory]
    [InlineData(1, 0, 1, 0, true)]
    [InlineData(1, 2, 1, 0, true)]
    [InlineData(1, 2, 1, 2, true)]
    [InlineData(1, 2, 1, 3, false)]
    [InlineData(1, 2, 2, 0, false)]
    [InlineData(1, 2, 0, 2, false)]
    [InlineData(1, 2, 1, -1, false)]
    [InlineData(1, -1, 1, 0, false)]
    public void CompatibilityRequiresMatchingMajorAndSupportedNonnegativeMinor(
        int hostMajor,
        int hostMinor,
        int pluginMajor,
        int pluginMinor,
        bool expected)
    {
        Assert.Equal(
            expected,
            DevicePluginContract.IsCompatible(
                new DevicePluginApiVersion(hostMajor, hostMinor),
                new DevicePluginApiVersion(pluginMajor, pluginMinor)));
    }

    [Fact]
    public void CatalogAcceptsSameMajorPluginWithOlderMinor()
    {
        var plugin = new VersionedPlugin(new DevicePluginApiVersion(1, 0));

        var catalog = new DevicePluginCatalog(
            [plugin],
            hostApiVersion: new DevicePluginApiVersion(1, 2));

        Assert.True(catalog.TryGetPlugin(plugin.Descriptor.Id, out var registered));
        Assert.Same(plugin, registered);
    }

    [Fact]
    public void CatalogRejectsNewerPluginMinorWithDiagnostic()
    {
        var plugin = new VersionedPlugin(new DevicePluginApiVersion(1, 1));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new DevicePluginCatalog([plugin], hostApiVersion: new DevicePluginApiVersion(1, 0)));

        Assert.Contains("test.versioned", exception.Message, StringComparison.Ordinal);
        Assert.Contains("1.1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("host supports 1.0", exception.Message, StringComparison.Ordinal);
    }

    private sealed class VersionedPlugin(DevicePluginApiVersion apiVersion) : IDevicePlugin
    {
        public DevicePluginDescriptor Descriptor { get; } = new(
            "test.versioned",
            "Versioned test plugin",
            apiVersion,
            [new DeviceProfileDescriptor(
                "test.versioned.profile",
                "Versioned test profile",
                SchemaVersion: 1,
                MinimumFrequencyHz: 1,
                MaximumFrequencyHz: 2,
                DefaultPointCount: 2,
                ReferenceImpedanceOhms: null,
                Capabilities: Array.Empty<DeviceCapabilityDescriptor>(),
                Connectors: Array.Empty<DeviceConnectorDescriptor>())]);

        public IMeasurementSource CreateSource(DeviceProfileDescriptor profile) =>
            throw new NotSupportedException("Version tests do not create measurement sources.");
    }
}
