# Selecting a device plugin and profile

VNA Studio keeps device identity in the `DeviceSelection` section of `%LOCALAPPDATA%/VnaStudio/settings.json`. Application preferences such as language, theme, and chart options remain separate settings.

```json
{
  "DeviceSelection": {
    "PluginId": "iw3hev-vna65mhz",
    "ProfileId": "iw3hev-65mhz"
  }
}
```

The IDs are stable identifiers from the registered plugin catalog, not display names or filenames. The in-app **Help → Registered device profiles** list shows the IDs and the profile's declared range and capabilities.

If both IDs are empty or omitted, the app selects a profile automatically only when exactly one plugin and one profile are registered. With multiple choices, set both IDs. A partial pair, malformed settings, an unregistered plugin, or an undeclared profile stops startup with a diagnostic; the app does not silently switch to Demo or another device. After changing the selection in the settings file, restart VNA Studio to create the selected plugin source.

The serial endpoint remains the separate `PortName` application setting and is passed through as supplied by the operating system. Selecting a plugin does not discover, open, or send anything to a port. Hardware operations remain behind that plugin's declared support and evidence gates.
