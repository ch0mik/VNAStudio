namespace Vna.Core;

/// <summary>
/// Calibration constants copied from legacy INI. The affine coefficients are documented as
/// y=ax+b; the RL offset acquisition semantics remain unverified.
/// </summary>
public sealed record LegacyCalibrationSettings(
    double ReturnLossOffsetVnaDb,
    double ReturnLossOffsetFilterDb,
    double ReturnLossA,
    double ReturnLossB,
    double PhaseA,
    double PhaseB,
    double InputVelocityFactor)
{
    public static LegacyCalibrationSettings Default { get; } = new(0, 0, 1, 0, 1, 0, 0.66);
}
