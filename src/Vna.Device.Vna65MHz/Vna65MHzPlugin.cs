using Microsoft.Extensions.Options;
using Vna.Core;
using Vna.Devices.Transport;

namespace Vna.Device.Vna65MHz;

/// <summary>Explicitly registered plugin for the VNA 1–65 MHz device family.</summary>
public sealed class Vna65MHzPlugin : IDevicePlugin
{
    private readonly Vna65MHzProtocolSettings _profile;
    private readonly Func<ISerialTransport> _transportFactory;
    private readonly IOptions<Vna65MHzSerialOptions> _serialOptions;

    public Vna65MHzPlugin(
        Vna65MHzProtocolSettings profile,
        Func<ISerialTransport> transportFactory,
        IOptions<Vna65MHzSerialOptions> serialOptions)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        ArgumentNullException.ThrowIfNull(_profile.Descriptor);
        _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
        _serialOptions = serialOptions ?? throw new ArgumentNullException(nameof(serialOptions));
        Descriptor = new DevicePluginDescriptor(
            DevicePluginIds.Vna65MHz,
            profile.Descriptor.DisplayName,
            DevicePluginContract.CurrentVersion,
            Array.AsReadOnly(new[] { profile.Descriptor }));
    }

    public DevicePluginDescriptor Descriptor { get; }

    public IMeasurementSource CreateSource(DeviceProfileDescriptor profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var declared = Descriptor.Profiles[0];
        if (!string.Equals(profile.Id, declared.Id, StringComparison.Ordinal) ||
            !string.Equals(profile.DisplayName, declared.DisplayName, StringComparison.Ordinal) ||
            profile.SchemaVersion != declared.SchemaVersion ||
            profile.MinimumFrequencyHz != declared.MinimumFrequencyHz ||
            profile.MaximumFrequencyHz != declared.MaximumFrequencyHz ||
            profile.DefaultPointCount != declared.DefaultPointCount ||
            profile.ReferenceImpedanceOhms != declared.ReferenceImpedanceOhms ||
            profile.Capabilities is null || !profile.Capabilities.SequenceEqual(declared.Capabilities) ||
            profile.Connectors is null || !profile.Connectors.SequenceEqual(declared.Connectors) ||
            profile.FrequencyPresetsHz is null || !profile.FrequencyPresetsHz.SequenceEqual(declared.FrequencyPresetsHz) ||
            !PresentationEquals(profile.Presentation, declared.Presentation))
            throw new ArgumentException("The selected profile is not declared by this plugin.", nameof(profile));

        return new Vna65MHzMeasurementSource(
            _profile,
            _transportFactory(),
            _serialOptions);
    }

    private static bool PresentationEquals(
        DeviceProfilePresentationDescriptor? first,
        DeviceProfilePresentationDescriptor? second) =>
        first is not null && second is not null &&
        first.DefaultCableVelocityFactor == second.DefaultCableVelocityFactor &&
        first.FrequencyBands is not null && second.FrequencyBands is not null &&
        first.FrequencyBands.SequenceEqual(second.FrequencyBands) &&
        first.TraceColors is not null && second.TraceColors is not null &&
        first.TraceColors.Count == second.TraceColors.Count &&
        first.TraceColors.All(pair => second.TraceColors.TryGetValue(pair.Key, out var color) &&
                                      string.Equals(pair.Value, color, StringComparison.OrdinalIgnoreCase));
}
