namespace Vna.Core;

/// <summary>A finite complex number used by Smith-chart calculations.</summary>
public readonly record struct SmithComplex(double Real, double Imaginary)
{
    public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);
    public SmithComplex Conjugate => new(Real, -Imaginary);
    public bool IsFinite => double.IsFinite(Real) && double.IsFinite(Imaginary);

    public override string ToString() => $"({Real:R}, {Imaginary:R}j)";
}

/// <summary>Pure conversions between normalized impedance, impedance and reflection coefficient.</summary>
public static class SmithChartMath
{
    public static SmithComplex ShortCircuitReflection { get; } = new(-1, 0);
    public static SmithComplex OpenCircuitReflection { get; } = new(1, 0);
    public static SmithComplex MatchedReflection { get; } = new(0, 0);

    public static bool TryGetReflectionFromSignedS11(
        double signedS11Db, double phaseDegrees, out SmithComplex reflection)
    {
        reflection = default;
        if (!double.IsFinite(signedS11Db) || !double.IsFinite(phaseDegrees)) return false;

        var magnitude = Math.Pow(10, signedS11Db / 20d);
        if (!double.IsFinite(magnitude)) return false;
        var phaseRadians = Math.IEEERemainder(phaseDegrees, 360d) * Math.PI / 180d;
        reflection = new SmithComplex(magnitude * Math.Cos(phaseRadians), magnitude * Math.Sin(phaseRadians));
        return reflection.IsFinite;
    }

    public static bool TryGetReflectionFromImpedance(
        double resistanceOhms, double reactanceOhms, double referenceImpedanceOhms, out SmithComplex reflection)
    {
        reflection = default;
        if (!double.IsFinite(resistanceOhms) || !double.IsFinite(reactanceOhms) ||
            !IsValidReference(referenceImpedanceOhms)) return false;

        var denominatorReal = resistanceOhms + referenceImpedanceOhms;
        var denominatorSquared = denominatorReal * denominatorReal + reactanceOhms * reactanceOhms;
        if (!double.IsFinite(denominatorSquared) || denominatorSquared == 0) return false;

        var numeratorReal = resistanceOhms - referenceImpedanceOhms;
        var numeratorImaginary = reactanceOhms;
        reflection = new SmithComplex(
            (numeratorReal * denominatorReal + numeratorImaginary * reactanceOhms) / denominatorSquared,
            (numeratorImaginary * denominatorReal - numeratorReal * reactanceOhms) / denominatorSquared);
        return reflection.IsFinite;
    }

    public static bool TryGetImpedanceFromReflection(
        SmithComplex reflection, double referenceImpedanceOhms, out SmithComplex impedance)
    {
        impedance = default;
        if (!reflection.IsFinite || !IsValidReference(referenceImpedanceOhms)) return false;

        var denominatorReal = 1 - reflection.Real;
        var denominatorImaginary = -reflection.Imaginary;
        var denominatorSquared = denominatorReal * denominatorReal + denominatorImaginary * denominatorImaginary;
        if (!double.IsFinite(denominatorSquared) || denominatorSquared == 0) return false;

        var numeratorReal = (1 - reflection.Real) * (1 + reflection.Real) -
                            reflection.Imaginary * reflection.Imaginary;
        var numeratorImaginary = 2 * reflection.Imaginary;
        impedance = new SmithComplex(
            referenceImpedanceOhms * numeratorReal / denominatorSquared,
            referenceImpedanceOhms * numeratorImaginary / denominatorSquared);
        return impedance.IsFinite;
    }

    public static bool IsInsideOrOnUnitCircle(SmithComplex reflection) =>
        reflection.IsFinite && reflection.Real * reflection.Real + reflection.Imaginary * reflection.Imaginary <= 1 + 1e-14;

    private static bool IsValidReference(double referenceImpedanceOhms) =>
        double.IsFinite(referenceImpedanceOhms) && referenceImpedanceOhms > 0;
}

public enum SmithGridCurveKind
{
    Resistance,
    Reactance,
    RealAxis,
    UnitCircle,
}

public sealed record SmithGridCurve(SmithGridCurveKind Kind, double Value, IReadOnlyList<SmithComplex> Points);

/// <summary>Generates normalized grid traces; all returned finite-impedance samples lie in the unit disk.</summary>
public static class SmithChartGrid
{
    private static readonly double[] Resistances = [0, 0.2, 0.5, 1, 2, 5];
    private static readonly double[] Reactances = [-5, -2, -1, -0.5, -0.2, 0.2, 0.5, 1, 2, 5];

    public static IReadOnlyList<SmithGridCurve> Create(double referenceImpedanceOhms, int subdivisions = 160)
    {
        if (!double.IsFinite(referenceImpedanceOhms) || referenceImpedanceOhms <= 0)
            throw new ArgumentOutOfRangeException(nameof(referenceImpedanceOhms));
        if (subdivisions < 16 || subdivisions > 2048)
            throw new ArgumentOutOfRangeException(nameof(subdivisions));

        var curves = new List<SmithGridCurve>(Resistances.Length + Reactances.Length + 2);
        foreach (var resistance in Resistances)
        {
            var points = new SmithComplex[subdivisions + 1];
            var centerReal = resistance / (resistance + 1);
            var radius = 1 / (resistance + 1);
            for (var index = 0; index <= subdivisions; index++)
            {
                var angle = 2 * Math.PI * index / subdivisions;
                points[index] = new SmithComplex(centerReal + radius * Math.Cos(angle), radius * Math.Sin(angle));
            }
            curves.Add(new SmithGridCurve(SmithGridCurveKind.Resistance, resistance, points));
        }

        foreach (var reactance in Reactances)
        {
            var points = new SmithComplex[subdivisions + 1];
            var centerReal = 1;
            var centerImaginary = 1 / reactance;
            var radius = 1 / Math.Abs(reactance);
            if (!SmithChartMath.TryGetReflectionFromImpedance(0, reactance, 1,
                    out var start))
                throw new InvalidOperationException("Unable to generate a finite reactance grid curve.");
            var startAngle = Math.Atan2(start.Imaginary - centerImaginary, start.Real - centerReal);
            if (reactance > 0 && startAngle < 0) startAngle += 2 * Math.PI;
            if (reactance < 0 && startAngle > 0) startAngle -= 2 * Math.PI;
            var endAngle = reactance > 0 ? 3 * Math.PI / 2 : -3 * Math.PI / 2;
            for (var index = 0; index <= subdivisions; index++)
            {
                var angle = startAngle + (endAngle - startAngle) * index / subdivisions;
                points[index] = new SmithComplex(
                    centerReal + radius * Math.Cos(angle), centerImaginary + radius * Math.Sin(angle));
            }
            curves.Add(new SmithGridCurve(SmithGridCurveKind.Reactance, reactance, points));
        }

        var realAxis = Enumerable.Range(0, subdivisions + 1)
            .Select(index => new SmithComplex(-1d + 2d * index / subdivisions, 0)).ToArray();
        curves.Add(new SmithGridCurve(SmithGridCurveKind.RealAxis, 0, realAxis));

        var unitCircle = Enumerable.Range(0, subdivisions + 1)
            .Select(index =>
            {
                var angle = 2d * Math.PI * index / subdivisions;
                return new SmithComplex(Math.Cos(angle), Math.Sin(angle));
            }).ToArray();
        curves.Add(new SmithGridCurve(SmithGridCurveKind.UnitCircle, 1, unitCircle));
        return curves;
    }
}

/// <summary>Aspect-preserving pixel transform. Pan coordinates are normalized reflection-coefficient units.</summary>
public readonly record struct SmithChartTransform
{
    public SmithChartTransform(double width, double height, double zoom = 1, double panReal = 0, double panImaginary = 0)
    {
        if (!double.IsFinite(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (!double.IsFinite(height) || height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (!double.IsFinite(zoom) || zoom is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(zoom));
        if (!double.IsFinite(panReal)) throw new ArgumentOutOfRangeException(nameof(panReal));
        if (!double.IsFinite(panImaginary)) throw new ArgumentOutOfRangeException(nameof(panImaginary));
        Width = width;
        Height = height;
        Zoom = zoom;
        PanReal = panReal;
        PanImaginary = panImaginary;
        RadiusPixels = Math.Min(width, height) * 0.5 * zoom;
    }

    public double Width { get; }
    public double Height { get; }
    public double Zoom { get; }
    public double PanReal { get; }
    public double PanImaginary { get; }
    public double RadiusPixels { get; }

    public (double X, double Y) ToPixel(SmithComplex reflection) =>
        (Width * 0.5 + (reflection.Real - PanReal) * RadiusPixels,
         Height * 0.5 - (reflection.Imaginary - PanImaginary) * RadiusPixels);

    public SmithComplex FromPixel(double x, double y) => new(
        PanReal + (x - Width * 0.5) / RadiusPixels,
        PanImaginary - (y - Height * 0.5) / RadiusPixels);
}
