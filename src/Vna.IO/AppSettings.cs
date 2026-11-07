namespace Vna.IO;

/// <summary>User-editable settings. PortName is an opaque OS-specific serial device name or path.</summary>
public sealed record AppSettings
{
    public string PortName { get; init; } = string.Empty;
    public string MeasurementSource { get; init; } = "demo";
    public DeviceSelectionSettings DeviceSelection { get; init; } = new();
    public string Language { get; init; } = "en-US";
    public string Theme { get; init; } = "dark";
    public string AntennaChartView { get; init; } = "frequency";
    public string SmithSwrCircleMode { get; init; } = "hidden";
    public double SmithFixedSwr { get; init; } = 2;
    public bool Show50OhmReference { get; init; }
    public bool ShowXcXl { get; init; }
    public bool ReturnLossX2 { get; init; }
    public bool ShowFullMarkerValues { get; init; } = true;
    public double ReturnLossDisplayOffsetDb { get; init; }
    public double? CableVelocityFactor { get; init; }
    public Dictionary<string, string> TraceColors { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Selected plugin and profile, kept separate from application display preferences.</summary>
public sealed record DeviceSelectionSettings
{
    /// <summary>Empty values allow an unambiguous single registered plugin/profile to be selected by default.</summary>
    public string PluginId { get; init; } = string.Empty;
    public string ProfileId { get; init; } = string.Empty;
}

