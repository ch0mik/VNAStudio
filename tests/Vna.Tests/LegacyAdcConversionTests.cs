using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class LegacyAdcConversionTests
{
    [Fact]
    public void ScalarTransmissionUsesFilterOffsetAndDoesNotInventReflectionValues()
    {
        var profile = DeviceProfile.Vna65MHz with
        {
            LegacyVnaType = 0,
            MagnitudeAdcDbPerCount = 0.1,
            LegacyCalibration = DeviceProfile.Vna65MHz.LegacyCalibration with
            {
                ReturnLossA = 0.5,
                ReturnLossB = 3,
                ReturnLossOffsetVnaDb = 1.5,
                ReturnLossOffsetFilterDb = 4,
            },
        };

        var point = RfSampleCalculator.FromLegacyAdcReadings(1_000_000, 100, 200, profile,
            LegacyAdcMeasurementMode.ScalarTransmission);

        Assert.Equal(-21, point.TransmissionDb);
        Assert.Null(point.ReturnLossDb);
        Assert.Null(point.Swr);
        Assert.Null(point.PhaseDegrees);
        Assert.Null(point.ImpedanceOhms);
    }

    [Fact]
    public void Captured50OhmSampleProducesPassiveReflectionAndNearUnitySwr()
    {
        // Actual COM9 capture at 1 MHz with the user's 50-ohm termination: AF 00 C1 03.
        var point = RfSampleCalculator.FromLegacyAdcReadings(1_000_000, 175, 961, DeviceProfile.Vna65MHz);
        Assert.Equal(-36.203635986, point.ReturnLossDb!.Value, 8);
        Assert.InRange(point.Swr!.Value, 1.031, 1.032);
        Assert.InRange(point.ResistanceOhms!.Value, 51, 52);
        Assert.InRange(point.ReactanceOhms!.Value, 0, 1);
        Assert.Equal(175 * DeviceProfile.Vna65MHz.PhaseAdcDegreesPerCount, point.PhaseDegrees!.Value, 8);
    }

    [Theory]
    [InlineData(0, -18.5)]
    [InlineData(1, 5.66)]
    public void AdcAffineCorrectionPrecedesReferenceAndPolarity(int vnaType, double expectedSignedRl)
    {
        var profile = DeviceProfile.Vna65MHz with
        {
            LegacyVnaType = vnaType,
            MagnitudeAdcDbPerCount = 0.1,
            PhaseAdcDegreesPerCount = 0.2,
            LegacyCalibration = DeviceProfile.Vna65MHz.LegacyCalibration with
            {
                ReturnLossA = 0.5, ReturnLossB = 3, ReturnLossOffsetVnaDb = 1.5,
                PhaseA = 2, PhaseB = -5,
            },
        };
        // Magnitude=200*0.1*0.5+3=13; the RF path subtracts the offset after polarity.
        var point = RfSampleCalculator.FromLegacyAdcReadings(1_000_000, 100, 200, profile);
        Assert.Equal(expectedSignedRl, point.ReturnLossDb!.Value, 8);
        Assert.Equal(35, point.PhaseDegrees!.Value, 8);
    }
}
