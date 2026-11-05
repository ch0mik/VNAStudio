using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class CableLengthTests
{
    [Theory]
    [InlineData(new[] { 1d, 4d, 1d, 4d, 1d, 4d, 1d }, 2, 4, CableResonancePairIssue.None)]
    [InlineData(new[] { 4d, 1d, 4d, 1d, 4d, 1d, 4d }, 2, 4, CableResonancePairIssue.None)]
    [InlineData(new[] { 1d, 4d, 1d, 0.5d, 4d, 1d, 4d }, 2, 4, CableResonancePairIssue.DifferentKinds)]
    [InlineData(new[] { 1d, 4d, 1d, 4d, 1d, 4d, 1d, 4d, 1d }, 2, 6, CableResonancePairIssue.NotAdjacent)]
    [InlineData(new[] { 2d, 2d, 2d, 2d, 2d, 2d, 2d }, 2, 4, CableResonancePairIssue.NotAtResonance)]
    public void ValidatesAdjacentSameKindResonances(double[] swr, int firstIndex, int secondIndex,
        CableResonancePairIssue expectedIssue)
    {
        var points = swr.Select((value, index) => TestData.Point((index + 1) * 1_000_000L, value)).ToArray();

        var result = CableResonancePairValidator.Validate(
            points, points[firstIndex].FrequencyHz, points[secondIndex].FrequencyHz);

        Assert.Equal(expectedIssue, result.Issue);
        Assert.Equal(expectedIssue == CableResonancePairIssue.None, result.IsValid);
    }

    [Fact]
    public void InvalidSampleBreaksResonanceSearchInsteadOfConnectingAcrossGap()
    {
        var points = new[]
        {
            TestData.Point(1_000_000, 1), TestData.Point(2_000_000, 4), TestData.Point(3_000_000, 1),
            TestData.Point(4_000_000, 4, SampleQuality.Invalid),
            TestData.Point(5_000_000, 1), TestData.Point(6_000_000, 4), TestData.Point(7_000_000, 1),
        };

        var result = CableResonancePairValidator.Validate(points, 2_000_000, 6_000_000);

        Assert.Equal(CableResonancePairIssue.DataGapBetweenMarkers, result.Issue);
    }

    [Fact]
    public void RequiresTwoDistinctMarkersThatExistInTheMeasurement()
    {
        var points = Enumerable.Range(0, 7)
            .Select(index => TestData.Point((index + 1) * 1_000_000L, index % 2 == 0 ? 1 : 4)).ToArray();

        Assert.Equal(CableResonancePairIssue.SameMarker,
            CableResonancePairValidator.Validate(points, 2_000_000, 2_000_000).Issue);
        Assert.Equal(CableResonancePairIssue.MarkerNotFound,
            CableResonancePairValidator.Validate(points, 2_000_000, 8_000_000).Issue);
        Assert.Equal(CableResonancePairIssue.MissingMarkers,
            CableResonancePairValidator.Validate(points, null, 4_000_000).Issue);
    }

    [Fact]
    public void RejectsTwoMarkersThatResolveToTheSameResonance()
    {
        var points = new[] { 1d, 2d, 3d, 3d, 2d, 1d, 2d }
            .Select((value, index) => TestData.Point((index + 1) * 1_000_000L, value)).ToArray();

        var result = CableResonancePairValidator.Validate(points, 3_000_000, 4_000_000);

        Assert.Equal(CableResonancePairIssue.SameResonance, result.Issue);
    }

    [Theory]
    [InlineData(1_000_000L, 2_000_000L, 0.66)]
    [InlineData(2_000_000L, 1_000_000L, 0.66)]
    public void CalculatesLengthFromAdjacentSameTypeResonances(long firstHz, long secondHz, double velocityFactor)
    {
        var expectedMeters = velocityFactor * CableLengthCalculator.SpeedOfLightMetersPerSecond / 2_000_000;

        Assert.Equal(expectedMeters, CableLengthCalculator.CalculateMeters(firstHz, secondHz, velocityFactor), 9);
    }

    [Theory]
    [InlineData(0, 1_000_000, 0.66)]
    [InlineData(1_000_000, 1_000_000, 0.66)]
    [InlineData(1_000_000, 2_000_000, 0)]
    [InlineData(1_000_000, 2_000_000, 1.01)]
    public void RejectsInvalidResonancesOrVelocityFactor(long firstHz, long secondHz, double velocityFactor) =>
        Assert.ThrowsAny<ArgumentException>(() => CableLengthCalculator.CalculateMeters(firstHz, secondHz, velocityFactor));
}
