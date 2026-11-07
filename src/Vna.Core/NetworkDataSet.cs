namespace Vna.Core;

/// <summary>A finite real/imaginary pair for a complex network parameter.</summary>
public sealed record ComplexCoefficient(double Real, double Imaginary)
{
    public bool IsFinite => double.IsFinite(Real) && double.IsFinite(Imaginary);
}

/// <summary>
/// Network data for one frequency. Two-port ordering follows the Touchstone v1 row order:
/// S11, S21, S12, S22. A one-port sample contains only S11.
/// </summary>
public sealed record NetworkParameterSample(
    double FrequencyHz,
    ComplexCoefficient? S11,
    ComplexCoefficient? S21 = null,
    ComplexCoefficient? S12 = null,
    ComplexCoefficient? S22 = null);

/// <summary>
/// Versioned complex S-parameter data, separate from reflection-derived or scalar transmission
/// measurements. The reference impedance is explicit and applies to every port in this dataset.
/// </summary>
public sealed record NetworkDataSet(
    int SchemaVersion,
    int PortCount,
    double ReferenceImpedanceOhms,
    IReadOnlyList<NetworkParameterSample> Samples)
{
    public const int CurrentSchemaVersion = 1;

    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
            throw new ArgumentOutOfRangeException(nameof(SchemaVersion), "Unsupported network data schema version.");
        if (PortCount is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(PortCount), "Only one-port and two-port network data are supported.");
        if (!double.IsFinite(ReferenceImpedanceOhms) || ReferenceImpedanceOhms <= 0)
            throw new ArgumentOutOfRangeException(nameof(ReferenceImpedanceOhms));
        ArgumentNullException.ThrowIfNull(Samples);
        if (Samples.Count == 0)
            throw new ArgumentException("Network data must contain at least one sample.", nameof(Samples));

        var previousFrequencyHz = -1d;
        foreach (var sample in Samples)
        {
            if (sample is null || !double.IsFinite(sample.FrequencyHz) || sample.FrequencyHz < 0 ||
                sample.FrequencyHz <= previousFrequencyHz)
                throw new ArgumentException("Sample frequencies must be finite, non-negative, and strictly increasing.", nameof(Samples));
            previousFrequencyHz = sample.FrequencyHz;

            if (PortCount == 1)
            {
                if (sample.S11 is null || sample.S21 is not null || sample.S12 is not null || sample.S22 is not null)
                    throw new ArgumentException("One-port data must contain S11 only.", nameof(Samples));
            }
            else if (sample.S11 is null || sample.S21 is null || sample.S12 is null || sample.S22 is null)
            {
                throw new ArgumentException("Two-port data must contain S11, S21, S12, and S22.", nameof(Samples));
            }

            if (sample.S11 is { IsFinite: false } || sample.S21 is { IsFinite: false } ||
                sample.S12 is { IsFinite: false } || sample.S22 is { IsFinite: false })
                throw new ArgumentException("Complex network parameter values must be finite.", nameof(Samples));
        }
    }
}
