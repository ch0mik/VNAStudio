namespace Vna.IO;

/// <summary>User-editable settings. PortName is an opaque OS-specific serial device name or path.</summary>
public sealed record AppSettings
{
    public string PortName { get; init; } = string.Empty;
    public string MeasurementSource { get; init; } = "demo";
    public string Language { get; init; } = "en-US";
    public string Theme { get; init; } = "dark";
    public string AntennaChartView { get; init; } = "frequency";
    public bool Show50OhmReference { get; init; }
    public bool ShowXcXl { get; init; }
    public bool ReturnLossX2 { get; init; }
    public bool ShowFullMarkerValues { get; init; } = true;
    public double ReturnLossDisplayOffsetDb { get; init; }
    public double? CableVelocityFactor { get; init; }
    public Dictionary<string, string> TraceColors { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

