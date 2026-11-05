using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Globalization;
using Vna.Core;
using Vna.Localization;

namespace Vna.App.Plotting;

/// <summary>Initial custom plot surface. Axis transforms are intentionally explicit and will be refined against legacy captures.</summary>
public sealed class VnaPlotControl : Control
{
    private bool? _dragMarker2;
    private bool? _activeMarker2;
    private bool _isPanning;
    private double _panPreviousX;

    public VnaPlotControl() => Focusable = true;

    public event Action<long, bool>? MarkerPicked;
    public event Action<bool, int>? MarkerStepRequested;
    public event Action<bool>? ZoomRequested;
    public event Action<long>? PanRequested;
    public static readonly StyledProperty<IReadOnlyList<MeasurementPoint>> PointsProperty =
        AvaloniaProperty.Register<VnaPlotControl, IReadOnlyList<MeasurementPoint>>(nameof(Points), Array.Empty<MeasurementPoint>());
    public static readonly StyledProperty<PlotTrace> VisibleTracesProperty =
        AvaloniaProperty.Register<VnaPlotControl, PlotTrace>(nameof(VisibleTraces), PlotTrace.All);
    public static readonly StyledProperty<ResponsePlotMode> ResponseModeProperty =
        AvaloniaProperty.Register<VnaPlotControl, ResponsePlotMode>(nameof(ResponseMode), ResponsePlotMode.Standard);
    public static readonly StyledProperty<PlotPalette> PaletteProperty =
        AvaloniaProperty.Register<VnaPlotControl, PlotPalette>(nameof(Palette), PlotPalette.LegacyDefault);
    public static readonly StyledProperty<string> LanguageCodeProperty =
        AvaloniaProperty.Register<VnaPlotControl, string>(nameof(LanguageCode), "pl-PL");
    public static readonly StyledProperty<long?> Marker1FrequencyProperty =
        AvaloniaProperty.Register<VnaPlotControl, long?>(nameof(Marker1Frequency));
    public static readonly StyledProperty<long?> Marker2FrequencyProperty =
        AvaloniaProperty.Register<VnaPlotControl, long?>(nameof(Marker2Frequency));
    public static readonly StyledProperty<long?> MinimumSwrFrequencyProperty =
        AvaloniaProperty.Register<VnaPlotControl, long?>(nameof(MinimumSwrFrequency));
    public static readonly StyledProperty<bool> ShowMarker1Property =
        AvaloniaProperty.Register<VnaPlotControl, bool>(nameof(ShowMarker1), true);
    public static readonly StyledProperty<bool> ShowMarker2Property =
        AvaloniaProperty.Register<VnaPlotControl, bool>(nameof(ShowMarker2), true);
    public static readonly StyledProperty<bool> Show50OhmReferenceProperty =
        AvaloniaProperty.Register<VnaPlotControl, bool>(nameof(Show50OhmReference));
    public static readonly StyledProperty<bool> ReturnLossX2Property =
        AvaloniaProperty.Register<VnaPlotControl, bool>(nameof(ReturnLossX2));
    public static readonly StyledProperty<bool> ShowMinimumSwrProperty =
        AvaloniaProperty.Register<VnaPlotControl, bool>(nameof(ShowMinimumSwr), true);
    public static readonly StyledProperty<long> ViewStartHzProperty =
        AvaloniaProperty.Register<VnaPlotControl, long>(nameof(ViewStartHz));
    public static readonly StyledProperty<long> ViewStopHzProperty =
        AvaloniaProperty.Register<VnaPlotControl, long>(nameof(ViewStopHz));

    static VnaPlotControl() => AffectsRender<VnaPlotControl>(PointsProperty, VisibleTracesProperty, ResponseModeProperty, PaletteProperty, LanguageCodeProperty,
        Marker1FrequencyProperty, Marker2FrequencyProperty, MinimumSwrFrequencyProperty, ShowMarker1Property, ShowMarker2Property,
        ShowMinimumSwrProperty, Show50OhmReferenceProperty, ReturnLossX2Property, ViewStartHzProperty, ViewStopHzProperty);

    public IReadOnlyList<MeasurementPoint> Points { get => GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    public PlotTrace VisibleTraces { get => GetValue(VisibleTracesProperty); set => SetValue(VisibleTracesProperty, value); }
    public ResponsePlotMode ResponseMode { get => GetValue(ResponseModeProperty); set => SetValue(ResponseModeProperty, value); }
    public PlotPalette Palette { get => GetValue(PaletteProperty); set => SetValue(PaletteProperty, value); }
    public string LanguageCode { get => GetValue(LanguageCodeProperty); set => SetValue(LanguageCodeProperty, value); }
    public long? Marker1Frequency { get => GetValue(Marker1FrequencyProperty); set => SetValue(Marker1FrequencyProperty, value); }
    public long? Marker2Frequency { get => GetValue(Marker2FrequencyProperty); set => SetValue(Marker2FrequencyProperty, value); }
    public long? MinimumSwrFrequency { get => GetValue(MinimumSwrFrequencyProperty); set => SetValue(MinimumSwrFrequencyProperty, value); }
    public bool ShowMarker1 { get => GetValue(ShowMarker1Property); set => SetValue(ShowMarker1Property, value); }
    public bool ShowMarker2 { get => GetValue(ShowMarker2Property); set => SetValue(ShowMarker2Property, value); }
    public bool ShowMinimumSwr { get => GetValue(ShowMinimumSwrProperty); set => SetValue(ShowMinimumSwrProperty, value); }
    public bool Show50OhmReference { get => GetValue(Show50OhmReferenceProperty); set => SetValue(Show50OhmReferenceProperty, value); }
    public bool ReturnLossX2 { get => GetValue(ReturnLossX2Property); set => SetValue(ReturnLossX2Property, value); }
    public long ViewStartHz { get => GetValue(ViewStartHzProperty); set => SetValue(ViewStartHzProperty, value); }
    public long ViewStopHz { get => GetValue(ViewStopHzProperty); set => SetValue(ViewStopHzProperty, value); }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        if (bounds.Width < 10 || bounds.Height < 10) return;
        context.DrawRectangle(new SolidColorBrush(Color.Parse(Palette.Background)), null, new Rect(bounds.Size));
        var plot = GetPlotArea();
        var gridPen = new Pen(new SolidColorBrush(Color.Parse(Palette.Grid)), 1);
        var borderPen = new Pen(new SolidColorBrush(Color.Parse(Palette.Border)), 1);
        for (var i = 0; i <= 5; i++)
        {
            var y = plot.Top + plot.Height * i / 5;
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
        }
        for (var i = 0; i <= 8; i++)
        {
            var x = plot.Left + plot.Width * i / 8;
            context.DrawLine(gridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
        }
        context.DrawRectangle(null, borderPen, plot);
        var (start, stop) = GetFrequencyRange();
        var returnLossAxis = ReturnLossX2 ? PlotSeriesCatalog.ReturnLossX2 : PlotSeriesCatalog.ReturnLoss;
        DrawAxisLabels(context, plot, start, stop, CultureInfo.GetCultureInfo(LocalizationService.NormalizeLanguage(LanguageCode)), Palette,
            ResponseMode == ResponsePlotMode.Standard ? returnLossAxis : GetResponseAxis());
        if (Show50OhmReference)
        {
            var referenceY = plot.Top + PlotSeriesCatalog.Ohms.FractionFromTop(50) * plot.Height;
            var referencePen = new Pen(new SolidColorBrush(Color.Parse(Palette.GetColor(PlotTrace.Impedance))), 1.2, DashStyle.Dash);
            context.DrawLine(referencePen, new Point(plot.Left, referenceY), new Point(plot.Right, referenceY));
        }
        if (Points.Count == 0) return;

        var transform = new PlotCoordinateTransform(start, stop, plot.Left, plot.Top, plot.Width, plot.Height);
        foreach (var trace in PlotSeriesCatalog.All)
        {
            if ((VisibleTraces & trace.Trace) == 0) continue;
            var renderedTrace = trace.Trace == PlotTrace.ReturnLoss ? ResponseMode switch
            {
                ResponsePlotMode.CableLoss => trace with { Axis = PlotSeriesCatalog.CableLoss, ReadValue = point => point.CableLossDb },
                ResponsePlotMode.Transmission => trace with { Axis = PlotSeriesCatalog.Transmission, ReadValue = point => point.TransmissionDb },
                _ => trace with { Axis = returnLossAxis },
            } : trace;
            var pattern = Palette.GetDashPattern(trace.Trace);
            var pen = new Pen(new SolidColorBrush(Color.Parse(Palette.GetColor(trace.Trace))), 1.7,
                pattern.Count == 0 ? null : new DashStyle(pattern, 0));
            var geometry = new StreamGeometry();
            using (var path = geometry.Open())
                foreach (var run in PlotSeriesGeometry.GetRuns(Points, renderedTrace, transform))
                {
                    if (run.Count < 2) continue;
                    path.BeginFigure(new Point(run[0].X, run[0].Y), false);
                    for (var index = 1; index < run.Count; index++) path.LineTo(new Point(run[index].X, run[index].Y));
                    path.EndFigure(false);
                }
            context.DrawGeometry(null, pen, geometry);
        }

        DrawMarker(context, plot, transform, ResponseMode == ResponsePlotMode.Standard && ShowMinimumSwr ? MinimumSwrFrequency : null, Palette.MinimumMarker,
            Palette.IsNightVision ? DashStyle.Dot : null);
        DrawMarker(context, plot, transform, ShowMarker1 ? Marker1Frequency : null, Palette.Marker1);
        DrawMarker(context, plot, transform, ShowMarker2 ? Marker2Frequency : null, Palette.Marker2,
            Palette.IsNightVision ? DashStyle.Dash : null);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var properties = e.GetCurrentPoint(this).Properties;
        var position = e.GetPosition(this);
        var isShiftDrag = e.KeyModifiers.HasFlag(KeyModifiers.Shift) && properties.IsLeftButtonPressed;
        if ((properties.IsMiddleButtonPressed || isShiftDrag) && GetPlotArea().Contains(position))
        {
            _isPanning = true;
            _panPreviousX = position.X;
            Focus();
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }
        if (!properties.IsLeftButtonPressed && !properties.IsRightButtonPressed) return;
        var isMarker2 = properties.IsRightButtonPressed;
        if (!PickMarkerAt(position, isMarker2, requireInside: true)) return;
        _activeMarker2 = isMarker2;
        Focus();
        _dragMarker2 = isMarker2;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Add or Key.OemPlus or Key.Subtract or Key.OemMinus)
        {
            ZoomRequested?.Invoke(e.Key is Key.Add or Key.OemPlus);
            e.Handled = true;
            return;
        }
        if (_activeMarker2 is not { } isMarker2 || e.Key is not (Key.Left or Key.Right)) return;

        MarkerStepRequested?.Invoke(isMarker2, e.Key == Key.Right ? 1 : -1);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isPanning)
        {
            var position = e.GetPosition(this);
            var (start, stop) = GetFrequencyRange();
            var plotWidth = GetPlotArea().Width;
            if (stop > start && plotWidth > 0)
            {
                var shiftHz = (long)Math.Round((_panPreviousX - position.X) / plotWidth * (stop - start));
                if (shiftHz != 0)
                {
                    PanRequested?.Invoke(shiftHz);
                    _panPreviousX = position.X;
                }
            }
            e.Handled = true;
            return;
        }
        if (_dragMarker2 is not { } isMarker2) return;
        PickMarkerAt(e.GetPosition(this), isMarker2, requireInside: false);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }
        if (_dragMarker2 is null) return;
        _dragMarker2 = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isPanning = false;
        _dragMarker2 = null;
    }

    private bool PickMarkerAt(Point position, bool isMarker2, bool requireInside)
    {
        if (Points.Count == 0) return false;
        var plot = GetPlotArea();
        if (requireInside && !plot.Contains(position)) return false;
        var (start, stop) = GetFrequencyRange();
        var transform = new PlotCoordinateTransform(start, stop, plot.Left, plot.Top, plot.Width, plot.Height);
        var sample = MeasurementPointQueries.FindNearest(Points, transform.XToFrequency(position.X), start, stop);
        if (sample is null) return false;
        MarkerPicked?.Invoke(sample.FrequencyHz, isMarker2);
        return true;
    }

    private Rect GetPlotArea() => new(68, 24, Math.Max(1, Bounds.Width - 130), Math.Max(1, Bounds.Height - 56));

    private (long Start, long Stop) GetFrequencyRange() => ViewStopHz > ViewStartHz
        ? (ViewStartHz, ViewStopHz)
        : Points.Count == 0 ? (0, 0) : (Points[0].FrequencyHz, Points[^1].FrequencyHz);

    private static void DrawMarker(
        DrawingContext context,
        Rect plot,
        PlotCoordinateTransform transform,
        long? frequency,
        string color,
        IDashStyle? dashStyle = null)
    {
        if (frequency is null || !transform.ContainsFrequency(frequency.Value)) return;
        var x = transform.FrequencyToX(frequency.Value);
        context.DrawLine(new Pen(new SolidColorBrush(Color.Parse(color)), 1.4, dashStyle),
            new Point(x, plot.Top), new Point(x, plot.Bottom));
    }

    private static void DrawAxisLabels(DrawingContext context, Rect plot, long start, long stop, CultureInfo culture, PlotPalette palette, PlotAxisScale returnLossAxis)
    {
        var labelBrush = new SolidColorBrush(Color.Parse(palette.AxisText));
        var titleBrush = new SolidColorBrush(Color.Parse(palette.AxisTitle));
        IReadOnlyList<(PlotAxisScale Scale, double X)> axes = returnLossAxis == PlotSeriesCatalog.ReturnLoss || returnLossAxis == PlotSeriesCatalog.ReturnLossX2
            ? new (PlotAxisScale Scale, double X)[]
            {
                (PlotSeriesCatalog.Swr, 4), (returnLossAxis, 37),
                (PlotSeriesCatalog.Phase, plot.Right + 4), (PlotSeriesCatalog.Ohms, plot.Right + 34),
            }
            : new (PlotAxisScale Scale, double X)[] { (returnLossAxis, 37d) };
        foreach (var axis in axes) DrawText(context, axis.Scale.Label, axis.X, 2, titleBrush, 7);

        foreach (var axis in axes)
        {
            foreach (var tick in axis.Scale.Ticks)
            {
                var y = plot.Top + plot.Height * axis.Scale.FractionFromTop(tick.Value) - 5;
                var label = tick.Label ?? tick.Value.ToString(axis.Scale.TickFormat, culture);
                DrawText(context, label, axis.X, y, labelBrush, 7);
            }
        }

        if (stop <= start) return;
        for (var i = 0; i <= 4; i++)
        {
            var fraction = i / 4d;
            var x = plot.Left + plot.Width * fraction;
            var mhz = (start + (stop - start) * fraction) / 1_000_000.0;
            DrawText(context, mhz.ToString("0.##", culture), x - 10, plot.Bottom + 4, labelBrush, 7);
        }
        DrawText(context, "MHz", plot.Right - 18, plot.Bottom + 18, labelBrush, 7);
    }

    private PlotAxisScale GetResponseAxis() => ResponseMode switch
    {
        ResponsePlotMode.CableLoss => PlotSeriesCatalog.CableLoss,
        ResponsePlotMode.Transmission => PlotSeriesCatalog.Transmission,
        _ => ReturnLossX2 ? PlotSeriesCatalog.ReturnLossX2 : PlotSeriesCatalog.ReturnLoss,
    };

    private static void DrawText(DrawingContext context, string text, double x, double y, IBrush brush, double size)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal), size, brush);
        context.DrawText(formatted, new Point(x, y));
    }
}



