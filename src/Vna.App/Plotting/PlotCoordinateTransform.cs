using Vna.Localization;
namespace Vna.App.Plotting;

/// <summary>Maps RF values to chart coordinates without depending on Avalonia.</summary>
public sealed class PlotCoordinateTransform
{
    private readonly long _startFrequencyHz;
    private readonly long _stopFrequencyHz;
    private readonly double _frequencySpanHz;
    private readonly double _left;
    private readonly double _top;
    private readonly double _width;
    private readonly double _height;

    public PlotCoordinateTransform(
        long startFrequencyHz,
        long stopFrequencyHz,
        double left,
        double top,
        double width,
        double height)
    {
        if (startFrequencyHz < 0)
            throw new ArgumentOutOfRangeException(nameof(startFrequencyHz));
        if (stopFrequencyHz < startFrequencyHz)
            throw new ArgumentOutOfRangeException(nameof(stopFrequencyHz));
        if (!double.IsFinite(left) || !double.IsFinite(top) ||
            !double.IsFinite(width) || width <= 0 || !double.IsFinite(height) || height <= 0)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(width), "PlotAreaInvalid");

        _startFrequencyHz = startFrequencyHz;
        _stopFrequencyHz = stopFrequencyHz;
        _frequencySpanHz = (double)stopFrequencyHz - startFrequencyHz;
        _left = left;
        _top = top;
        _width = width;
        _height = height;
    }

    public double FrequencyToX(long frequencyHz) => _frequencySpanHz <= 0
        ? _left + _width / 2
        : _left + (frequencyHz - (double)_startFrequencyHz) / _frequencySpanHz * _width;

    public bool ContainsFrequency(long frequencyHz) =>
        frequencyHz >= _startFrequencyHz && frequencyHz <= _stopFrequencyHz;

    public long XToFrequency(double x)
    {
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (_frequencySpanHz <= 0) return _startFrequencyHz;
        var fraction = Math.Clamp((x - _left) / _width, 0, 1);
        if (fraction <= 0) return _startFrequencyHz;
        if (fraction >= 1) return _stopFrequencyHz;
        return _startFrequencyHz + (long)Math.Round(
            _frequencySpanHz * fraction, MidpointRounding.AwayFromZero);
    }

    public double ValueToY(double value, PlotAxisScale axis)
    {
        ArgumentNullException.ThrowIfNull(axis);
        return _top + axis.FractionFromTop(value) * _height;
    }
}
