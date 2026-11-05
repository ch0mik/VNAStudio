using System.Text.Json.Serialization;

namespace Vna.Core;

public enum SampleQuality
{
    Valid,
    Missing,
    Invalid,
}

/// <summary>Whether this sample's phase distinguishes the physical sign of reactance.</summary>
public enum PhaseSignConfidence
{
    Missing,
    Ambiguous,
    Verified,
}

/// <summary>Evidence source for phase-sign confidence; provenance does not imply RF calibration.</summary>
public enum PhaseSignProvenance
{
    Unknown,
    LegacyVna65Mhz,
    AnalyticDemo,
    ImportedUnverified,
    ReferenceLoadValidation,
}

/// <summary>One frequency sample. Nullable values identify quantities unavailable from the source/calibration.</summary>
public sealed record MeasurementPoint(
    long FrequencyHz,
    double? Swr,
    double? ReturnLossDb,
    double? PhaseDegrees,
    double? ImpedanceOhms,
    double? ResistanceOhms,
    double? ReactanceOhms,
    SampleQuality Quality = SampleQuality.Valid)
{
    public PhaseSignConfidence PhaseSignConfidence { get; init; } = PhaseDegrees is null
        ? PhaseSignConfidence.Missing
        : PhaseSignConfidence.Ambiguous;

    public PhaseSignProvenance PhaseSignProvenance { get; init; } = PhaseSignProvenance.Unknown;

    /// <summary>Derived one-way cable loss; only meaningful for a calibrated open/short-ended cable measurement.</summary>
    [JsonIgnore]
    public double? CableLossDb => CableLossCalculator.CalculateOneWayDb(ReturnLossDb);

    /// <summary>Scalar transmission magnitude in dB when the device is measuring its FILTER/Wobuloscope path.</summary>
    public double? TransmissionDb { get; init; }
}
