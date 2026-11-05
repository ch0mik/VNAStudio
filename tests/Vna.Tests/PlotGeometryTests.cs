using Vna.App.Plotting;
using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class PlotGeometryTests
{
    [Theory]
    [InlineData(200, 100)]
    [InlineData(800, 400)]
    [InlineData(1600, 900)]
    public void CoordinatesFollowCurrentSize(double width, double height)
    {
        var transform = new PlotCoordinateTransform(1_000_000, 65_000_000, 68, 24, width, height);
        Assert.Equal(68, transform.FrequencyToX(1_000_000));
        Assert.Equal(68 + width, transform.FrequencyToX(65_000_000));
        Assert.Equal(68 + width / 2, transform.FrequencyToX(33_000_000));
        Assert.Equal(33_000_000, transform.XToFrequency(68 + width / 2));
        Assert.Equal(24, transform.ValueToY(10, PlotSeriesCatalog.Swr));
        Assert.Equal(24 + height, transform.ValueToY(1, PlotSeriesCatalog.Swr));
        Assert.Equal(24, transform.ValueToY(0, PlotSeriesCatalog.Phase));
        Assert.Equal(24 + height, transform.ValueToY(180, PlotSeriesCatalog.Phase));
    }

    [Fact]
    public void TickLabelsAndCurveUseSameScale()
    {
        foreach (var series in PlotSeriesCatalog.All)
        {
            var transform = new PlotCoordinateTransform(1, 2, 0, 20, 200, 400);
            for (var i = 0; i <= 5; i++)
            {
                var fraction = i / 5d;
                var tickValue = series.Axis.ValueAtFractionFromTop(fraction);
                Assert.Equal(20 + 400 * fraction, transform.ValueToY(tickValue, series.Axis), precision: 8);
            }
        }
    }

    [Fact]
    public void ScreenshotAxisTicksUseConfirmedValuesAndMatchCurvePositions()
    {
        Assert.Equal(new[] { 0d, 45d, 90d, 135d, 180d }, PlotSeriesCatalog.Phase.Ticks.Select(tick => tick.Value));
        Assert.Equal(new[] { 0d, 75d, 150d, 225d, 300d }, PlotSeriesCatalog.Ohms.Ticks.Select(tick => tick.Value));
        Assert.Equal(new[] { 10d, 7.5, 5, 2.5, 1 }, PlotSeriesCatalog.Swr.Ticks.Select(tick => tick.Value));
        Assert.Equal(new[] { "10.0", "7.5", "5.0", "2.5", "1.0" }, PlotSeriesCatalog.Swr.Ticks.Select(tick => tick.Label));
        Assert.Equal(new[] { "0", "10", "20", "30" }, PlotSeriesCatalog.ReturnLoss.Ticks.Select(tick => tick.Label));
        Assert.Equal(new[] { "0", "20", "40", "60" }, PlotSeriesCatalog.ReturnLossX2.Ticks.Select(tick => tick.Label));

        var transform = new PlotCoordinateTransform(1, 2, 0, 20, 200, 400);
        Assert.Equal(120, transform.ValueToY(45, PlotSeriesCatalog.Phase));
        Assert.Equal(120, transform.ValueToY(75, PlotSeriesCatalog.Ohms));
        Assert.Equal(20 + 400d / 3, transform.ValueToY(-10, PlotSeriesCatalog.ReturnLoss), precision: 8);
        Assert.Equal(20 + 400d / 3, transform.ValueToY(-20, PlotSeriesCatalog.ReturnLossX2), precision: 8);
    }

    [Fact]
    public void SingleFrequencyIsCenteredAndDoesNotDivideByZero()
    {
        var transform = new PlotCoordinateTransform(42, 42, 10, 20, 200, 100);
        Assert.Equal(110, transform.FrequencyToX(42));
        Assert.Equal(42, transform.XToFrequency(999));
        Assert.False(transform.ContainsFrequency(43));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UnavailableQuantityBreaksTheCurve(double? gap)
    {
        var points = new[] { TestData.Point(1), TestData.Point(2), TestData.Point(3, gap), TestData.Point(4), TestData.Point(5) };
        var segments = PlotSeriesGeometry.GetSegments(points, PlotSeriesCatalog.All[0],
            new PlotCoordinateTransform(1, 5, 0, 0, 400, 100)).ToArray();
        Assert.Equal(2, segments.Length);
        Assert.Equal(100, segments[0].To.X);
        Assert.Equal(300, segments[1].From.X);
    }

    [Theory]
    [InlineData(SampleQuality.Invalid)]
    [InlineData(SampleQuality.Missing)]
    public void InvalidSampleBreaksEveryCurve(SampleQuality quality)
    {
        var points = new[] { TestData.Point(1), TestData.Point(2, quality: quality), TestData.Point(3) };
        foreach (var series in PlotSeriesCatalog.All)
            Assert.Empty(PlotSeriesGeometry.GetSegments(points, series, new PlotCoordinateTransform(1, 3, 0, 0, 200, 100)));
    }

    [Fact]
    public void CurveRunsPreserveMeasurementGapsForContinuousDashRendering()
    {
        var points = new[] { TestData.Point(1), TestData.Point(2), TestData.Point(3, null), TestData.Point(4), TestData.Point(5) };
        var runs = PlotSeriesGeometry.GetRuns(points, PlotSeriesCatalog.All[0],
            new PlotCoordinateTransform(1, 5, 0, 0, 400, 100)).ToArray();
        Assert.Equal(2, runs.Length);
        Assert.All(runs, run => Assert.Equal(2, run.Count));
        Assert.Equal(100, runs[0][1].X);
        Assert.Equal(300, runs[1][0].X);
    }

    [Fact]
    public void MarkerNeverSnapsOutsideZoomedRange()
    {
        var points = new[] { TestData.Point(100), TestData.Point(200), TestData.Point(300) };
        Assert.Equal(200, MeasurementPointQueries.FindNearest(points, 149, 150, 250)!.FrequencyHz);
        Assert.Null(MeasurementPointQueries.FindNearest(points, 150, 140, 160));
        Assert.Equal(100, MeasurementPointQueries.FindNearest(points, 150)!.FrequencyHz);
    }

    [Fact]
    public void MinimumIgnoresNonfiniteInvalidAndUnphysicalSwr()
    {
        var points = new[]
        {
            TestData.Point(1, double.NaN), TestData.Point(2, 0.5), TestData.Point(3, 1, SampleQuality.Invalid),
            TestData.Point(4, double.PositiveInfinity), TestData.Point(5, null),
        };
        Assert.Null(MeasurementPointQueries.FindMinimumSwr(points));
        Assert.Equal(6, MeasurementPointQueries.FindMinimumSwr(points.Append(TestData.Point(6, 1.5)).ToArray())!.FrequencyHz);
    }
}
