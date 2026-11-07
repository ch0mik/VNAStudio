using Vna.Core;

namespace Vna.Device.Vna65MHz;

/// <summary>Protocol and ADC settings owned by the VNA65 plugin, separated from the Core profile snapshot.</summary>
public sealed record Vna65MHzProtocolSettings(
    DeviceProfileDescriptor Descriptor,
    long DdsClockHz,
    int DdsPllPrescaler,
    int VnaType,
    bool Svna,
    int StartPauseMilliseconds,
    int StepPauseMilliseconds,
    double MagnitudeAdcDbPerCount,
    double PhaseAdcDegreesPerCount,
    LegacyCalibrationSettings Calibration)
{
    public double ReferenceImpedanceOhms =>
        (Descriptor ?? throw new InvalidOperationException("The VNA65 profile descriptor is required.")).ReferenceImpedanceOhms ??
        throw new InvalidOperationException("The VNA65 profile must declare its reference impedance.");

    /// <summary>Temporary bridge for existing INI/profile and saved-session compatibility.</summary>
    public static Vna65MHzProtocolSettings FromLegacyProfile(DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new Vna65MHzProtocolSettings(
            Vna65MHzDeviceProfile.Describe(profile),
            profile.DdsClockHz,
            profile.DdsPllPrescaler,
            profile.LegacyVnaType,
            profile.LegacySvna,
            profile.StartPauseMilliseconds,
            profile.StepPauseMilliseconds,
            profile.MagnitudeAdcDbPerCount,
            profile.PhaseAdcDegreesPerCount,
            profile.LegacyCalibration);
    }
}
