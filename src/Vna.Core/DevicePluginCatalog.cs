namespace Vna.Core;

/// <summary>
/// Immutable snapshot of the device plugins explicitly supplied by the host composition root.
/// Invalid contract versions or ambiguous IDs fail fast during application startup.
/// </summary>
public sealed class DevicePluginCatalog
{
    private readonly IReadOnlyDictionary<string, RegisteredPlugin> _plugins;

    public DevicePluginCatalog(
        IEnumerable<IDevicePlugin> plugins,
        DevicePluginApiVersion? hostApiVersion = null)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        var hostVersion = hostApiVersion ?? DevicePluginContract.CurrentVersion;
        if (hostVersion.Major <= 0 || hostVersion.Minor < 0)
            throw new ArgumentOutOfRangeException(nameof(hostApiVersion), "The host API version must have a positive major and non-negative minor.");

        var registered = new Dictionary<string, RegisteredPlugin>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in plugins)
        {
            ArgumentNullException.ThrowIfNull(plugin);
            var descriptor = plugin.Descriptor ?? throw new InvalidOperationException("A device plugin returned a null descriptor.");
            var pluginId = RequireId(descriptor.Id, "plugin");
            if (string.IsNullOrWhiteSpace(descriptor.DisplayName))
                throw new InvalidOperationException($"Device plugin '{pluginId}' has no display name.");
            if (!DevicePluginContract.IsCompatible(hostVersion, descriptor.ApiVersion))
                throw new InvalidOperationException(
                    $"Device plugin '{pluginId}' declares unsupported API {descriptor.ApiVersion.Major}.{descriptor.ApiVersion.Minor}; host supports {hostVersion.Major}.{hostVersion.Minor}.");
            if (descriptor.Profiles is null || descriptor.Profiles.Count == 0)
                throw new InvalidOperationException($"Device plugin '{pluginId}' must declare at least one profile.");

            if (registered.ContainsKey(pluginId))
                throw new InvalidOperationException($"Device plugin ID '{pluginId}' is registered more than once (IDs are case-insensitive).");

            var profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var profiles = new List<DeviceProfileDescriptor>(descriptor.Profiles.Count);
            foreach (var profile in descriptor.Profiles)
            {
                if (profile is null)
                    throw new InvalidOperationException($"Device plugin '{pluginId}' declares a null profile.");
                var profileId = RequireId(profile.Id, $"profile in plugin '{pluginId}'");
                if (string.IsNullOrWhiteSpace(profile.DisplayName))
                    throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has no display name.");
                if (profile.SchemaVersion < 1)
                    throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has an unsupported schema version.");
                if (profile.MinimumFrequencyHz < 0 || profile.MaximumFrequencyHz <= profile.MinimumFrequencyHz ||
                    profile.DefaultPointCount is < 2 or > 4000)
                    throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has an invalid frequency range or default point count.");
                if (profile.ReferenceImpedanceOhms is { } referenceImpedance &&
                    (!double.IsFinite(referenceImpedance) || referenceImpedance <= 0))
                    throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has an invalid reference impedance.");
                if (!profileIds.Add(profileId))
                    throw new InvalidOperationException($"Profile ID '{profileId}' is duplicated in plugin '{pluginId}' (IDs are case-insensitive).");
                if (profile.Capabilities is null || profile.Connectors is null || profile.FrequencyPresetsHz is null ||
                    profile.Presentation is null)
                    throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has a null capability, connector, preset, or presentation descriptor.");
                if (profile.FrequencyPresetsHz.Any(frequency => frequency < profile.MinimumFrequencyHz ||
                                                                frequency > profile.MaximumFrequencyHz))
                    throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has a frequency preset outside its declared range.");

                profiles.Add(profile with
                {
                    Capabilities = SnapshotCapabilities(profile.Capabilities, pluginId, profileId),
                    Connectors = SnapshotConnectors(profile.Connectors, pluginId, profileId),
                    FrequencyPresetsHz = Array.AsReadOnly(profile.FrequencyPresetsHz.ToArray()),
                    Presentation = SnapshotPresentation(profile.Presentation, pluginId, profileId),
                });
            }

            var snapshot = descriptor with { Profiles = Array.AsReadOnly(profiles.ToArray()) };
            registered.Add(pluginId, new RegisteredPlugin(plugin, snapshot));
        }

        _plugins = new System.Collections.ObjectModel.ReadOnlyDictionary<string, RegisteredPlugin>(registered);
        Descriptors = Array.AsReadOnly(registered.Values.Select(item => item.Descriptor).ToArray());
    }

    /// <summary>Returns the validated plugin descriptors in explicit registration order.</summary>
    public IReadOnlyList<DevicePluginDescriptor> Descriptors { get; }

    public bool TryGetPlugin(string pluginId, out IDevicePlugin? plugin)
    {
        if (!string.IsNullOrWhiteSpace(pluginId) && _plugins.TryGetValue(pluginId, out var registration))
        {
            plugin = registration.Plugin;
            return true;
        }

        plugin = null;
        return false;
    }

    public DeviceProfileDescriptor GetProfile(string pluginId, string profileId)
    {
        if (!_plugins.TryGetValue(pluginId, out var registration))
            throw new KeyNotFoundException($"Device plugin '{pluginId}' is not registered.");

        return registration.Descriptor.Profiles.FirstOrDefault(profile =>
                   string.Equals(profile.Id, profileId, StringComparison.OrdinalIgnoreCase))
               ?? throw new KeyNotFoundException($"Profile '{profileId}' is not declared by device plugin '{pluginId}'.");
    }

    private static string RequireId(string? id, string kind)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Trim() != id)
            throw new InvalidOperationException($"The {kind} ID must be non-empty and have no surrounding whitespace.");
        return id;
    }

    private static IReadOnlyList<DeviceCapabilityDescriptor> SnapshotCapabilities(
        IReadOnlyList<DeviceCapabilityDescriptor> capabilities,
        string pluginId,
        string profileId)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var snapshot = new DeviceCapabilityDescriptor[capabilities.Count];
        for (var index = 0; index < capabilities.Count; index++)
        {
            var capability = capabilities[index];
            if (capability is null)
                throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' contains a null capability.");
            var id = RequireId(capability.Id, $"capability in profile '{profileId}'");
            if (!ids.Add(id))
                throw new InvalidOperationException($"Capability ID '{id}' is duplicated in profile '{profileId}' (IDs are case-insensitive).");
            if (!Enum.IsDefined(capability.Kind) || !Enum.IsDefined(capability.Availability) || !Enum.IsDefined(capability.Evidence))
                throw new InvalidOperationException($"Capability '{id}' in profile '{profileId}' contains an unknown enum value.");
            snapshot[index] = capability;
        }

        return Array.AsReadOnly(snapshot);
    }

    private static IReadOnlyList<DeviceConnectorDescriptor> SnapshotConnectors(
        IReadOnlyList<DeviceConnectorDescriptor> connectors,
        string pluginId,
        string profileId)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var snapshot = new DeviceConnectorDescriptor[connectors.Count];
        for (var index = 0; index < connectors.Count; index++)
        {
            var connector = connectors[index];
            if (connector is null)
                throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' contains a null connector.");
            var id = RequireId(connector.Id, $"connector in profile '{profileId}'");
            if (!ids.Add(id))
                throw new InvalidOperationException($"Connector ID '{id}' is duplicated in profile '{profileId}' (IDs are case-insensitive).");
            if (string.IsNullOrWhiteSpace(connector.Label))
                throw new InvalidOperationException($"Connector '{id}' in profile '{profileId}' has no label.");
            if (!Enum.IsDefined(connector.Role) || !Enum.IsDefined(connector.LabelEvidence) || !Enum.IsDefined(connector.RoleEvidence))
                throw new InvalidOperationException($"Connector '{id}' in profile '{profileId}' contains an unknown enum value.");
            snapshot[index] = connector;
        }

        return Array.AsReadOnly(snapshot);
    }

    private static DeviceProfilePresentationDescriptor SnapshotPresentation(
        DeviceProfilePresentationDescriptor presentation,
        string pluginId,
        string profileId)
    {
        if (presentation.FrequencyBands is null || presentation.TraceColors is null)
            throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has a null presentation list or color map.");
        if (presentation.DefaultCableVelocityFactor is { } velocityFactor &&
            (!double.IsFinite(velocityFactor) || velocityFactor is <= 0 or > 1))
            throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has an invalid default cable velocity factor.");

        foreach (var band in presentation.FrequencyBands)
            if (band is null || string.IsNullOrWhiteSpace(band.Name) || !Enum.IsDefined(band.Group) ||
                band.StartFrequencyHz <= 0 || band.StopFrequencyHz <= band.StartFrequencyHz)
                throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has an invalid frequency band.");

        var traceColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in presentation.TraceColors)
        {
            if (string.IsNullOrWhiteSpace(key) || !key.Trim().Equals(key, StringComparison.Ordinal) ||
                value is null || value.Length != 7 || value[0] != '#' ||
                !uint.TryParse(value.AsSpan(1), System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' has an invalid trace color.");
            if (!traceColors.TryAdd(key, value.ToUpperInvariant()))
                throw new InvalidOperationException($"Profile '{profileId}' in plugin '{pluginId}' repeats trace color key '{key}'.");
        }

        return new DeviceProfilePresentationDescriptor(
            Array.AsReadOnly(presentation.FrequencyBands.ToArray()),
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(traceColors),
            presentation.DefaultCableVelocityFactor);
    }

    private sealed record RegisteredPlugin(IDevicePlugin Plugin, DevicePluginDescriptor Descriptor);
}
