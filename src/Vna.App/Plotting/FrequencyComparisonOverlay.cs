using Vna.Core;

namespace Vna.App.Plotting;

/// <summary>A named comparison sweep rendered from its original samples on a frequency chart.</summary>
public sealed record FrequencyComparisonOverlay(
    string Name,
    IReadOnlyList<MeasurementPoint> Points,
    string Color);
