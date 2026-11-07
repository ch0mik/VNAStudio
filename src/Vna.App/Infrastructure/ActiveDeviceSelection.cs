using Vna.Core;
using Vna.IO;

namespace Vna.App.Infrastructure;

/// <summary>The validated plugin/profile pair selected for this application launch.</summary>
public sealed record ActiveDeviceSelection(
    string PluginId,
    IDevicePlugin Plugin,
    DeviceProfileDescriptor Profile)
{
    public static ActiveDeviceSelection Resolve(DevicePluginCatalog catalog, DeviceSelectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(settings);

        var pluginId = settings.PluginId;
        var profileId = settings.ProfileId;
        if (string.IsNullOrWhiteSpace(pluginId) && string.IsNullOrWhiteSpace(profileId))
        {
            if (catalog.Descriptors.Count != 1 || catalog.Descriptors[0].Profiles.Count != 1)
                throw new InvalidOperationException(
                    "Choose both DeviceSelection:PluginId and DeviceSelection:ProfileId in settings.json; automatic selection requires exactly one registered plugin and profile.");

            pluginId = catalog.Descriptors[0].Id;
            profileId = catalog.Descriptors[0].Profiles[0].Id;
        }
        else if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(profileId))
        {
            throw new InvalidOperationException(
                "DeviceSelection:PluginId and DeviceSelection:ProfileId must be configured together.");
        }

        if (!catalog.TryGetPlugin(pluginId, out var plugin) || plugin is null)
            throw new InvalidOperationException(
                $"Selected device plugin '{pluginId}' is not registered. Check DeviceSelection:PluginId in settings.json.");

        DeviceProfileDescriptor profile;
        try
        {
            profile = catalog.GetProfile(pluginId, profileId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"Selected profile '{profileId}' is not declared by plugin '{pluginId}'. Check DeviceSelection in settings.json.", exception);
        }

        return new ActiveDeviceSelection(plugin.Descriptor.Id, plugin, profile);
    }
}
