namespace Vna.Core;

public enum CableResonanceKind { Peak, Dip }

public enum CableResonancePairIssue
{
    None,
    MissingMarkers,
    SameMarker,
    MarkerNotFound,
    DataGapBetweenMarkers,
    NotAtResonance,
    SameResonance,
    DifferentKinds,
    NotAdjacent,
}

public readonly record struct CableResonancePairValidation(
    CableResonancePairIssue Issue,
    CableResonanceKind? Kind = null)
{
    public bool IsValid => Issue == CableResonancePairIssue.None;
}

/// <summary>Checks that cable-length markers select adjacent SWR peaks or adjacent SWR dips.</summary>
public static class CableResonancePairValidator
{
    private const int MarkerToleranceSamples = 2;

    public static CableResonancePairValidation Validate(
        IReadOnlyList<MeasurementPoint> points, long? marker1Hz, long? marker2Hz)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (marker1Hz is null || marker2Hz is null) return new(CableResonancePairIssue.MissingMarkers);
        if (marker1Hz == marker2Hz) return new(CableResonancePairIssue.SameMarker);

        var marker1Index = FindMarkerIndex(points, marker1Hz.Value);
        var marker2Index = FindMarkerIndex(points, marker2Hz.Value);
        if (marker1Index < 0 || marker2Index < 0) return new(CableResonancePairIssue.MarkerNotFound);
        var lowerMarkerIndex = Math.Min(marker1Index, marker2Index);
        var upperMarkerIndex = Math.Max(marker1Index, marker2Index);
        for (var index = lowerMarkerIndex; index <= upperMarkerIndex; index++)
        {
            if (!HasUsableSwr(points[index]))
                return new(CableResonancePairIssue.DataGapBetweenMarkers);
        }

        var candidates = FindResonances(points);
        var first = FindNearestCandidate(candidates, marker1Index);
        var second = FindNearestCandidate(candidates, marker2Index);
        if (first is null || second is null) return new(CableResonancePairIssue.NotAtResonance);
        if (first.Value.Index == second.Value.Index) return new(CableResonancePairIssue.SameResonance);
        if (first.Value.Kind != second.Value.Kind)
            return new(CableResonancePairIssue.DifferentKinds);

        var lower = Math.Min(first.Value.Index, second.Value.Index);
        var upper = Math.Max(first.Value.Index, second.Value.Index);
        if (candidates.Any(candidate => candidate.Kind == first.Value.Kind &&
                                        candidate.Index > lower && candidate.Index < upper))
            return new(CableResonancePairIssue.NotAdjacent, first.Value.Kind);

        return new(CableResonancePairIssue.None, first.Value.Kind);
    }

    private static int FindMarkerIndex(IReadOnlyList<MeasurementPoint> points, long frequencyHz)
    {
        for (var index = 0; index < points.Count; index++)
        {
            if (points[index].FrequencyHz == frequencyHz) return index;
        }
        return -1;
    }

    private static ResonanceCandidate? FindNearestCandidate(
        IReadOnlyList<ResonanceCandidate> candidates, int markerIndex) => candidates
        .Where(candidate => Math.Abs(candidate.Index - markerIndex) <= MarkerToleranceSamples)
        .OrderBy(candidate => Math.Abs(candidate.Index - markerIndex))
        .ThenByDescending(candidate => candidate.Prominence)
        .ThenBy(candidate => candidate.Index)
        .Cast<ResonanceCandidate?>()
        .FirstOrDefault();

    private static List<ResonanceCandidate> FindResonances(IReadOnlyList<MeasurementPoint> points)
    {
        var candidates = new List<ResonanceCandidate>();
        var segmentStart = 0;
        while (segmentStart < points.Count)
        {
            while (segmentStart < points.Count && !HasUsableSwr(points[segmentStart])) segmentStart++;
            if (segmentStart >= points.Count) break;

            var segmentEnd = segmentStart + 1;
            while (segmentEnd < points.Count && HasUsableSwr(points[segmentEnd])) segmentEnd++;
            AddSegmentResonances(points, segmentStart, segmentEnd, candidates);
            segmentStart = segmentEnd + 1;
        }
        return candidates;
    }

    private static void AddSegmentResonances(
        IReadOnlyList<MeasurementPoint> points, int start, int end, List<ResonanceCandidate> candidates)
    {
        var count = end - start;
        if (count < 3) return;

        var smoothed = new double[count];
        for (var offset = 0; offset < count; offset++)
        {
            var first = Math.Max(0, offset - 1);
            var last = Math.Min(count - 1, offset + 1);
            var total = 0d;
            for (var sample = first; sample <= last; sample++) total += points[start + sample].Swr!.Value;
            smoothed[offset] = total / (last - first + 1);
        }

        var plateauStart = 1;
        while (plateauStart < count - 1)
        {
            var plateauEnd = plateauStart;
            while (plateauEnd + 1 < count - 1 && NearlyEqual(smoothed[plateauEnd + 1], smoothed[plateauStart]))
                plateauEnd++;

            var value = smoothed[plateauStart];
            var left = smoothed[plateauStart - 1];
            var right = smoothed[plateauEnd + 1];
            var kind = value > left && value > right
                ? CableResonanceKind.Peak
                : value < left && value < right
                    ? CableResonanceKind.Dip
                    : (CableResonanceKind?)null;

            if (kind is { } resonanceKind)
            {
                var index = start + (plateauStart + plateauEnd) / 2;
                var prominence = Math.Min(Math.Abs(value - left), Math.Abs(value - right));
                candidates.Add(new ResonanceCandidate(index, resonanceKind, prominence));
            }
            plateauStart = plateauEnd + 1;
        }
    }

    private static bool HasUsableSwr(MeasurementPoint point) =>
        point.Quality == SampleQuality.Valid && point.Swr is { } swr && double.IsFinite(swr) && swr > 0;

    private static bool NearlyEqual(double first, double second) =>
        Math.Abs(first - second) <= 1e-12 * Math.Max(1, Math.Max(Math.Abs(first), Math.Abs(second)));

    private readonly record struct ResonanceCandidate(int Index, CableResonanceKind Kind, double Prominence);
}

/// <summary>Estimates cable length from adjacent, same-type resonance markers.</summary>
public static class CableLengthCalculator
{
    public const double SpeedOfLightMetersPerSecond = 299_792_458;

    /// <summary>
    /// Adjacent resonance spacing in a reflected signal corresponds to a round-trip along the cable:
    /// length = velocityFactor × c / (2 × frequencySpacing).
    /// </summary>
    public static double CalculateMeters(long firstResonanceHz, long secondResonanceHz, double velocityFactor)
    {
        if (firstResonanceHz <= 0) throw new ArgumentOutOfRangeException(nameof(firstResonanceHz));
        if (secondResonanceHz <= 0) throw new ArgumentOutOfRangeException(nameof(secondResonanceHz));
        if (firstResonanceHz == secondResonanceHz) throw new ArgumentException("Resonance frequencies must differ.");
        if (!double.IsFinite(velocityFactor) || velocityFactor is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(velocityFactor));

        var spacingHz = Math.Abs((double)secondResonanceHz - firstResonanceHz);
        return velocityFactor * SpeedOfLightMetersPerSecond / (2 * spacingHz);
    }
}
