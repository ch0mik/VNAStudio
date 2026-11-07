using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Vna.Core;
using Vna.Localization;

namespace Vna.App.Plotting;

public enum SmithSwrCircleMode { Hidden, Fixed, Marker1 }

/// <summary>Responsive, vector Smith impedance chart. Marker interaction is added by the view-model stage.</summary>
public sealed class SmithPlotControl : Control
{
    private const double CircleMargin = 34;
    private static readonly IReadOnlyList<SmithGridCurve> NormalizedGrid = SmithChartGrid.Create(50, 192);
    private bool? _dragMarker2;
    private bool _isPanning;
    private bool? _activeMarker2;
    private Point _lastPointerPosition;
    private Point? _hoverPosition;

    public SmithPlotControl() => Focusable = true;

    public event Action<long, bool>? MarkerPicked;
    public event Action<bool, int>? MarkerStepRequested;
    public event Action<double, SmithComplex, double, double>? ZoomRequested;
    public event Action<double, double>? PanRequested;

    public static readonly StyledProperty<IReadOnlyList<MeasurementPoint>> PointsProperty =
        AvaloniaProperty.Register<SmithPlotControl, IReadOnlyList<MeasurementPoint>>(nameof(Points), Array.Empty<MeasurementPoint>());
    public static readonly StyledProperty<IReadOnlyList<SmithComparisonOverlay>> ComparisonOverlaysProperty =
        AvaloniaProperty.Register<SmithPlotControl, IReadOnlyList<SmithComparisonOverlay>>(
            nameof(ComparisonOverlays), Array.Empty<SmithComparisonOverlay>());
    public static readonly StyledProperty<double?> ReferenceImpedanceOhmsProperty =
        AvaloniaProperty.Register<SmithPlotControl, double?>(nameof(ReferenceImpedanceOhms));
    public static readonly StyledProperty<PlotPalette> PaletteProperty =
        AvaloniaProperty.Register<SmithPlotControl, PlotPalette>(nameof(Palette), PlotPalette.LegacyDefault);
    public static readonly StyledProperty<string> LanguageCodeProperty =
        AvaloniaProperty.Register<SmithPlotControl, string>(nameof(LanguageCode), "en-US");
    public static readonly StyledProperty<long?> Marker1FrequencyProperty =
        AvaloniaProperty.Register<SmithPlotControl, long?>(nameof(Marker1Frequency));
    public static readonly StyledProperty<long?> Marker2FrequencyProperty =
        AvaloniaProperty.Register<SmithPlotControl, long?>(nameof(Marker2Frequency));
    public static readonly StyledProperty<bool> ShowMarker1Property =
        AvaloniaProperty.Register<SmithPlotControl, bool>(nameof(ShowMarker1), true);
    public static readonly StyledProperty<bool> ShowMarker2Property =
        AvaloniaProperty.Register<SmithPlotControl, bool>(nameof(ShowMarker2), true);
    public static readonly StyledProperty<long> ViewStartHzProperty =
        AvaloniaProperty.Register<SmithPlotControl, long>(nameof(ViewStartHz));
    public static readonly StyledProperty<long> ViewStopHzProperty =
        AvaloniaProperty.Register<SmithPlotControl, long>(nameof(ViewStopHz));
    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<SmithPlotControl, double>(nameof(Zoom), 1);
    public static readonly StyledProperty<double> PanRealProperty =
        AvaloniaProperty.Register<SmithPlotControl, double>(nameof(PanReal));
    public static readonly StyledProperty<double> PanImaginaryProperty =
        AvaloniaProperty.Register<SmithPlotControl, double>(nameof(PanImaginary));
    public static readonly StyledProperty<SmithSwrCircleMode> SwrCircleModeProperty =
        AvaloniaProperty.Register<SmithPlotControl, SmithSwrCircleMode>(nameof(SwrCircleMode));
    public static readonly StyledProperty<double?> FixedSwrProperty =
        AvaloniaProperty.Register<SmithPlotControl, double?>(nameof(FixedSwr));

    static SmithPlotControl() => AffectsRender<SmithPlotControl>(PointsProperty, ComparisonOverlaysProperty, ReferenceImpedanceOhmsProperty,
        PaletteProperty, LanguageCodeProperty, Marker1FrequencyProperty, Marker2FrequencyProperty,
        ShowMarker1Property, ShowMarker2Property, ViewStartHzProperty, ViewStopHzProperty,
        ZoomProperty, PanRealProperty, PanImaginaryProperty, SwrCircleModeProperty, FixedSwrProperty);

    public IReadOnlyList<MeasurementPoint> Points { get => GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    public IReadOnlyList<SmithComparisonOverlay> ComparisonOverlays
    {
        get => GetValue(ComparisonOverlaysProperty);
        set => SetValue(ComparisonOverlaysProperty, value);
    }
    public double? ReferenceImpedanceOhms { get => GetValue(ReferenceImpedanceOhmsProperty); set => SetValue(ReferenceImpedanceOhmsProperty, value); }
    public PlotPalette Palette { get => GetValue(PaletteProperty); set => SetValue(PaletteProperty, value); }
    public string LanguageCode { get => GetValue(LanguageCodeProperty); set => SetValue(LanguageCodeProperty, value); }
    public long? Marker1Frequency { get => GetValue(Marker1FrequencyProperty); set => SetValue(Marker1FrequencyProperty, value); }
    public long? Marker2Frequency { get => GetValue(Marker2FrequencyProperty); set => SetValue(Marker2FrequencyProperty, value); }
    public bool ShowMarker1 { get => GetValue(ShowMarker1Property); set => SetValue(ShowMarker1Property, value); }
    public bool ShowMarker2 { get => GetValue(ShowMarker2Property); set => SetValue(ShowMarker2Property, value); }
    public long ViewStartHz { get => GetValue(ViewStartHzProperty); set => SetValue(ViewStartHzProperty, value); }
    public long ViewStopHz { get => GetValue(ViewStopHzProperty); set => SetValue(ViewStopHzProperty, value); }
    public double Zoom { get => GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public double PanReal { get => GetValue(PanRealProperty); set => SetValue(PanRealProperty, value); }
    public double PanImaginary { get => GetValue(PanImaginaryProperty); set => SetValue(PanImaginaryProperty, value); }
    public SmithSwrCircleMode SwrCircleMode { get => GetValue(SwrCircleModeProperty); set => SetValue(SwrCircleModeProperty, value); }
    public double? FixedSwr { get => GetValue(FixedSwrProperty); set => SetValue(FixedSwrProperty, value); }

    public int CountOutsideUnitCircle => Points.Count(point =>
        IsInCurrentRange(point) && point.Quality == SampleQuality.Valid && TryGetCandidates(point, out var reflection, out var mirror) &&
        (!SmithChartMath.IsInsideOrOnUnitCircle(reflection) ||
         mirror is { } alternate && !SmithChartMath.IsInsideOrOnUnitCircle(alternate)));

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0) return;

        var background = new SolidColorBrush(Color.Parse(Palette.Background));
        context.FillRectangle(background, new Rect(0, 0, width, height));
        if (width <= 2 * CircleMargin || height <= 2 * CircleMargin) return;

        var plot = new Rect(CircleMargin, CircleMargin, width - 2 * CircleMargin, height - 2 * CircleMargin);
        var transform = new SmithChartTransform(plot.Width, plot.Height, Math.Clamp(Zoom, 1, 8), PanReal, PanImaginary);
        var transformedCenter = transform.ToPixel(new SmithComplex(0, 0));
        var circle = new Rect(plot.Left + transformedCenter.X - transform.RadiusPixels,
            plot.Top + transformedCenter.Y - transform.RadiusPixels,
            2 * transform.RadiusPixels, 2 * transform.RadiusPixels);
        var gridPen = new Pen(new SolidColorBrush(Color.Parse(Palette.Grid)), 0.8);
        var borderPen = new Pen(new SolidColorBrush(Color.Parse(Palette.Border)), 1.2);

        context.DrawEllipse(null, borderPen, circle);
        var hasSwrCircle = TryGetSwrCircle(out var swrCircleValue, out var swrCircleMagnitude);
        using (context.PushGeometryClip(new EllipseGeometry(circle)))
        {
            foreach (var curve in NormalizedGrid)
                DrawCurve(context, curve.Points, transform, plot, gridPen, close: false);
            if (hasSwrCircle) DrawSwrCircle(context, transform, plot, swrCircleMagnitude);
            DrawComparisonData(context, transform, plot);
            DrawData(context, transform, plot);
        }
        if (hasSwrCircle) DrawSwrCircleLabel(context, plot, swrCircleValue);
        DrawLabels(context, circle, plot, transform);
        DrawMarkers(context, transform, plot);
        if (_hoverPosition is { } hoverPosition && FindNearestSample(hoverPosition) is { } hoverSample)
            DrawHoverReadout(context, hoverPosition, hoverSample);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var properties = e.GetCurrentPoint(this).Properties;
        var position = e.GetPosition(this);
        if (properties.IsMiddleButtonPressed)
        {
            _hoverPosition = null;
            InvalidateVisual();
            _isPanning = true;
            _lastPointerPosition = position;
            Focus();
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }
        if (!properties.IsLeftButtonPressed && !properties.IsRightButtonPressed) return;

        var isMarker2 = properties.IsRightButtonPressed;
        var selectedFrequency = FindNearestFrequency(position, 18);
        if (selectedFrequency is null) return;
        _hoverPosition = null;
        InvalidateVisual();
        _activeMarker2 = isMarker2;
        _dragMarker2 = isMarker2;
        MarkerPicked?.Invoke(selectedFrequency.Value, isMarker2);
        Focus();
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this);
        if (_isPanning)
        {
            var plot = GetPlotArea();
            var baseRadius = Math.Min(plot.Width, plot.Height) * 0.5;
            if (baseRadius > 0)
            {
                PanRequested?.Invoke(-(position.X - _lastPointerPosition.X) / (baseRadius * Math.Clamp(Zoom, 1, 8)),
                    (position.Y - _lastPointerPosition.Y) / (baseRadius * Math.Clamp(Zoom, 1, 8)));
            }
            _lastPointerPosition = position;
            e.Handled = true;
            return;
        }
        if (_dragMarker2 is { } isMarker2)
        {
            var frequency = FindNearestFrequency(position, 24);
            if (frequency is not null) MarkerPicked?.Invoke(frequency.Value, isMarker2);
            e.Handled = true;
            return;
        }

        _hoverPosition = position;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoverPosition is null) return;
        _hoverPosition = null;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isPanning && _dragMarker2 is null) return;
        _isPanning = false;
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

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        RequestZoom(e.Delta.Y > 0 ? 2 : 0.5, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Add or Key.OemPlus or Key.Subtract or Key.OemMinus)
        {
            var plot = GetPlotArea();
            RequestZoom(e.Key is Key.Add or Key.OemPlus ? 2 : 0.5,
                new Point(plot.Left + plot.Width / 2, plot.Top + plot.Height / 2));
            e.Handled = true;
        }
        else if (e.Key is Key.Left or Key.Right)
        {
            var isMarker2 = _activeMarker2 ?? false;
            _activeMarker2 = isMarker2;
            MarkerStepRequested?.Invoke(isMarker2, e.Key == Key.Right ? 1 : -1);
            e.Handled = true;
        }
    }

    private void RequestZoom(double factor, Point position)
    {
        var plot = GetPlotArea();
        if (!plot.Contains(position)) return;
        var baseRadius = Math.Min(plot.Width, plot.Height) * 0.5;
        if (baseRadius <= 0) return;
        var transform = new SmithChartTransform(plot.Width, plot.Height, Math.Clamp(Zoom, 1, 8), PanReal, PanImaginary);
        var reflection = transform.FromPixel(position.X - plot.Left, position.Y - plot.Top);
        var offsetX = (position.X - plot.Left - plot.Width / 2) / baseRadius;
        var offsetY = -(position.Y - plot.Top - plot.Height / 2) / baseRadius;
        ZoomRequested?.Invoke(factor, reflection, offsetX, offsetY);
    }

    private long? FindNearestFrequency(Point position, double maximumDistancePixels)
    {
        var plot = GetPlotArea();
        if (!plot.Contains(position) || Points.Count == 0) return null;
        var transform = new SmithChartTransform(plot.Width, plot.Height, Math.Clamp(Zoom, 1, 8), PanReal, PanImaginary);
        var maximumDistanceSquared = maximumDistancePixels * maximumDistancePixels;
        var bestDistanceSquared = maximumDistanceSquared;
        long? bestFrequency = null;
        var previousFrequency = _activeMarker2 == true ? Marker2Frequency : Marker1Frequency;

        for (var branch = 0; branch < 2; branch++)
        {
            var run = new List<(long FrequencyHz, Point Pixel)>();
            void EvaluateRun()
            {
                if (run.Count == 1) Consider(run[0].FrequencyHz, run[0].Pixel);
                for (var index = 1; index < run.Count; index++)
                {
                    var first = run[index - 1];
                    var second = run[index];
                    var dx = second.Pixel.X - first.Pixel.X;
                    var dy = second.Pixel.Y - first.Pixel.Y;
                    var lengthSquared = dx * dx + dy * dy;
                    var fraction = lengthSquared <= 1e-12 ? 0 : Math.Clamp(
                        ((position.X - first.Pixel.X) * dx + (position.Y - first.Pixel.Y) * dy) / lengthSquared, 0, 1);
                    var nearest = new Point(first.Pixel.X + fraction * dx, first.Pixel.Y + fraction * dy);
                    var frequency = fraction < 0.5 ? first.FrequencyHz : second.FrequencyHz;
                    Consider(frequency, nearest);
                }
                run.Clear();
            }

            foreach (var point in Points)
            {
                if (!IsInCurrentRange(point) || point.Quality != SampleQuality.Valid ||
                    !TryGetCandidates(point, out var primary, out var mirror) ||
                    !SmithChartMath.IsInsideOrOnUnitCircle(primary) ||
                    branch == 1 && (mirror is not { } alternate || !SmithChartMath.IsInsideOrOnUnitCircle(alternate)))
                {
                    EvaluateRun();
                    continue;
                }
                var reflection = branch == 0 ? primary : mirror!.Value;
                var pixel = transform.ToPixel(reflection);
                run.Add((point.FrequencyHz, new Point(plot.Left + pixel.X, plot.Top + pixel.Y)));
            }
            EvaluateRun();
        }

        return bestFrequency;

        void Consider(long frequencyHz, Point nearest)
        {
            var dx = position.X - nearest.X;
            var dy = position.Y - nearest.Y;
            var distanceSquared = dx * dx + dy * dy;
            if (distanceSquared > bestDistanceSquared + 0.25) return;
            var tied = Math.Abs(distanceSquared - bestDistanceSquared) <= 0.25;
            var isPrevious = previousFrequency == frequencyHz;
            var previousIsBest = previousFrequency == bestFrequency;
            if (tied && previousIsBest && !isPrevious) return;
            if (tied && isPrevious && !previousIsBest || distanceSquared < bestDistanceSquared - 0.25 ||
                bestFrequency is null || tied && !previousIsBest && !isPrevious && frequencyHz < bestFrequency)
            {
                bestDistanceSquared = distanceSquared;
                bestFrequency = frequencyHz;
            }
        }
    }

    private Rect GetPlotArea() => new(CircleMargin, CircleMargin,
        Math.Max(1, Bounds.Width - 2 * CircleMargin), Math.Max(1, Bounds.Height - 2 * CircleMargin));

    private void DrawData(DrawingContext context, SmithChartTransform transform, Rect plot)
    {
        var verified = new List<SmithComplex>();
        var positive = new List<SmithComplex>();
        var negative = new List<SmithComplex>();
        var runs = new List<(List<SmithComplex> Points, string Color, IDashStyle? Dash)>();

        void FlushAmbiguous()
        {
            if (positive.Count == 0) return;
            if (positive.Count >= 1)
            {
                runs.Add((positive, Palette.GetColor(PlotTrace.ReturnLoss), DashStyle.Dash));
                runs.Add((negative, Palette.GetColor(PlotTrace.ReturnLoss), DashStyle.Dot));
            }
            positive = [];
            negative = [];
        }

        void FlushVerified()
        {
            if (verified.Count == 0) return;
            if (verified.Count >= 1)
                runs.Add((verified, Palette.GetColor(PlotTrace.Phase), Palette.IsNightVision ? DashStyle.Dash : null));
            verified = [];
        }

        foreach (var point in Points)
        {
            if (!IsInCurrentRange(point))
            {
                FlushVerified();
                FlushAmbiguous();
                continue;
            }
            if (point.Quality != SampleQuality.Valid || !TryGetCandidates(point, out var first, out var second) ||
                !SmithChartMath.IsInsideOrOnUnitCircle(first) ||
                second is { } mirrored && !SmithChartMath.IsInsideOrOnUnitCircle(mirrored))
            {
                FlushVerified();
                FlushAmbiguous();
                continue;
            }

            if (second is { } alternate)
            {
                FlushVerified();
                positive.Add(first);
                negative.Add(alternate);
            }
            else
            {
                FlushAmbiguous();
                verified.Add(first);
            }
        }
        FlushVerified();
        FlushAmbiguous();

        foreach (var run in runs)
        {
            var pen = new Pen(new SolidColorBrush(Color.Parse(run.Color)), 1.8, run.Dash);
            var firstPixel = transform.ToPixel(run.Points[0]);
            var degenerate = run.Points.Count == 1 || run.Points.Skip(1).All(point =>
            {
                var pixel = transform.ToPixel(point);
                var dx = pixel.X - firstPixel.X;
                var dy = pixel.Y - firstPixel.Y;
                return dx * dx + dy * dy <= 0.25;
            });
            if (degenerate)
            {
                context.DrawEllipse(null, pen, new Point(plot.Left + firstPixel.X, plot.Top + firstPixel.Y), 2, 2);
            }
            else
            {
                DrawCurve(context, run.Points, transform, plot, pen, close: false);
            }
        }
    }

    private void DrawComparisonData(DrawingContext context, SmithChartTransform transform, Rect plot)
    {
        foreach (var overlay in ComparisonOverlays)
        {
            var verified = new List<SmithComplex>();
            var positive = new List<SmithComplex>();
            var negative = new List<SmithComplex>();

            void FlushVerified()
            {
                DrawRun(verified, overlay.Color, DashStyle.Dash);
                verified.Clear();
            }

            void FlushAmbiguous()
            {
                DrawRun(positive, overlay.Color, DashStyle.Dash);
                DrawRun(negative, overlay.Color, DashStyle.Dot);
                positive.Clear();
                negative.Clear();
            }

            foreach (var point in overlay.Points)
            {
                if (!IsInCurrentRange(point) || point.Quality != SampleQuality.Valid ||
                    !TryGetCandidates(point, overlay.ReferenceImpedanceOhms, out var first, out var second) ||
                    !SmithChartMath.IsInsideOrOnUnitCircle(first) ||
                    second is { } mirrored && !SmithChartMath.IsInsideOrOnUnitCircle(mirrored))
                {
                    FlushVerified();
                    FlushAmbiguous();
                    continue;
                }

                if (second is { } alternate)
                {
                    FlushVerified();
                    positive.Add(first);
                    negative.Add(alternate);
                }
                else
                {
                    FlushAmbiguous();
                    verified.Add(first);
                }
            }

            FlushVerified();
            FlushAmbiguous();

            void DrawRun(IReadOnlyList<SmithComplex> points, string color, IDashStyle? dash)
            {
                if (points.Count == 0) return;
                var pen = new Pen(new SolidColorBrush(Color.Parse(color)), 1.35, dash);
                var firstPixel = transform.ToPixel(points[0]);
                var degenerate = points.Count == 1 || points.Skip(1).All(point =>
                {
                    var pixel = transform.ToPixel(point);
                    var dx = pixel.X - firstPixel.X;
                    var dy = pixel.Y - firstPixel.Y;
                    return dx * dx + dy * dy <= 0.25;
                });
                if (degenerate)
                    context.DrawEllipse(null, pen, new Point(plot.Left + firstPixel.X, plot.Top + firstPixel.Y), 1.8, 1.8);
                else
                    DrawCurve(context, points, transform, plot, pen, close: false);
            }
        }
    }

    private void DrawSwrCircle(DrawingContext context, SmithChartTransform transform, Rect plot,
        double reflectionMagnitude)
    {
        var centerPixel = transform.ToPixel(SmithChartMath.MatchedReflection);
        var radiusPixels = reflectionMagnitude * transform.RadiusPixels;
        if (!double.IsFinite(radiusPixels) || radiusPixels < 0) return;

        var pen = new Pen(new SolidColorBrush(Color.Parse(Palette.GetColor(PlotTrace.Swr))), 1.2, DashStyle.Dash);
        context.DrawEllipse(null, pen,
            new Rect(plot.Left + centerPixel.X - radiusPixels, plot.Top + centerPixel.Y - radiusPixels,
                radiusPixels * 2, radiusPixels * 2));
    }

    private void DrawSwrCircleLabel(DrawingContext context, Rect plot, double swr)
    {
        DrawText(context, Text.Format("SmithSwrCircleValue", swr.ToString("G3", LocalizationService.Instance.Culture)),
            plot.Right - 82, plot.Top + 4, new SolidColorBrush(Color.Parse(Palette.AxisText)), 8);
    }

    private bool TryGetSwrCircle(out double swr, out double reflectionMagnitude)
    {
        swr = 0;
        reflectionMagnitude = 0;
        switch (SwrCircleMode)
        {
            case SmithSwrCircleMode.Hidden:
                return false;
            case SmithSwrCircleMode.Fixed:
                if (FixedSwr is not { } fixedSwr || !double.IsFinite(fixedSwr) || fixedSwr < 1)
                    return false;
                swr = fixedSwr;
                reflectionMagnitude = (fixedSwr - 1) / (fixedSwr + 1);
                return double.IsFinite(reflectionMagnitude) && reflectionMagnitude is >= 0 and <= 1;
            case SmithSwrCircleMode.Marker1:
                if (!ShowMarker1 || Marker1Frequency is not { } frequency) return false;
                var point = Points.FirstOrDefault(item => item.FrequencyHz == frequency &&
                    item.Quality == SampleQuality.Valid && IsInCurrentRange(item));
                if (point is null || !TryGetCandidates(point, out var reflection, out _) ||
                    !SmithChartMath.IsInsideOrOnUnitCircle(reflection)) return false;
                reflectionMagnitude = reflection.Magnitude;
                if (!double.IsFinite(reflectionMagnitude) || reflectionMagnitude >= 1) return false;
                swr = (1 + reflectionMagnitude) / (1 - reflectionMagnitude);
                return double.IsFinite(swr) && swr >= 1;
            default:
                return false;
        }
    }

    private bool TryGetCandidates(MeasurementPoint point, out SmithComplex primary, out SmithComplex? mirror)
        => TryGetCandidates(point, ReferenceImpedanceOhms, out primary, out mirror);

    private static bool TryGetCandidates(MeasurementPoint point, double? referenceImpedanceOhms,
        out SmithComplex primary, out SmithComplex? mirror)
    {
        primary = default;
        mirror = null;
        var z0 = referenceImpedanceOhms is { } reference && double.IsFinite(reference) && reference > 0
            ? reference : (double?)null;

        if (point.PhaseSignConfidence == PhaseSignConfidence.Verified)
        {
            if (point.ResistanceOhms is { } resistance && point.ReactanceOhms is { } reactance &&
                z0 is { } verifiedZ0 && SmithChartMath.TryGetReflectionFromImpedance(resistance, reactance, verifiedZ0, out primary))
                return true;
            return point.ReturnLossDb is { } rl && point.PhaseDegrees is { } phase &&
                   SmithChartMath.TryGetReflectionFromSignedS11(rl, phase, out primary);
        }

        if (point.ReturnLossDb is { } signedS11 && point.PhaseDegrees is { } measuredPhase &&
            SmithChartMath.TryGetReflectionFromSignedS11(signedS11, measuredPhase, out primary))
        {
            mirror = primary.Conjugate;
            return true;
        }

        if (point.ResistanceOhms is not { } r || point.ReactanceOhms is not { } x) return false;
        if (point.PhaseSignConfidence is PhaseSignConfidence.Ambiguous or PhaseSignConfidence.Missing)
        {
            if (z0 is not { } ambiguousZ0 ||
                !SmithChartMath.TryGetReflectionFromImpedance(r, Math.Abs(x), ambiguousZ0, out primary)) return false;
            mirror = primary.Conjugate;
            return true;
        }

        return false;
    }

    private static void DrawCurve(DrawingContext context, IReadOnlyList<SmithComplex> points,
        SmithChartTransform transform, Rect plot, Pen pen, bool close)
    {
        if (points.Count < 2) return;
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            var first = transform.ToPixel(points[0]);
            path.BeginFigure(new Point(plot.Left + first.X, plot.Top + first.Y), false);
            for (var index = 1; index < points.Count; index++)
            {
                var position = transform.ToPixel(points[index]);
                path.LineTo(new Point(plot.Left + position.X, plot.Top + position.Y));
            }
            path.EndFigure(close);
        }
        context.DrawGeometry(null, pen, geometry);
    }

    private void DrawMarkers(DrawingContext context, SmithChartTransform transform, Rect plot)
    {
        DrawMarker(Marker1Frequency, ShowMarker1, Palette.Marker1, null);
        DrawMarker(Marker2Frequency, ShowMarker2, Palette.Marker2, Palette.IsNightVision ? DashStyle.Dash : null);

        void DrawMarker(long? frequency, bool visible, string color, IDashStyle? dash)
        {
            if (!visible || frequency is null) return;
            var point = Points.FirstOrDefault(item => item.FrequencyHz == frequency.Value &&
                item.Quality == SampleQuality.Valid && IsInCurrentRange(item));
            if (point is null || !TryGetCandidates(point, out var reflection, out var mirror)) return;
            DrawCandidate(reflection, square: false);
            if (mirror is { } alternate) DrawCandidate(alternate, square: true);

            void DrawCandidate(SmithComplex candidate, bool square)
            {
                if (!SmithChartMath.IsInsideOrOnUnitCircle(candidate)) return;
                var pixel = transform.ToPixel(candidate);
                var center = new Point(plot.Left + pixel.X, plot.Top + pixel.Y);
                var markerPen = new Pen(new SolidColorBrush(Color.Parse(color)), 1.4, dash);
                if (square)
                    context.DrawRectangle(null, markerPen, new Rect(center.X - 4, center.Y - 4, 8, 8));
                else
                    context.DrawEllipse(new SolidColorBrush(Color.Parse(color)),
                        new Pen(new SolidColorBrush(Color.Parse(Palette.Background)), 1), center, 4, 4);
                context.DrawLine(markerPen, new Point(center.X - 7, center.Y), new Point(center.X + 7, center.Y));
            }
        }
    }

    private MeasurementPoint? FindNearestSample(Point position)
    {
        var plot = GetPlotArea();
        if (!plot.Contains(position) || Points.Count == 0) return null;
        var transform = new SmithChartTransform(plot.Width, plot.Height, Math.Clamp(Zoom, 1, 8), PanReal, PanImaginary);
        var bestDistanceSquared = 14d * 14d;
        MeasurementPoint? bestPoint = null;

        foreach (var point in Points)
        {
            if (!IsInCurrentRange(point) || point.Quality != SampleQuality.Valid ||
                !TryGetCandidates(point, out var primary, out var mirror) ||
                !SmithChartMath.IsInsideOrOnUnitCircle(primary) ||
                mirror is { } alternate && !SmithChartMath.IsInsideOrOnUnitCircle(alternate))
                continue;

            Consider(primary);
            if (mirror is { } second) Consider(second);

            void Consider(SmithComplex reflection)
            {
                var pixel = transform.ToPixel(reflection);
                var dx = position.X - (plot.Left + pixel.X);
                var dy = position.Y - (plot.Top + pixel.Y);
                var distanceSquared = dx * dx + dy * dy;
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestPoint = point;
                }
            }
        }

        return bestPoint;
    }

    private void DrawHoverReadout(DrawingContext context, Point pointer, MeasurementPoint point)
    {
        var culture = LocalizationService.Instance.Culture;
        var lines = new List<string>
        {
            Text.Format("SmithHoverFrequency", (point.FrequencyHz / 1_000_000d).ToString("0.######", culture)),
        };

        if (TryGetHoverImpedance(point, out var impedance, out var alternate))
        {
            if (alternate is { } second)
            {
                var magnitude = Math.Abs(impedance.Imaginary);
                lines.Add(Text.Format("SmithHoverImpedancePositive",
                    impedance.Real.ToString("0.###", culture), magnitude.ToString("0.###", culture)));
                lines.Add(Text.Format("SmithHoverImpedanceNegative",
                    second.Real.ToString("0.###", culture), Math.Abs(second.Imaginary).ToString("0.###", culture)));
            }
            else
            {
                var sign = impedance.Imaginary < 0 ? "−" : "+";
                lines.Add(Text.Format("SmithHoverImpedance", impedance.Real.ToString("0.###", culture), sign,
                    Math.Abs(impedance.Imaginary).ToString("0.###", culture)));
            }
        }

        if (ReferenceImpedanceOhms is { } z0 && double.IsFinite(z0) && z0 > 0)
            lines.Add(Text.Format("SmithHoverReference", z0.ToString("0.##", culture)));
        else
            lines.Add(Text.Get("SmithHoverReferenceUnknown"));

        var confidence = point.PhaseSignConfidence switch
        {
            PhaseSignConfidence.Verified => Text.Get("SmithSignVerified"),
            PhaseSignConfidence.Ambiguous => Text.Get("SmithSignAmbiguous"),
            _ => Text.Get("SmithSignMissing"),
        };
        lines.Add(Text.Format("SmithHoverConfidence", confidence));

        var width = Math.Max(0, Math.Min(260, Bounds.Width - 8));
        var height = lines.Count * 12 + 8;
        var x = pointer.X + 14;
        if (x + width > Bounds.Width - 4) x = pointer.X - width - 14;
        x = Math.Clamp(x, 4, Math.Max(4, Bounds.Width - width - 4));
        var y = pointer.Y + 14;
        if (y + height > Bounds.Height - 4) y = pointer.Y - height - 14;
        y = Math.Clamp(y, 4, Math.Max(4, Bounds.Height - height - 4));

        var rectangle = new Rect(x, y, width, height);
        context.DrawRectangle(new SolidColorBrush(Color.Parse(Palette.Background)),
            new Pen(new SolidColorBrush(Color.Parse(Palette.Border)), 1), rectangle);
        var brush = new SolidColorBrush(Color.Parse(Palette.AxisTitle));
        for (var index = 0; index < lines.Count; index++)
            DrawText(context, lines[index], x + 5, y + 4 + index * 12, brush, 8);
    }

    private bool TryGetHoverImpedance(MeasurementPoint point, out SmithComplex impedance,
        out SmithComplex? alternate)
    {
        impedance = default;
        alternate = null;
        if (point.PhaseSignConfidence == PhaseSignConfidence.Missing) return false;

        if (point.ResistanceOhms is { } resistance && point.ReactanceOhms is { } reactance &&
            double.IsFinite(resistance) && double.IsFinite(reactance))
        {
            var imaginary = point.PhaseSignConfidence == PhaseSignConfidence.Ambiguous
                ? Math.Abs(reactance) : reactance;
            impedance = new SmithComplex(resistance, imaginary);
            if (point.PhaseSignConfidence == PhaseSignConfidence.Ambiguous)
                alternate = impedance.Conjugate;
            return true;
        }

        if (ReferenceImpedanceOhms is not { } z0 || !double.IsFinite(z0) || z0 <= 0 ||
            !TryGetCandidates(point, out var reflection, out var mirrored) ||
            !SmithChartMath.TryGetImpedanceFromReflection(reflection, z0, out impedance))
            return false;

        if (point.PhaseSignConfidence == PhaseSignConfidence.Ambiguous)
        {
            if (mirrored is { } mirror && SmithChartMath.TryGetImpedanceFromReflection(mirror, z0, out var second))
                alternate = second;
            else
                alternate = impedance.Conjugate;
        }
        return true;
    }

    private void DrawLabels(DrawingContext context, Rect circle, Rect plot, SmithChartTransform transform)
    {
        var labelBrush = new SolidColorBrush(Color.Parse(Palette.AxisText));
        DrawText(context, "Γ=−1", circle.Left - 8, circle.Bottom + 3, labelBrush, 8);
        DrawText(context, "Γ=1", circle.Right - 18, circle.Bottom + 3, labelBrush, 8);
        DrawText(context, "Γ=0 · " + Text.Get("SmithMatched"), circle.Left + circle.Width / 2 - 23,
            circle.Top + circle.Height / 2 + 5, labelBrush, 7);
        DrawText(context, Text.Get("SmithChartTitle"), 6, 4, new SolidColorBrush(Color.Parse(Palette.AxisTitle)), 9);

        if (Points.Count == 0 && ComparisonOverlays.Count > 0)
            DrawText(context, Text.Get("SmithComparisonReferencePerTrace"), 6, 17, labelBrush, 8);
        else if (ReferenceImpedanceOhms is { } z0 && double.IsFinite(z0) && z0 > 0)
            DrawText(context, Text.Format("SmithReference", z0.ToString("0.##", LocalizationService.Instance.Culture)),
                6, 17, labelBrush, 8);
        else
            DrawText(context, Text.Get("SmithReferenceUnknown"), 6, 17, labelBrush, 8);

        var usable = 0;
        var outside = 0;
        foreach (var point in Points)
        {
            if (!IsInCurrentRange(point)) continue;
            if (point.Quality != SampleQuality.Valid) continue;
            if (!TryGetCandidates(point, out var reflection, out var mirror)) continue;
            if (!SmithChartMath.IsInsideOrOnUnitCircle(reflection) ||
                mirror is { } alternate && !SmithChartMath.IsInsideOrOnUnitCircle(alternate))
            {
                outside++;
                continue;
            }
            usable++;
        }
        if (usable == 0)
            DrawText(context, Text.Get("SmithNoComplexData"), plot.Left, plot.Bottom - 15, labelBrush, 8);
        if (outside > 0)
            DrawText(context, Text.Format("SmithOutsideCount", outside), plot.Left + 4, plot.Top + 4,
                new SolidColorBrush(Color.Parse(Palette.AxisTitle)), 8);

        var ambiguous = Points.Any(point => IsInCurrentRange(point) &&
            point.PhaseSignConfidence != PhaseSignConfidence.Verified &&
            point.Quality == SampleQuality.Valid && (point.PhaseDegrees is not null || point.ReactanceOhms is not null));
        if (ambiguous)
            DrawText(context, Text.Get("SmithPhaseAmbiguous"), plot.Left + 4, plot.Bottom - 3,
                labelBrush, 8);
    }

    private bool IsInCurrentRange(MeasurementPoint point) =>
        (Points.Count == 0 && ComparisonOverlays.Count > 0) ||
        ViewStopHz <= ViewStartHz || point.FrequencyHz >= ViewStartHz && point.FrequencyHz <= ViewStopHz;

    private static void DrawText(DrawingContext context, string text, double x, double y, IBrush brush, double size)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal), size, brush);
        context.DrawText(formatted, new Point(x, y));
    }
}
