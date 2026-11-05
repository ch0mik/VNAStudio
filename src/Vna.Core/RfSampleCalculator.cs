using Vna.Localization;
namespace Vna.Core;

public enum LegacyAdcMeasurementMode
{
    Reflection,
    ScalarTransmission,
}

/// <summary>
/// Converts signed reflection coefficient readings to derived RF values.
/// ReturnLossDb uses the signed convention 20*log10(|Γ|); phase uses Γ = |Γ|·exp(+jφ).
/// </summary>
public static class RfSampleCalculator
{
    private const double SingularDenominatorSquared = 1e-24;

    /// <summary>
    /// Converts the legacy ADC channels using the original RF magnitude formula, normalized
    /// to this application's signed 20*log10(|Gamma|) convention. VNA_type changes magnitude
    /// polarity and reference level; it does not change serial frame length.
    /// </summary>
    public static MeasurementPoint FromLegacyAdcReadings(
        long frequencyHz, int phaseAdc, int magnitudeAdc, DeviceProfile profile,
        LegacyAdcMeasurementMode mode = LegacyAdcMeasurementMode.Reflection)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (phaseAdc is < 0 or > 1023) throw new ArgumentOutOfRangeException(nameof(phaseAdc));
        if (magnitudeAdc is < 0 or > 1023) throw new ArgumentOutOfRangeException(nameof(magnitudeAdc));
        if (profile.LegacyVnaType is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(profile));

        var calibration = profile.LegacyCalibration;
        var magnitudeDb = magnitudeAdc * profile.MagnitudeAdcDbPerCount * calibration.ReturnLossA +
                          calibration.ReturnLossB;
        // Original constants: float 30 at 00401248, double -9.84 at 00401720.
        var typeOffsetDb = profile.LegacyVnaType == 1 ? -9.84 : 0;
        var polarity = profile.LegacyVnaType == 1 ? -1 : 1;
        var modeOffset = mode == LegacyAdcMeasurementMode.ScalarTransmission
            ? calibration.ReturnLossOffsetFilterDb
            : calibration.ReturnLossOffsetVnaDb;
        var signedReturnLossDb = (magnitudeDb - 30 - typeOffsetDb) * polarity - modeOffset;
        // PH_b follows the degree-based CSV convention. The original RF path adds it in
        // radians; equivalence is confirmed for the supplied profile's PH_b=0 only.
        var phaseDegrees = phaseAdc * profile.PhaseAdcDegreesPerCount * calibration.PhaseA +
                           calibration.PhaseB;
        if (mode == LegacyAdcMeasurementMode.ScalarTransmission)
        {
            return new MeasurementPoint(frequencyHz, Swr: null, ReturnLossDb: null, PhaseDegrees: null,
                ImpedanceOhms: null, ResistanceOhms: null, ReactanceOhms: null)
            {
                PhaseSignConfidence = PhaseSignConfidence.Missing,
                PhaseSignProvenance = PhaseSignProvenance.Unknown,
                TransmissionDb = signedReturnLossDb,
            };
        }

        return FromReturnLossAndPhase(frequencyHz, signedReturnLossDb, phaseDegrees, profile);
    }

    /// <summary>
    /// Applies the affine coefficients from SPECIAL_CAL in analyz.ini to already-scaled,
    /// signed return-loss and phase readings, then derives RF values. ADC count decoding and
    /// RL_OFFSET_vna/filter are outside this method because its inputs are already scaled.
    /// For hardware ADC channels, use FromLegacyAdcReadings instead.
    /// </summary>
    public static MeasurementPoint FromScaledReadings(
        long frequencyHz,
        double returnLossBeforeAffineCalibrationDb,
        double phaseBeforeAffineCalibrationDegrees,
        DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var calibration = profile.LegacyCalibration;
        if (!double.IsFinite(returnLossBeforeAffineCalibrationDb))
            throw new ArgumentOutOfRangeException(nameof(returnLossBeforeAffineCalibrationDb));
        if (!double.IsFinite(phaseBeforeAffineCalibrationDegrees))
            throw new ArgumentOutOfRangeException(nameof(phaseBeforeAffineCalibrationDegrees));
        if (!double.IsFinite(calibration.ReturnLossA) || !double.IsFinite(calibration.ReturnLossB) ||
            !double.IsFinite(calibration.PhaseA) || !double.IsFinite(calibration.PhaseB))
            throw new ArgumentException("The profile contains non-finite affine calibration coefficients.", nameof(profile));

        var returnLossDb = calibration.ReturnLossA * returnLossBeforeAffineCalibrationDb + calibration.ReturnLossB;
        var phaseDegrees = calibration.PhaseA * phaseBeforeAffineCalibrationDegrees + calibration.PhaseB;
        if (!double.IsFinite(returnLossDb) || !double.IsFinite(phaseDegrees))
            return InvalidPoint(frequencyHz);

        return FromReturnLossAndPhase(frequencyHz, returnLossDb, phaseDegrees, profile);
    }

    public static MeasurementPoint FromReturnLossAndPhase(
        long frequencyHz,
        double returnLossDb,
        double phaseDegrees,
        DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (frequencyHz < 0)
            throw new ArgumentOutOfRangeException(nameof(frequencyHz));
        if (!double.IsFinite(returnLossDb))
            throw new ArgumentOutOfRangeException(nameof(returnLossDb));
        if (!double.IsFinite(phaseDegrees))
            throw new ArgumentOutOfRangeException(nameof(phaseDegrees));
        if (!double.IsFinite(profile.ReferenceImpedanceOhms) || profile.ReferenceImpedanceOhms <= 0)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(profile), "ReferenceImpedancePositive");

        var reflectionMagnitude = Math.Pow(10, returnLossDb / 20);
        if (!double.IsFinite(reflectionMagnitude))
            return InvalidPoint(frequencyHz);

        var normalizedPhaseDegrees = Math.IEEERemainder(phaseDegrees, 360);
        var phaseRadians = normalizedPhaseDegrees * Math.PI / 180;
        var gammaReal = reflectionMagnitude * Math.Cos(phaseRadians);
        var gammaImaginary = reflectionMagnitude * Math.Sin(phaseRadians);

        double? swr = reflectionMagnitude < 1
            ? (1 + reflectionMagnitude) / (1 - reflectionMagnitude)
            : null;

        var denominatorReal = 1 - gammaReal;
        var denominatorImaginary = -gammaImaginary;
        var denominatorSquared = denominatorReal * denominatorReal + denominatorImaginary * denominatorImaginary;
        if (!double.IsFinite(denominatorSquared) || denominatorSquared <= SingularDenominatorSquared)
        {
            return new MeasurementPoint(
                frequencyHz, swr, returnLossDb, phaseDegrees,
                ImpedanceOhms: null, ResistanceOhms: null, ReactanceOhms: null);
        }

        var impedanceReal = profile.ReferenceImpedanceOhms *
                            (1 - gammaReal * gammaReal - gammaImaginary * gammaImaginary) / denominatorSquared;
        var impedanceImaginary = profile.ReferenceImpedanceOhms * (2 * gammaImaginary / denominatorSquared);
        if (!double.IsFinite(impedanceReal) || !double.IsFinite(impedanceImaginary))
        {
            return new MeasurementPoint(
                frequencyHz, swr, returnLossDb, phaseDegrees,
                ImpedanceOhms: null, ResistanceOhms: null, ReactanceOhms: null);
        }

        var impedanceMagnitude = Math.Sqrt(impedanceReal * impedanceReal + impedanceImaginary * impedanceImaginary);
        return new MeasurementPoint(
            FrequencyHz: frequencyHz,
            Swr: swr,
            ReturnLossDb: returnLossDb,
            PhaseDegrees: phaseDegrees,
            ImpedanceOhms: double.IsFinite(impedanceMagnitude) ? impedanceMagnitude : null,
            ResistanceOhms: impedanceReal,
            ReactanceOhms: impedanceImaginary);
    }

    private static MeasurementPoint InvalidPoint(long frequencyHz) => new(
        FrequencyHz: frequencyHz,
        Swr: null,
        ReturnLossDb: null,
        PhaseDegrees: null,
        ImpedanceOhms: null,
        ResistanceOhms: null,
        ReactanceOhms: null,
        Quality: SampleQuality.Invalid);
}
