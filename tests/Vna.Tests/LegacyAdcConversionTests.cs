using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class LegacyAdcConversionTests
{
    [Fact]
    public void ScalarTransmissionUsesFilterOffsetAndDoesNotInventReflectionValues()
    {
        var profile = Vna65MHzDeviceProfile.Default with
        {
            LegacyVnaType = 0,
            MagnitudeAdcDbPerCount = 0.1,
            LegacyCalibration = Vna65MHzDeviceProfile.Default.LegacyCalibration with
            {
                ReturnLossA = 0.5,
                ReturnLossB = 3,
                ReturnLossOffsetVnaDb = 1.5,
                ReturnLossOffsetFilterDb = 4,
            },
        };

        var settings = Vna65MHzProtocolSettings.FromLegacyProfile(profile);
        var point = Vna65MHzRfConverter.FromAdcReadings(1_000_000, 100, 200, settings,
            MeasurementAcquisitionMode.ScalarTransmission);

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
        var point = Vna65MHzRfConverter.FromAdcReadings(1_000_000, 175, 961,
            Vna65MHzProtocolSettings.FromLegacyProfile(Vna65MHzDeviceProfile.Default));
        Assert.Equal(-36.203635986, point.ReturnLossDb!.Value, 8);
        Assert.InRange(point.Swr!.Value, 1.031, 1.032);
        Assert.InRange(point.ResistanceOhms!.Value, 51, 52);
        Assert.InRange(Math.Abs(point.ReactanceOhms!.Value), 0, 1);
        Assert.Equal(175 * Vna65MHzDeviceProfile.Default.PhaseAdcDegreesPerCount, point.PhaseDegrees!.Value, 8);
    }

    [Fact]
    public void NonFiniteReflectionMagnitudeReturnsAnInvalidSample()
    {
        var point = RfSampleCalculator.FromReturnLossAndPhase(1_000_000, 10_000, 30, 50);

        Assert.Equal(SampleQuality.Invalid, point.Quality);
        Assert.Null(point.Swr);
        Assert.Null(point.ReturnLossDb);
        Assert.Null(point.PhaseDegrees);
        Assert.Null(point.ImpedanceOhms);
        Assert.Null(point.ResistanceOhms);
        Assert.Null(point.ReactanceOhms);
        Assert.Null(point.TransmissionDb);
    }

    [Theory]
    [InlineData(0, -18.5)]
    [InlineData(1, 5.66)]
    public void AdcAffineCorrectionPrecedesReferenceAndPolarity(int vnaType, double expectedSignedRl)
    {
        var profile = Vna65MHzDeviceProfile.Default with
        {
            LegacyVnaType = vnaType,
            MagnitudeAdcDbPerCount = 0.1,
            PhaseAdcDegreesPerCount = 0.2,
            LegacyCalibration = Vna65MHzDeviceProfile.Default.LegacyCalibration with
            {
                ReturnLossA = 0.5, ReturnLossB = 3, ReturnLossOffsetVnaDb = 1.5,
                PhaseA = 2, PhaseB = -5,
            },
        };
        // Magnitude=200*0.1*0.5+3=13; the RF path subtracts the offset after polarity.
        var point = Vna65MHzRfConverter.FromAdcReadings(1_000_000, 100, 200,
            Vna65MHzProtocolSettings.FromLegacyProfile(profile));
        Assert.Equal(expectedSignedRl, point.ReturnLossDb!.Value, 8);
        Assert.Equal(35, point.PhaseDegrees!.Value, 8);
    }
}
