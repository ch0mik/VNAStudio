using Vna.Core;

namespace Vna.App.Plotting;

/// <summary>A provenance-preserving sampled sweep drawn as a comparison on the Smith chart.</summary>
public sealed record SmithComparisonOverlay(
    string Name,
    IReadOnlyList<MeasurementPoint> Points,
    double? ReferenceImpedanceOhms,
    string Color);
