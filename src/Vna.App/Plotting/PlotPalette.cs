using Vna.Core;
using Vna.App.Theming;
using Avalonia.Media;

namespace Vna.App.Plotting;

public sealed class PlotPalette
{
    private readonly IReadOnlyDictionary<PlotTrace, string> _colors;

    private PlotPalette(LegacyPlotColorSettings colors, AppThemeKind theme = AppThemeKind.Light)
    {
        var traces = new Dictionary<PlotTrace, string>
        {
            [PlotTrace.Phase] = ToHexColor(colors.Phase),
            [PlotTrace.ReturnLoss] = ToHexColor(colors.ReturnLoss),
            [PlotTrace.Reactance] = ToHexColor(colors.Reactance),
            [PlotTrace.Resistance] = ToHexColor(colors.Resistance),
            [PlotTrace.Impedance] = ToHexColor(colors.Impedance),
            [PlotTrace.Swr] = ToHexColor(colors.Swr),
        };
        IsNightVision = theme == AppThemeKind.NightVision;
        Background = theme switch { AppThemeKind.Dark => "#101722", AppThemeKind.NightVision => "#030000", _ => ToHexColor(colors.Background) };
        Grid = theme switch { AppThemeKind.Dark => "#273244", AppThemeKind.NightVision => "#280000", _ => "#E5EAF1" };
        Border = theme switch { AppThemeKind.Dark => "#45536C", AppThemeKind.NightVision => "#600000", _ => "#C9D2DF" };
        AxisText = theme switch { AppThemeKind.Dark => "#AAB8CC", AppThemeKind.NightVision => "#A00000", _ => "#566174" };
        AxisTitle = theme switch { AppThemeKind.Dark => "#DBE5F3", AppThemeKind.NightVision => "#C00000", _ => "#344258" };
        MinimumMarker = theme switch { AppThemeKind.Dark => "#69C69D", AppThemeKind.NightVision => "#A00000", _ => "#0F8A57" };
        Marker1 = theme switch { AppThemeKind.Dark => "#FF8192", AppThemeKind.NightVision => "#D00000", _ => "#C82F46" };
        Marker2 = theme switch { AppThemeKind.Dark => "#89B7FF", AppThemeKind.NightVision => "#880000", _ => "#1753C8" };
        if (theme == AppThemeKind.Dark)
            foreach (var trace in traces.Keys.ToArray()) traces[trace] = LiftForDark(traces[trace]);
        if (IsNightVision)
        {
            traces[PlotTrace.Swr] = "#C00000";
            traces[PlotTrace.ReturnLoss] = "#800000";
            traces[PlotTrace.Phase] = "#B00000";
            traces[PlotTrace.Impedance] = "#E00000";
            traces[PlotTrace.Resistance] = "#980000";
            traces[PlotTrace.Reactance] = "#680000";
        }
        _colors = traces;
    }

    public string Background { get; }
    public string Grid { get; }
    public string Border { get; }
    public string AxisText { get; }
    public string AxisTitle { get; }
    public string MinimumMarker { get; }
    public string Marker1 { get; }
    public string Marker2 { get; }
    public bool IsNightVision { get; }
    public static PlotPalette LegacyDefault { get; } = new(LegacyPlotColorSettings.Default);

    public string GetColor(PlotTrace trace) => _colors[trace];

    public IReadOnlyList<double> GetDashPattern(PlotTrace trace) => IsNightVision && NightPatterns.TryGetValue(trace, out var pattern)
        ? pattern : Array.Empty<double>();

    private static readonly IReadOnlyDictionary<PlotTrace, IReadOnlyList<double>> NightPatterns =
        new Dictionary<PlotTrace, IReadOnlyList<double>>
        {
            [PlotTrace.ReturnLoss] = Array.AsReadOnly(new double[] { 6, 3 }),
            [PlotTrace.Phase] = Array.AsReadOnly(new double[] { 3, 3 }),
            [PlotTrace.Impedance] = Array.AsReadOnly(new double[] { 10, 3, 2, 3 }),
            [PlotTrace.Resistance] = Array.AsReadOnly(new double[] { 1, 3 }),
            [PlotTrace.Reactance] = Array.AsReadOnly(new double[] { 6, 3, 1, 3, 1, 3 }),
        };

    public static PlotPalette FromProfile(
        DeviceProfile profile,
        AppThemeKind theme = AppThemeKind.Light,
        IReadOnlyDictionary<PlotTrace, string>? traceColorOverrides = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var colors = profile.PlotColors;
        if (traceColorOverrides is not null)
        {
            uint ColorRef(PlotTrace trace, uint original)
            {
                if (!traceColorOverrides.TryGetValue(trace, out var value)) return original;
                var color = Color.Parse(value);
                return (uint)(color.R | color.G << 8 | color.B << 16);
            }

            colors = colors with
            {
                Swr = ColorRef(PlotTrace.Swr, colors.Swr),
                ReturnLoss = ColorRef(PlotTrace.ReturnLoss, colors.ReturnLoss),
                Phase = ColorRef(PlotTrace.Phase, colors.Phase),
                Impedance = ColorRef(PlotTrace.Impedance, colors.Impedance),
                Resistance = ColorRef(PlotTrace.Resistance, colors.Resistance),
                Reactance = ColorRef(PlotTrace.Reactance, colors.Reactance),
            };
        }

        return new PlotPalette(colors, theme);
    }

    private static string LiftForDark(string color)
    {
        var original = Color.Parse(color);
        static double Channel(byte channel) => channel / 255d <= 0.04045
            ? channel / 255d / 12.92 : Math.Pow((channel / 255d + 0.055) / 1.055, 2.4);
        static double Luminance(Color c) => 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        var background = Luminance(Color.Parse("#101722"));
        for (var step = 0; step <= 50; step++)
        {
            var amount = step / 50d;
            var lifted = Color.FromRgb((byte)Math.Round(original.R + (255 - original.R) * amount),
                (byte)Math.Round(original.G + (255 - original.G) * amount),
                (byte)Math.Round(original.B + (255 - original.B) * amount));
            if ((Luminance(lifted) + 0.05) / (background + 0.05) >= 4.5)
                return $"#{lifted.R:X2}{lifted.G:X2}{lifted.B:X2}";
        }
        return "#FFFFFF";
    }

    private static string ToHexColor(uint colorRef) =>
        $"#{(byte)colorRef:X2}{(byte)(colorRef >> 8):X2}{(byte)(colorRef >> 16):X2}";
}
