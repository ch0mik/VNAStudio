using Vna.Core;

namespace Vna.App.Plotting;

public readonly record struct PlotPosition(double X, double Y);
public readonly record struct PlotLineSegment(PlotPosition From, PlotPosition To);

public static class PlotSeriesGeometry
{
    public static IEnumerable<PlotLineSegment> GetSegments(
        IReadOnlyList<MeasurementPoint> points,
        PlotSeriesDefinition series,
        PlotCoordinateTransform transform)
    {
        foreach (var run in GetRuns(points, series, transform))
            for (var index = 1; index < run.Count; index++)
                yield return new PlotLineSegment(run[index - 1], run[index]);
    }

    public static IEnumerable<IReadOnlyList<PlotPosition>> GetRuns(
        IReadOnlyList<MeasurementPoint> points,
        PlotSeriesDefinition series,
        PlotCoordinateTransform transform)
    {
        var run = new List<PlotPosition>();
        foreach (var sample in points)
        {
            if (!transform.ContainsFrequency(sample.FrequencyHz) || !series.TryGetValue(sample, out var value))
            {
                if (run.Count > 0)
                {
                    yield return run;
                    run = new List<PlotPosition>();
                }
                continue;
            }

            run.Add(new PlotPosition(
                transform.FrequencyToX(sample.FrequencyHz),
                transform.ValueToY(value, series.Axis)));
        }
        if (run.Count > 0) yield return run;
    }
}
