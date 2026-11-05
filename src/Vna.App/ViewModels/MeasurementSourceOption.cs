using Vna.Devices;

namespace Vna.App.ViewModels;

public sealed record MeasurementSourceOption(DeviceSourceMode Mode, string DisplayName);
