namespace Vna.IO;

/// <summary>Shape and value validation for persisted user settings before they are applied.</summary>
public static class AppSettingsValidation
{
    public static bool TryValidate(AppSettings? settings, out string error)
    {
        if (settings is null)
        {
            error = "Settings are missing.";
            return false;
        }

        if (settings.DeviceSelection is null)
        {
            error = "Device selection settings are missing.";
            return false;
        }

        var pluginId = settings.DeviceSelection.PluginId;
        var profileId = settings.DeviceSelection.ProfileId;
        var pluginIdIsEmpty = string.IsNullOrWhiteSpace(pluginId);
        var profileIdIsEmpty = string.IsNullOrWhiteSpace(profileId);
        if (pluginId is null || profileId is null ||
            pluginIdIsEmpty != profileIdIsEmpty ||
            pluginIdIsEmpty && (pluginId.Length != 0 || profileId.Length != 0) ||
            !pluginIdIsEmpty && (pluginId.Trim() != pluginId || profileId.Trim() != profileId))
        {
            error = "Device plugin and profile IDs must either both be empty or both be non-empty without surrounding whitespace.";
            return false;
        }

        if (settings.MeasurementSource is null ||
            !settings.MeasurementSource.Trim().Equals("demo", StringComparison.OrdinalIgnoreCase) &&
            !settings.MeasurementSource.Trim().Equals("hardware", StringComparison.OrdinalIgnoreCase) &&
            !settings.MeasurementSource.Trim().Equals("playback", StringComparison.OrdinalIgnoreCase))
        {
            error = "Measurement source must be demo, hardware, or playback.";
            return false;
        }

        if (settings.PortName is null || settings.TraceColors is null)
        {
            error = "Port name and trace color settings cannot be null.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
