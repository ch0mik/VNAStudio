using Vna.Localization;
using Vna.Core;

namespace Vna.IO;

public static class LegacyAnalyzIniProfileFactory
{
    public static DeviceProfile Create(LegacyAnalyzIniOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Profile);
        ArgumentNullException.ThrowIfNull(options.PortSetting);
        ArgumentNullException.ThrowIfNull(options.Parameters);
        ArgumentNullException.ThrowIfNull(options.Calibration);
        ArgumentNullException.ThrowIfNull(options.SpecialCalibration);
        ArgumentNullException.ThrowIfNull(options.GraphProperties);
        ArgumentNullException.ThrowIfNull(options.GeneratorMemory);
        ArgumentNullException.ThrowIfNull(options.BandLimits);

        var parameters = options.Parameters;
        var calibration = options.SpecialCalibration;
        if (options.PortSetting.VnaType is < 0 or > 1 || options.PortSetting.PortType is < 1 or > 2 ||
            options.PortSetting.Svna is < 0 or > 1)
            throw LocalizedErrors.InvalidData("IniDeviceTypeInvalid");
        if (string.IsNullOrWhiteSpace(options.Profile.Id) || string.IsNullOrWhiteSpace(options.Profile.DisplayName))
            throw LocalizedErrors.InvalidData("IniProfileNameRequired");
        if (parameters.FMin < 0 || parameters.FMax <= parameters.FMin)
            throw LocalizedErrors.InvalidData("IniFrequencyRangeInvalid");
        if (parameters.Champ is < 2 or > 4000)
            throw LocalizedErrors.InvalidData("IniPointCountInvalid");
        if (parameters.DdsClock <= 0 || parameters.DdsPll <= 0)
            throw LocalizedErrors.InvalidData("IniDdsInvalid");
        if (parameters.StartPause < 0 || parameters.StepPause < 0)
            throw LocalizedErrors.InvalidData("IniPausesInvalid");
        if (!double.IsFinite(calibration.ZValue) || calibration.ZValue <= 0 ||
            !double.IsFinite(calibration.AdcPhres) || calibration.AdcPhres <= 0 ||
            !double.IsFinite(calibration.AdcMagres) || calibration.AdcMagres <= 0)
            throw LocalizedErrors.InvalidData("IniAdcInvalid");
        if (!double.IsFinite(options.Calibration.RlOffsetVna) ||
            !double.IsFinite(options.Calibration.RlOffsetFilter) ||
            !double.IsFinite(calibration.RlA) || !double.IsFinite(calibration.RlB) ||
            !double.IsFinite(calibration.PhA) || !double.IsFinite(calibration.PhB) ||
            !double.IsFinite(parameters.InputVelFactor) || parameters.InputVelFactor is <= 0 or > 1)
            throw LocalizedErrors.InvalidData("IniCalibrationInvalid");
        var graph = options.GraphProperties;
        var generatorMemories = new[]
        {
            options.GeneratorMemory.F_M0, options.GeneratorMemory.F_M1, options.GeneratorMemory.F_M2,
            options.GeneratorMemory.F_M3, options.GeneratorMemory.F_M4, options.GeneratorMemory.F_M5,
            options.GeneratorMemory.F_M6, options.GeneratorMemory.F_M7, options.GeneratorMemory.F_M8,
            options.GeneratorMemory.F_M9,
        };
        var configuredGeneratorMemories = generatorMemories.Where(frequency => frequency > 0).ToArray();
        if (configuredGeneratorMemories.Length is not (0 or 10) ||
            configuredGeneratorMemories.Any(frequency => frequency < parameters.FMin || frequency > parameters.FMax))
            throw LocalizedErrors.InvalidData("IniGeneratorMemoryInvalid");
        var legacyBands = CreateBandPresets(options.BandLimits);
        if (legacyBands.Any(band => band.StartFrequencyHz <= 0 || band.StopFrequencyHz <= band.StartFrequencyHz))
            throw LocalizedErrors.InvalidData("IniBandRangeInvalid");
        if (new[] { graph.ColorS1, graph.ColorS2, graph.ColorS3, graph.ColorS4, graph.ColorS5, graph.ColorS6, graph.BackGround }
            .Any(color => color is < 0 or > 0xFFFFFF))
            throw LocalizedErrors.InvalidData("IniColorInvalid");

        return new DeviceProfile(
            Id: options.Profile.Id,
            DisplayName: options.Profile.DisplayName,
            MinimumFrequencyHz: parameters.FMin,
            MaximumFrequencyHz: parameters.FMax,
            DefaultPointCount: parameters.Champ,
            DdsClockHz: parameters.DdsClock,
            ReferenceImpedanceOhms: calibration.ZValue,
            MagnitudeAdcDbPerCount: calibration.AdcMagres,
            PhaseAdcDegreesPerCount: calibration.AdcPhres)
        {
            LegacyVnaType = options.PortSetting.VnaType,
            LegacySvna = options.PortSetting.Svna == 1,
            DdsPllPrescaler = parameters.DdsPll,
            StartPauseMilliseconds = parameters.StartPause,
            StepPauseMilliseconds = parameters.StepPause,
            LegacyCalibration = new LegacyCalibrationSettings(
                ReturnLossOffsetVnaDb: options.Calibration.RlOffsetVna,
                ReturnLossOffsetFilterDb: options.Calibration.RlOffsetFilter,
                ReturnLossA: calibration.RlA,
                ReturnLossB: calibration.RlB,
                PhaseA: calibration.PhA,
                PhaseB: calibration.PhB,
                InputVelocityFactor: parameters.InputVelFactor),
            PlotColors = new LegacyPlotColorSettings(
                Phase: (uint)graph.ColorS1,
                ReturnLoss: (uint)graph.ColorS2,
                Reactance: (uint)graph.ColorS3,
                Resistance: (uint)graph.ColorS4,
                Impedance: (uint)graph.ColorS5,
                Swr: (uint)graph.ColorS6,
                Background: (uint)graph.BackGround),
            GeneratorMemoryFrequenciesHz = configuredGeneratorMemories,
            LegacyBandPresets = legacyBands,
        };
    }

    private static AmateurBandPreset[] CreateBandPresets(LegacyBandLimitSection limits)
    {
        (string Label, AmateurBandGroup Group, long Start, long Stop)[] bands =
        [
            ("160 m", AmateurBandGroup.Hf, limits.F_M160s, limits.F_M160e),
            ("80 m", AmateurBandGroup.Hf, limits.F_M80s, limits.F_M80e),
            ("60 m", AmateurBandGroup.Hf, limits.F_M60s, limits.F_M60e),
            ("40 m", AmateurBandGroup.Hf, limits.F_M40s, limits.F_M40e),
            ("30 m", AmateurBandGroup.Hf, limits.F_M30s, limits.F_M30e),
            ("20 m", AmateurBandGroup.Hf, limits.F_M20s, limits.F_M20e),
            ("17 m", AmateurBandGroup.Hf, limits.F_M17s, limits.F_M17e),
            ("15 m", AmateurBandGroup.Hf, limits.F_M15s, limits.F_M15e),
            ("12 m", AmateurBandGroup.Hf, limits.F_M12s, limits.F_M12e),
            ("10 m", AmateurBandGroup.Hf, limits.F_M10s, limits.F_M10e),
            ("6 m", AmateurBandGroup.Vhf, limits.F_M6s, limits.F_M6e),
            ("2 m", AmateurBandGroup.Vhf, limits.F_M2s, limits.F_M2e),
            ("70 cm", AmateurBandGroup.Vhf, limits.F_M70cs, limits.F_M70ce),
        ];

        if (bands.Any(band => (band.Start == 0) != (band.Stop == 0)))
            throw LocalizedErrors.InvalidData("IniBandRangeInvalid");

        return bands
            .Where(band => band.Start != 0 || band.Stop != 0)
            .Select(band => new AmateurBandPreset(band.Label, band.Group, band.Start, band.Stop))
            .ToArray();
    }
}
