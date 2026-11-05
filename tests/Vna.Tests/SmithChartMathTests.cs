using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class SmithChartMathTests
{
    [Theory]
    [InlineData(50, 0, 50, 0, 0)]
    [InlineData(0, 0, 50, -1, 0)]
    [InlineData(100, 0, 50, 1d / 3, 0)]
    [InlineData(25, 0, 50, -1d / 3, 0)]
    [InlineData(50, 50, 50, 0.2, 0.4)]
    [InlineData(50, -50, 50, 0.2, -0.4)]
    [InlineData(150, 0, 75, 1d / 3, 0)]
    [InlineData(37.5, 0, 75, -1d / 3, 0)]
    public void ImpedanceVectorsMapToKnownReflection(double resistance, double reactance, double z0,
        double expectedReal, double expectedImaginary)
    {
        Assert.True(SmithChartMath.TryGetReflectionFromImpedance(resistance, reactance, z0, out var gamma));
        Assert.Equal(expectedReal, gamma.Real, 12);
        Assert.Equal(expectedImaginary, gamma.Imaginary, 12);
        Assert.True(SmithChartMath.TryGetImpedanceFromReflection(gamma, z0, out var impedance));
        Assert.Equal(resistance, impedance.Real, 10);
        Assert.Equal(reactance, impedance.Imaginary, 10);
    }

    [Theory]
    [InlineData(-6.989700043360188, 63.43494882292201, 0.2, 0.4)]
    [InlineData(-6.989700043360188, -63.43494882292201, 0.2, -0.4)]
    public void SignedReturnLossAndPhaseProduceComplexReflection(double db, double phase, double real, double imaginary)
    {
        Assert.True(SmithChartMath.TryGetReflectionFromSignedS11(db, phase, out var gamma));
        Assert.Equal(real, gamma.Real, 12);
        Assert.Equal(imaginary, gamma.Imaginary, 12);
    }

    [Fact]
    public void ExactMatchShortAndOpenHaveExplicitReflectionPoints()
    {
        Assert.Equal(new SmithComplex(0, 0), SmithChartMath.MatchedReflection);
        Assert.Equal(new SmithComplex(-1, 0), SmithChartMath.ShortCircuitReflection);
        Assert.Equal(new SmithComplex(1, 0), SmithChartMath.OpenCircuitReflection);
        Assert.False(SmithChartMath.TryGetImpedanceFromReflection(SmithChartMath.OpenCircuitReflection, 50, out _));
        Assert.True(SmithChartMath.TryGetImpedanceFromReflection(SmithChartMath.MatchedReflection, 50, out var matched));
        Assert.Equal(new SmithComplex(50, 0), matched);
    }

    [Theory]
    [InlineData(double.NaN, 0, 50)]
    [InlineData(50, double.PositiveInfinity, 50)]
    [InlineData(50, 0, 0)]
    [InlineData(50, 0, double.NaN)]
    public void InvalidImpedanceInputsAreRejected(double resistance, double reactance, double z0) =>
        Assert.False(SmithChartMath.TryGetReflectionFromImpedance(resistance, reactance, z0, out _));

    [Fact]
    public void ReflectionOutsideDiskIsFlaggedWithoutClamping()
    {
        Assert.True(SmithChartMath.TryGetReflectionFromSignedS11(3.010299956639812, 0, out var gamma));
        Assert.False(SmithChartMath.IsInsideOrOnUnitCircle(gamma));
        Assert.Equal(Math.Sqrt(2), gamma.Magnitude, 12);
    }

    [Fact]
    public void PositiveSignedReturnLossRemainsOutsideUnitDisk()
    {
        Assert.True(SmithChartMath.TryGetReflectionFromSignedS11(0.001, 0, out var gamma));
        Assert.False(SmithChartMath.IsInsideOrOnUnitCircle(gamma));
    }

    [Fact]
    public void OpenCircuitIsOnBoundaryAndImpedanceConversionIsSingular()
    {
        Assert.True(SmithChartMath.TryGetReflectionFromSignedS11(0, 0, out var gamma));
        Assert.True(SmithChartMath.IsInsideOrOnUnitCircle(gamma));
        Assert.False(SmithChartMath.TryGetImpedanceFromReflection(gamma, 50, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(90)]
    public void UnitReflectionWithDifferentPhasesStaysOnCircle(double phase)
    {
        Assert.True(SmithChartMath.TryGetReflectionFromSignedS11(0, phase, out var gamma));
        Assert.True(SmithChartMath.IsInsideOrOnUnitCircle(gamma));
        Assert.Equal(1, gamma.Magnitude, 14);
    }

    [Fact]
    public void NearOpenImpedanceConversionRemainsAccurate()
    {
        var reflection = new SmithComplex(0.9999999999, 0);
        Assert.True(SmithChartMath.TryGetImpedanceFromReflection(reflection, 50, out var impedance));
        Assert.InRange(Math.Abs(impedance.Real - 999_999_917_209.6359d) / 999_999_917_209.6359d, 0, 1e-12);
    }

    [Theory]
    [InlineData(320, 240)]
    [InlineData(1440, 900)]
    [InlineData(900, 1440)]
    public void TransformPreservesCircleAspectAndRoundTripsCoordinates(double width, double height)
    {
        var transform = new SmithChartTransform(width, height, 1.5, 0.1, -0.2);
        var center = transform.ToPixel(new SmithComplex(0.1, -0.2));
        var right = transform.ToPixel(new SmithComplex(1.1, -0.2));
        var top = transform.ToPixel(new SmithComplex(0.1, 0.8));

        Assert.Equal(transform.RadiusPixels, right.X - center.X, 10);
        Assert.Equal(transform.RadiusPixels, center.Y - top.Y, 10);
        var source = new SmithComplex(0.25, -0.4);
        var pixel = transform.ToPixel(source);
        var roundTrip = transform.FromPixel(pixel.X, pixel.Y);
        Assert.Equal(source.Real, roundTrip.Real, 12);
        Assert.Equal(source.Imaginary, roundTrip.Imaginary, 12);
    }

    [Fact]
    public void GridHasExpectedNormalizedLabelsAndStaysInUnitDisk()
    {
        var grid = SmithChartGrid.Create(50);

        Assert.Contains(grid, curve => curve.Kind == SmithGridCurveKind.Resistance && curve.Value == 1);
        Assert.Contains(grid, curve => curve.Kind == SmithGridCurveKind.Reactance && curve.Value == -1);
        Assert.Contains(grid, curve => curve.Kind == SmithGridCurveKind.Reactance && curve.Value == 1);
        Assert.Contains(grid, curve => curve.Kind == SmithGridCurveKind.RealAxis);
        Assert.Contains(grid, curve => curve.Kind == SmithGridCurveKind.UnitCircle);
        Assert.All(grid.SelectMany(curve => curve.Points), point =>
            Assert.True(point.Magnitude <= 1 + 1e-12, $"Grid point {point} exceeds the unit circle."));
        Assert.All(grid.Where(curve => curve.Kind == SmithGridCurveKind.Resistance), curve =>
        {
            Assert.Equal(SmithChartMath.OpenCircuitReflection, curve.Points[0]);
            Assert.Equal(SmithChartMath.OpenCircuitReflection.Real, curve.Points[^1].Real, 12);
        });
        Assert.All(grid.Where(curve => curve.Kind == SmithGridCurveKind.Reactance), curve =>
        {
            Assert.Equal(1, curve.Points[^1].Real, 12);
            Assert.Equal(0, curve.Points[^1].Imaginary, 12);
        });
    }
}
