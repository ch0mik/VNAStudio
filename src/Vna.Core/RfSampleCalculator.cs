using Vna.Localization;
namespace Vna.Core;

/// <summary>
/// Converts signed reflection coefficient readings to derived RF values.
/// ReturnLossDb uses the signed convention 20*log10(|Γ|); phase uses Γ = |Γ|·exp(+jφ).
/// </summary>
public static class RfSampleCalculator
{
    private const double SingularDenominatorSquared = 1e-24;

    public static MeasurementPoint FromReturnLossAndPhase(
        long frequencyHz,
        double returnLossDb,
        double phaseDegrees,
        DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return FromReturnLossAndPhase(frequencyHz, returnLossDb, phaseDegrees, profile.ReferenceImpedanceOhms);
    }

    public static MeasurementPoint FromReturnLossAndPhase(
        long frequencyHz,
        double returnLossDb,
        double phaseDegrees,
        double referenceImpedanceOhms)
    {
        if (frequencyHz < 0)
            throw new ArgumentOutOfRangeException(nameof(frequencyHz));
        if (!double.IsFinite(returnLossDb))
            throw new ArgumentOutOfRangeException(nameof(returnLossDb));
        if (!double.IsFinite(phaseDegrees))
            throw new ArgumentOutOfRangeException(nameof(phaseDegrees));
        if (!double.IsFinite(referenceImpedanceOhms) || referenceImpedanceOhms <= 0)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(referenceImpedanceOhms), "ReferenceImpedancePositive");

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

        var impedanceReal = referenceImpedanceOhms *
                            (1 - gammaReal * gammaReal - gammaImaginary * gammaImaginary) / denominatorSquared;
        var impedanceImaginary = referenceImpedanceOhms * (2 * gammaImaginary / denominatorSquared);
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

    private static MeasurementPoint InvalidPoint(long frequencyHz) =>
        new(frequencyHz, null, null, null, null, null, null, SampleQuality.Invalid);
}
