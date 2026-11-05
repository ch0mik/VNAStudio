using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class CableLossCalculatorTests
{
    [Theory]
    [InlineData(-20, 10)]
    [InlineData(-0.4, 0.2)]
    [InlineData(0, 0)]
    public void ConvertsSignedReturnLossToOneWayEstimate(double returnLossDb, double expectedLossDb)
    {
        Assert.Equal(expectedLossDb, CableLossCalculator.CalculateOneWayDb(returnLossDb));
    }

    [Fact]
    public void RejectsValuesOutsideSignedReturnLossDomain()
    {
        Assert.Null(CableLossCalculator.CalculateOneWayDb(null));
        Assert.Null(CableLossCalculator.CalculateOneWayDb(double.NaN));
        Assert.Null(CableLossCalculator.CalculateOneWayDb(double.PositiveInfinity));
        Assert.Null(CableLossCalculator.CalculateOneWayDb(0.1));
    }
}
