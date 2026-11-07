namespace Vna.Core;

/// <summary>Stable identifiers used when migrating sessions written before plugin IDs existed.</summary>
public static class LegacyDevicePluginIds
{
    public const string Vna65MHz = "legacy-vna65";
    public const string Vna65MHzProfile = "iw3hev-65mhz";
}

/// <summary>Stable identity of the current VNA 1–65 MHz plugin.</summary>
public static class DevicePluginIds
{
    public const string Vna65MHz = "iw3hev-vna65mhz";
}
