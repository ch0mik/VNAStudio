namespace Vna.Core;

public static class MeasurementPointQueries
{
    public static MeasurementPoint? FindNearest(
        IReadOnlyList<MeasurementPoint> points,
        long frequencyHz,
        long minimumFrequencyHz = 0,
        long maximumFrequencyHz = long.MaxValue)
    {
        MeasurementPoint? nearest = null;
        var nearestDistance = double.PositiveInfinity;
        foreach (var point in points)
        {
            if (point.FrequencyHz < minimumFrequencyHz || point.FrequencyHz > maximumFrequencyHz) continue;
            var distance = Math.Abs((double)point.FrequencyHz - frequencyHz);
            if (distance < nearestDistance ||
                (distance == nearestDistance && (nearest is null || point.FrequencyHz < nearest.FrequencyHz)))
            {
                nearest = point;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    public static MeasurementPoint? FindMinimumSwr(IReadOnlyList<MeasurementPoint> points)
    {
        MeasurementPoint? minimum = null;
        foreach (var point in points)
        {
            if (point.Quality != SampleQuality.Valid || point.Swr is not { } swr || !double.IsFinite(swr) || swr < 1)
                continue;
            if (minimum is null || swr < minimum.Swr!.Value) minimum = point;
        }
        return minimum;
    }
}
