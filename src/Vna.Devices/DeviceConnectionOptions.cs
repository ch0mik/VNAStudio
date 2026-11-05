namespace Vna.Devices;

/// <summary>
/// Connection settings supplied by the user. PortName is passed through as entered so it can be a
/// Windows name (for example COM5) or a Unix device path (for example /dev/ttyUSB0).
/// </summary>
public sealed record DeviceConnectionOptions(string PortName);
