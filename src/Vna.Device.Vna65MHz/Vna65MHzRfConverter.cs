using Vna.Core;

namespace Vna.Device.Vna65MHz;

/// <summary>
/// VNA65-specific conversion of legacy ADC and SPECIAL_CAL values. The resulting signed
/// reflection data is passed to Core's device-neutral impedance calculation.
/// </summary>
public static class Vna65MHzRfConverter
{
    /// <summary>
    /// Converts the legacy ADC channels using the original RF magnitude formula, normalized
    /// to this application's signed 20*log10(|Gamma|) convention. VNA_type changes magnitude
    /// polarity and reference level; it does not change serial frame length.
    /// </summary>
    public static MeasurementPoint FromAdcReadings(
        long frequencyHz,
        int phaseAdc,
        int magnitudeAdc,
        Vna65MHzProtocolSettings profile,
        MeasurementAcquisitionMode mode = MeasurementAcquisitionMode.Reflection)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (phaseAdc is < 0 or > 1023) throw new ArgumentOutOfRangeException(nameof(phaseAdc));
        if (magnitudeAdc is < 0 or > 1023) throw new ArgumentOutOfRangeException(nameof(magnitudeAdc));
        if (profile.VnaType is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(profile));

        var calibration = profile.Calibration;
        var magnitudeDb = magnitudeAdc * profile.MagnitudeAdcDbPerCount * calibration.ReturnLossA +
                          calibration.ReturnLossB;
        // Original constants: float 30 at 00401248, double -9.84 at 00401720.
        var typeOffsetDb = profile.VnaType == 1 ? -9.84 : 0;
        var polarity = profile.VnaType == 1 ? -1 : 1;
        var modeOffset = mode == MeasurementAcquisitionMode.ScalarTransmission
            ? calibration.ReturnLossOffsetFilterDb
            : calibration.ReturnLossOffsetVnaDb;
        var signedReturnLossDb = (magnitudeDb - 30 - typeOffsetDb) * polarity - modeOffset;
        // PH_b follows the degree-based CSV convention. The original RF path adds it in
        // radians; equivalence is confirmed for the supplied profile's PH_b=0 only.
        var phaseDegrees = phaseAdc * profile.PhaseAdcDegreesPerCount * calibration.PhaseA +
                           calibration.PhaseB;
        if (mode == MeasurementAcquisitionMode.ScalarTransmission)
        {
            return new MeasurementPoint(frequencyHz, Swr: null, ReturnLossDb: null, PhaseDegrees: null,
                ImpedanceOhms: null, ResistanceOhms: null, ReactanceOhms: null)
            {
                PhaseSignConfidence = PhaseSignConfidence.Missing,
                PhaseSignProvenance = PhaseSignProvenance.Unknown,
                TransmissionDb = signedReturnLossDb,
            };
        }

        return RfSampleCalculator.FromReturnLossAndPhase(frequencyHz, signedReturnLossDb, phaseDegrees,
            profile.ReferenceImpedanceOhms);
    }

    /// <summary>
    /// Applies VNA65 SPECIAL_CAL affine coefficients to already-scaled signed return-loss and
    /// phase readings, then derives RF values. ADC count decoding and mode offsets are separate.
    /// </summary>
    public static MeasurementPoint FromScaledReadings(
        long frequencyHz,
        double returnLossBeforeAffineCalibrationDb,
        double phaseBeforeAffineCalibrationDegrees,
        Vna65MHzProtocolSettings profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var calibration = profile.Calibration;
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
            return new MeasurementPoint(frequencyHz, Swr: null, ReturnLossDb: null, PhaseDegrees: null,
                ImpedanceOhms: null, ResistanceOhms: null, ReactanceOhms: null, Quality: SampleQuality.Invalid);

        return RfSampleCalculator.FromReturnLossAndPhase(frequencyHz, returnLossDb, phaseDegrees,
            profile.ReferenceImpedanceOhms);
    }
}
