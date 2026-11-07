namespace Vna.Core;

public enum MeasurementAdjustmentComponent
{
    ReturnLossDisplayOffset,
    DdsFrequencyCorrection,
    ReflectionCalibration,
    FilterNormalization,
    HardwareTrim,
}

public enum MeasurementAdjustmentStatus
{
    Unknown,
    NotApplied,
    Applied,
}

public enum MeasurementAdjustmentOrigin
{
    Unknown,
    ApplicationPipeline,
    ApplicationSetting,
    DeviceProfile,
    SyntheticModel,
    ReferenceMeasurement,
}

public enum MeasurementAdjustmentEvidence
{
    Unknown,
    ConfiguredUnverified,
    ReferenceValidated,
    DisplayOnly,
    Synthetic,
    KnownAbsent,
}

public sealed record MeasurementAdjustmentParameter(
    string Name,
    double Value,
    string Unit,
    MeasurementAdjustmentUncertainty? Uncertainty = null);

public sealed record MeasurementAdjustmentUncertainty(
    double StandardUncertainty,
    string Method);

/// <summary>Provenance for one transformation or correction applied to a sweep.</summary>
public sealed record MeasurementAdjustmentSnapshot(
    MeasurementAdjustmentComponent Component,
    MeasurementAdjustmentStatus Status,
    MeasurementAdjustmentOrigin Origin,
    MeasurementAdjustmentEvidence Evidence,
    string? SourceId,
    IReadOnlyList<MeasurementAdjustmentParameter> Parameters);

/// <summary>
/// Versioned processing provenance. Missing snapshots in older session files mean unknown;
/// they are not reconstructed from current settings.
/// </summary>
public sealed record MeasurementCalibrationSnapshot(
    int SchemaVersion,
    IReadOnlyList<MeasurementAdjustmentSnapshot> Adjustments)
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Captures provenance from the active processing source rather than protocol fields in the
    /// transitional DeviceProfile. Hardware sources without a report remain Unknown.
    /// </summary>
    public static MeasurementCalibrationSnapshot Capture(
        SessionAnalysisMode analysisMode,
        MeasurementDataKind dataKind,
        bool hardwareSource,
        bool syntheticSource,
        double returnLossDisplayOffsetDb,
        MeasurementAdjustmentSnapshot? appliedReflectionCalibration = null)
    {
        if (!Enum.IsDefined(analysisMode)) throw new ArgumentOutOfRangeException(nameof(analysisMode));
        if (!Enum.IsDefined(dataKind)) throw new ArgumentOutOfRangeException(nameof(dataKind));
        if (!double.IsFinite(returnLossDisplayOffsetDb)) throw new ArgumentOutOfRangeException(nameof(returnLossDisplayOffsetDb));
        if (appliedReflectionCalibration is { Component: not MeasurementAdjustmentComponent.ReflectionCalibration })
            throw new ArgumentException("Processing provenance must describe reflection calibration.", nameof(appliedReflectionCalibration));

        var reflectionData = dataKind == MeasurementDataKind.ReflectionDerived;
        var syntheticData = syntheticSource;
        var hasDisplayOffset = reflectionData && returnLossDisplayOffsetDb != 0;
        var reflectionAdjustment = !reflectionData
            ? UnknownAdjustment(MeasurementAdjustmentComponent.ReflectionCalibration)
            : hardwareSource
                ? appliedReflectionCalibration ?? UnknownAdjustment(MeasurementAdjustmentComponent.ReflectionCalibration)
                : syntheticData
                    ? new MeasurementAdjustmentSnapshot(
                        MeasurementAdjustmentComponent.ReflectionCalibration,
                        MeasurementAdjustmentStatus.NotApplied,
                        MeasurementAdjustmentOrigin.SyntheticModel,
                        MeasurementAdjustmentEvidence.Synthetic,
                        null,
                        Array.Empty<MeasurementAdjustmentParameter>())
                    : UnknownAdjustment(MeasurementAdjustmentComponent.ReflectionCalibration);

        return new MeasurementCalibrationSnapshot(CurrentSchemaVersion,
        [
            new MeasurementAdjustmentSnapshot(
                MeasurementAdjustmentComponent.ReturnLossDisplayOffset,
                hasDisplayOffset ? MeasurementAdjustmentStatus.Applied : MeasurementAdjustmentStatus.NotApplied,
                MeasurementAdjustmentOrigin.ApplicationSetting,
                MeasurementAdjustmentEvidence.DisplayOnly,
                "ReturnLossDisplayOffsetDb",
                hasDisplayOffset
                    ? [new MeasurementAdjustmentParameter("offset_db", returnLossDisplayOffsetDb, "dB")]
                    : Array.Empty<MeasurementAdjustmentParameter>()),
            new MeasurementAdjustmentSnapshot(
                MeasurementAdjustmentComponent.DdsFrequencyCorrection,
                MeasurementAdjustmentStatus.NotApplied,
                MeasurementAdjustmentOrigin.ApplicationPipeline,
                MeasurementAdjustmentEvidence.KnownAbsent,
                null,
                Array.Empty<MeasurementAdjustmentParameter>()),
            reflectionAdjustment,
            new MeasurementAdjustmentSnapshot(
                MeasurementAdjustmentComponent.FilterNormalization,
                syntheticData && dataKind == MeasurementDataKind.ScalarTransmissionMagnitude
                    ? MeasurementAdjustmentStatus.NotApplied : MeasurementAdjustmentStatus.Unknown,
                syntheticData ? MeasurementAdjustmentOrigin.SyntheticModel : MeasurementAdjustmentOrigin.Unknown,
                syntheticData && dataKind == MeasurementDataKind.ScalarTransmissionMagnitude
                    ? MeasurementAdjustmentEvidence.KnownAbsent : MeasurementAdjustmentEvidence.Unknown,
                null,
                Array.Empty<MeasurementAdjustmentParameter>()),
            new MeasurementAdjustmentSnapshot(
                MeasurementAdjustmentComponent.HardwareTrim,
                syntheticData ? MeasurementAdjustmentStatus.NotApplied : MeasurementAdjustmentStatus.Unknown,
                syntheticData ? MeasurementAdjustmentOrigin.SyntheticModel : MeasurementAdjustmentOrigin.Unknown,
                syntheticData ? MeasurementAdjustmentEvidence.KnownAbsent : MeasurementAdjustmentEvidence.Unknown,
                null,
                Array.Empty<MeasurementAdjustmentParameter>()),
        ]);
    }

    private static MeasurementAdjustmentSnapshot UnknownAdjustment(MeasurementAdjustmentComponent component) => new(
        component,
        MeasurementAdjustmentStatus.Unknown,
        MeasurementAdjustmentOrigin.Unknown,
        MeasurementAdjustmentEvidence.Unknown,
        null,
        Array.Empty<MeasurementAdjustmentParameter>());
}
