using Vna.Core;

namespace Vna.Devices;

/// <summary>
/// Creates the temporary UI-facing profile bridge from a neutral plugin descriptor.
/// Protocol values in this compatibility object are placeholders; acquisition is owned by the plugin.
/// Its numeric Zref fallback exists only for old consumers that require a scalar; the neutral descriptor remains authoritative.
/// </summary>
public static class DeviceProfileCompatibilityFactory
{
    public static DeviceProfile Create(DeviceProfileDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.Presentation);

        return new DeviceProfile(
            descriptor.Id,
            descriptor.DisplayName,
            descriptor.MinimumFrequencyHz,
            descriptor.MaximumFrequencyHz,
            descriptor.DefaultPointCount,
            DdsClockHz: 0,
            ReferenceImpedanceOhms: descriptor.ReferenceImpedanceOhms ?? 50,
            MagnitudeAdcDbPerCount: 0,
            PhaseAdcDegreesPerCount: 0)
        {
            GeneratorMemoryFrequenciesHz = descriptor.FrequencyPresetsHz.ToArray(),
            LegacyBandPresets = descriptor.Presentation.FrequencyBands.ToArray(),
            Capabilities = descriptor.Capabilities.ToArray(),
            Connectors = descriptor.Connectors.ToArray(),
        };
    }
}
