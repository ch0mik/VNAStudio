using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class ResonanceQTests
{
    [Fact]
    public void ReproducesManualParallelResonanceExample()
    {
        var q = ResonanceQCalculator.Calculate(14_400_000, 14_312_000, 14_481_600);
        Assert.Equal(84.9, q, 1);
    }

    [Theory]
    [InlineData(10_000_000L, 10_000_000L, 10_000_000L)]
    [InlineData(10_000_000L, 11_000_000L, 12_000_000L)]
    [InlineData(10_000_000L, 8_000_000L, 7_000_000L)]
    public void RejectsInvalidResonanceOrBandwidthEdges(long resonanceHz, long lowHz, long highHz) =>
        Assert.ThrowsAny<ArgumentException>(() => ResonanceQCalculator.Calculate(resonanceHz, lowHz, highHz));
}
