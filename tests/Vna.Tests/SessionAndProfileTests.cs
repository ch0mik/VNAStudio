using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vna.App.Plotting;
using Vna.Core;
using Vna.IO;
using Xunit;

namespace Vna.Tests;

public sealed class SessionAndProfileTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void IniSvnaFlagIsIndependentOfVnaType(int svna, bool expected)
    {
        var config = new ConfigurationBuilder()
            .AddIniFile(Path.Combine(AppContext.BaseDirectory, "Assets", "analyser306b-65mhz.ini"))
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PORTSETTING:VNA_type"] = "1", ["PORTSETTING:SVNA"] = svna.ToString(),
            })
            .Build();
        var profile = LegacyAnalyzIniProfileFactory.Create(config.Get<LegacyAnalyzIniOptions>()!);
        Assert.Equal(1, profile.LegacyVnaType);
        Assert.Equal(expected, profile.LegacySvna);
    }

    [Fact]
    public void BundledIniBindsThroughOptionsAndPreservesAllConstants()
    {
        var config = new ConfigurationBuilder()
            .AddIniFile(Path.Combine(AppContext.BaseDirectory, "Assets", "analyser306b-65mhz.ini"))
            .Build();
        var services = new ServiceCollection();
        services.Configure<LegacyAnalyzIniOptions>(config);
        using var provider = services.BuildServiceProvider();
        var profile = LegacyAnalyzIniProfileFactory.Create(provider.GetRequiredService<IOptions<LegacyAnalyzIniOptions>>().Value);
        var expected = DeviceProfile.Vna65MHz;
        Assert.Equal("VNA 1-65 MHz", profile.DisplayName);
        Assert.Equal(1_000_000, profile.MinimumFrequencyHz);
        Assert.Equal(65_000_000, profile.MaximumFrequencyHz);
        Assert.Equal(191_993_220, profile.DdsClockHz);
        Assert.Equal(649, profile.DefaultPointCount);
        Assert.Equal(0.058651026, profile.MagnitudeAdcDbPerCount, 9);
        Assert.Equal(0.175953079, profile.PhaseAdcDegreesPerCount, 9);
        Assert.Equal(expected with { GeneratorMemoryFrequenciesHz = profile.GeneratorMemoryFrequenciesHz }, profile);
        Assert.Equal(new long[]
        {
            3_600_000, 7_050_000, 10_500_000, 14_100_000, 18_050_000,
            21_050_000, 24_550_000, 27_195_000, 28_050_000, 29_050_000,
        }, profile.GeneratorMemoryFrequenciesHz);
        var palette = PlotPalette.FromProfile(profile);
        Assert.Equal("#FF0000", palette.GetColor(PlotTrace.Swr));
        Assert.Equal("#0000FF", palette.GetColor(PlotTrace.ReturnLoss));
        Assert.Equal("#D78502", palette.GetColor(PlotTrace.Phase));
        Assert.Equal("#19FF00", palette.GetColor(PlotTrace.Resistance));
        Assert.Equal("#E81AFC", palette.GetColor(PlotTrace.Impedance));
        Assert.Equal("#008000", palette.GetColor(PlotTrace.Reactance));
        Assert.Equal("#F0F0F0", palette.Background);
    }

    [Fact]
    public void ProfileAcceptsInclusiveIniFrequencyLimitsAndRejectsOutOfRangeSweeps()
    {
        new SweepSettings(1_000_000, 65_000_000, 649).ValidateAgainst(DeviceProfile.Vna65MHz);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SweepSettings(999_999, 65_000_000, 649).ValidateAgainst(DeviceProfile.Vna65MHz));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SweepSettings(1_000_000, 65_000_001, 649).ValidateAgainst(DeviceProfile.Vna65MHz));
    }

    [Fact]
    public void IniFactoryRejectsGeneratorMemoryOutsideProfileRange()
    {
        var config = new ConfigurationBuilder()
            .AddIniFile(Path.Combine(AppContext.BaseDirectory, "Assets", "analyser306b-65mhz.ini"))
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MEMORY:F_M0"] = "100" })
            .Build();
        Assert.Throws<InvalidDataException>(() => LegacyAnalyzIniProfileFactory.Create(config.Get<LegacyAnalyzIniOptions>()!));
    }

    [Fact]
    public void VelocityFactorComesFromParametersSectionAsInOriginalIni()
    {
        var config = new ConfigurationBuilder()
            .AddIniFile(Path.Combine(AppContext.BaseDirectory, "Assets", "analyser306b-65mhz.ini"))
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PARAMETERS:InputVelFactor"] = "0.81" })
            .Build();
        Assert.Equal(0.81, LegacyAnalyzIniProfileFactory.Create(config.Get<LegacyAnalyzIniOptions>()!).LegacyCalibration.InputVelocityFactor);
    }

    [Fact]
    public void LegacyBandLimitsBindOptionalRangePairsFromAnalyzIni()
    {
        var config = new ConfigurationBuilder()
            .AddIniFile(Path.Combine(AppContext.BaseDirectory, "Assets", "analyser306b-65mhz.ini"))
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BANDS_LIMIT:F_M80s"] = "3500000",
                ["BANDS_LIMIT:F_M80e"] = "3800000",
                ["BANDS_LIMIT:F_M2s"] = "144000000",
                ["BANDS_LIMIT:F_M2e"] = "146000000",
            })
            .Build();

        var profile = LegacyAnalyzIniProfileFactory.Create(config.Get<LegacyAnalyzIniOptions>()!);

        Assert.Collection(profile.LegacyBandPresets,
            hf =>
            {
                Assert.Equal("80 m", hf.Name);
                Assert.Equal(AmateurBandGroup.Hf, hf.Group);
                Assert.Equal(3_500_000, hf.StartFrequencyHz);
                Assert.Equal(3_800_000, hf.StopFrequencyHz);
            },
            vhf =>
            {
                Assert.Equal("2 m", vhf.Name);
                Assert.Equal(AmateurBandGroup.Vhf, vhf.Group);
                Assert.Equal(144_000_000, vhf.StartFrequencyHz);
            });
    }

    [Fact]
    public void LegacyBandLimitRejectsIncompleteRangePair()
    {
        var config = new ConfigurationBuilder()
            .AddIniFile(Path.Combine(AppContext.BaseDirectory, "Assets", "analyser306b-65mhz.ini"))
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BANDS_LIMIT:F_M80s"] = "3500000" })
            .Build();

        Assert.Throws<InvalidDataException>(() => LegacyAnalyzIniProfileFactory.Create(config.Get<LegacyAnalyzIniOptions>()!));
    }

    [Fact]
    public void ScaledReadingsUseIniAffineCalibrationBeforeRfDerivation()
    {
        var profile = DeviceProfile.Vna65MHz with
        {
            LegacyCalibration = DeviceProfile.Vna65MHz.LegacyCalibration with
            {
                ReturnLossA = 2,
                ReturnLossB = -3,
                PhaseA = 3,
                PhaseB = 4,
            },
        };

        var point = RfSampleCalculator.FromScaledReadings(14_100_000, -1.2, 9.5, profile);

        Assert.Equal(2 * -1.2 - 3, point.ReturnLossDb!.Value, 12);
        Assert.Equal(3 * 9.5 + 4, point.PhaseDegrees!.Value, 12);
        Assert.NotNull(point.Swr);
        Assert.NotNull(point.ImpedanceOhms);
    }

    [Fact]
    public async Task CsvExportUsesInvariantDecimalAndExplicitUnitsAndLeavesMissingValuesBlank()
    {
        var session = TestData.Session();
        var points = session.Points.ToArray();
        points[0] = points[0] with { Swr = 1.25, ReactanceOhms = null };
        using var stream = new MemoryStream();
        await MeasurementCsvFile.SaveAsync(stream, session with { Points = points }, TestContext.Current.CancellationToken);
        var csv = Encoding.UTF8.GetString(stream.ToArray());
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Frequency_Hz,SWR,Impedance_Ohm,ReturnLoss_dB,Phase_Degrees,Resistance_Ohm,Reactance_Ohm,Quality,Transmission_dB,AnalysisMode", lines[0]);
        Assert.StartsWith("1000000,1.25,", lines[1], StringComparison.Ordinal);
        Assert.Contains(",,Valid", lines[1], StringComparison.Ordinal);
        Assert.DoesNotContain("1,25", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CsvRoundTripPreservesAllStudioColumnsAndSampleQuality()
    {
        var session = TestData.Session();
        var points = session.Points.ToArray();
        points[1] = points[1] with { ReactanceOhms = null, Quality = SampleQuality.Missing };
        session = session with { Points = points };
        using var stream = new MemoryStream();

        await MeasurementCsvFile.SaveAsync(stream, session, TestContext.Current.CancellationToken);
        stream.Position = 0;
        var loaded = await MeasurementCsvFile.LoadAsync(stream, DeviceProfile.Vna65MHz, TestContext.Current.CancellationToken);

        Assert.Equal("VNA Studio CSV", loaded.DataSource);
        Assert.Equal(session.Points.Select(point => point with
        {
            PhaseSignConfidence = point.PhaseDegrees is null ? PhaseSignConfidence.Missing : PhaseSignConfidence.Ambiguous,
            PhaseSignProvenance = point.PhaseDegrees is null ? PhaseSignProvenance.Unknown : PhaseSignProvenance.ImportedUnverified,
        }).ToArray(), loaded.Points.ToArray());
        Assert.Equal(session.Sweep, loaded.Sweep);
    }

    [Fact]
    public async Task FilterCsvRoundTripPreservesTransmissionAndAnalysisMode()
    {
        var original = TestData.Session() with
        {
            AnalysisMode = SessionAnalysisMode.Filter,
            Points = TestData.Session().Points.Select((point, index) => point with
            {
                Swr = null,
                ImpedanceOhms = null,
                ReturnLossDb = null,
                PhaseDegrees = null,
                ResistanceOhms = null,
                ReactanceOhms = null,
                PhaseSignConfidence = PhaseSignConfidence.Missing,
                PhaseSignProvenance = PhaseSignProvenance.Unknown,
                TransmissionDb = -1.5 - index,
            }).ToArray(),
        };
        using var stream = new MemoryStream();

        await MeasurementCsvFile.SaveAsync(stream, original, TestContext.Current.CancellationToken);
        stream.Position = 0;
        var imported = await MeasurementCsvFile.LoadAsync(stream, DeviceProfile.Vna65MHz, TestContext.Current.CancellationToken);

        Assert.Equal(SessionAnalysisMode.Filter, imported.AnalysisMode);
        Assert.Equal(original.Points.Select(point => point.TransmissionDb), imported.Points.Select(point => point.TransmissionDb));
        Assert.All(imported.Points, point => Assert.Null(point.ReturnLossDb));
    }

    [Fact]
    public async Task PreviousStudioCsvHeaderStillImportsAsAntenna()
    {
        const string csv = "Frequency_Hz,SWR,Impedance_Ohm,ReturnLoss_dB,Phase_Degrees,Resistance_Ohm,Reactance_Ohm,Quality\n" +
                           "1000000,1.2,52,-20,0,51,1,Valid\n" +
                           "2000000,1.3,53,-18,1,52,2,Valid\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var loaded = await MeasurementCsvFile.LoadAsync(stream, DeviceProfile.Vna65MHz, TestContext.Current.CancellationToken);

        Assert.Equal(SessionAnalysisMode.Antenna, loaded.AnalysisMode);
        Assert.Equal("VNA Studio CSV", loaded.DataSource);
        Assert.All(loaded.Points, point => Assert.Null(point.TransmissionDb));
    }

    [Theory]
    [InlineData("\"Frequency(Hz)\",\"Return Loss(dB)\"\n1000000,-10\n2000000,-12\n", false)]
    [InlineData("\"Frequency(Hz)\",\"Phase(deg)\",\"Return Loss(dB)\"\n1000000,10,-10\n2000000,20,-12\n", true)]
    public async Task ImportsIdentifiedLegacyAnalyserCsvColumnsWithoutInventingMissingValues(string csv, bool hasPhase)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var session = await MeasurementCsvFile.LoadAsync(stream, DeviceProfile.Vna65MHz, TestContext.Current.CancellationToken);

        Assert.Equal("Analyser 306b antenna CSV", session.DataSource);
        Assert.Equal(1_000_000, session.Sweep.StartFrequencyHz);
        Assert.Equal(2_000_000, session.Sweep.StopFrequencyHz);
        Assert.Equal(-10, session.Points[0].ReturnLossDb);
        Assert.Equal(hasPhase ? 10 : null, session.Points[0].PhaseDegrees);
        Assert.Null(session.Points[0].Swr);
        Assert.Null(session.Points[0].ImpedanceOhms);
        Assert.Equal(hasPhase ? PhaseSignConfidence.Ambiguous : PhaseSignConfidence.Missing, session.Points[0].PhaseSignConfidence);
        Assert.Equal(hasPhase ? PhaseSignProvenance.ImportedUnverified : PhaseSignProvenance.Unknown, session.Points[0].PhaseSignProvenance);
    }
    [Fact]
    public async Task SessionRoundTripPreservesNullableSamplesProfileAndPalette()
    {
        var original = TestData.Session();
        original = original with { Points = original.Points.Select(p => p with { ReactanceOhms = null }).ToArray() };
        using var stream = new MemoryStream();
        await MeasurementSessionFile.SaveAsync(stream, original, TestContext.Current.CancellationToken);
        stream.Position = 0;
        var loaded = await MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken);
        Assert.Equal(MeasurementSessionFile.CurrentFormatVersion, loaded.FormatVersion);
        Assert.Equal(SessionAnalysisMode.Antenna, loaded.AnalysisMode);
        Assert.Equal(original.ProfileSnapshot! with
        {
            GeneratorMemoryFrequenciesHz = loaded.ProfileSnapshot!.GeneratorMemoryFrequenciesHz,
            LegacyBandPresets = loaded.ProfileSnapshot.LegacyBandPresets,
        }, loaded.ProfileSnapshot);
        Assert.Equal(original.ProfileSnapshot!.GeneratorMemoryFrequenciesHz, loaded.ProfileSnapshot!.GeneratorMemoryFrequenciesHz);
        Assert.Equal(original.ProfileSnapshot!.LegacyBandPresets, loaded.ProfileSnapshot!.LegacyBandPresets);
        Assert.Equal(original.Sweep, loaded.Sweep);
        Assert.Equal(original.Points.ToArray(), loaded.Points.ToArray());
        Assert.Equal(original.CapturedAtUtc, loaded.CapturedAtUtc);
        Assert.Equal(original.DataSource, loaded.DataSource);
    }

    [Fact]
    public async Task VersionThreeSessionRoundTripPreservesVerifiedPhaseEvidence()
    {
        var original = TestData.Session() with
        {
            FormatVersion = 3,
            Points = TestData.Session().Points.Select(point => point with
            {
                PhaseSignConfidence = PhaseSignConfidence.Verified,
                PhaseSignProvenance = PhaseSignProvenance.ReferenceLoadValidation,
            }).ToArray(),
        };
        using var stream = new MemoryStream();
        await MeasurementSessionFile.SaveAsync(stream, original, TestContext.Current.CancellationToken);
        stream.Position = 0;
        var loaded = await MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal(original.Points.ToArray(), loaded.Points.ToArray());
        Assert.Equal(MeasurementSessionFile.CurrentFormatVersion, loaded.FormatVersion);
    }

    [Fact]
    public async Task VersionThreeSessionMigratesToFourAndPreservesScalarTransmission()
    {
        var original = TestData.Session() with
        {
            FormatVersion = 3,
            AnalysisMode = SessionAnalysisMode.Filter,
            Points = TestData.Session().Points.Select((point, index) => point with
            {
                Swr = null,
                ReturnLossDb = null,
                PhaseDegrees = null,
                PhaseSignConfidence = PhaseSignConfidence.Missing,
                PhaseSignProvenance = PhaseSignProvenance.Unknown,
                ImpedanceOhms = null,
                ResistanceOhms = null,
                ReactanceOhms = null,
                TransmissionDb = index == 0 ? -4.25 : null,
            }).ToArray(),
        };
        using var stream = new MemoryStream();
        await MeasurementSessionFile.SaveAsync(stream, original, TestContext.Current.CancellationToken);
        stream.Position = 0;
        var loaded = await MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal(4, loaded.FormatVersion);
        Assert.Equal(SessionAnalysisMode.Filter, loaded.AnalysisMode);
        Assert.Equal(-4.25, loaded.Points[0].TransmissionDb);
        Assert.Null(loaded.Points[0].ReturnLossDb);
    }

    [Theory]
    [InlineData(PhaseSignProvenance.Unknown)]
    [InlineData(PhaseSignProvenance.LegacyVna65Mhz)]
    [InlineData(PhaseSignProvenance.ImportedUnverified)]
    public async Task SessionRejectsVerifiedPhaseWithoutTrustedEvidence(PhaseSignProvenance provenance)
    {
        var point = TestData.Point(1_000_000) with
        {
            PhaseSignConfidence = PhaseSignConfidence.Verified,
            PhaseSignProvenance = provenance,
        };
        var session = TestData.Session() with { FormatVersion = 3, Points = [point, .. TestData.Session().Points.Skip(1)] };
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<InvalidDataException>(() => MeasurementSessionFile.SaveAsync(stream, session, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LegacyVersionOneSessionMigratesWithConservativePhaseConfidence()
    {
        var legacy = TestData.Session() with { FormatVersion = 1 };
        var json = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(legacy))!.AsObject();
        json.Remove(nameof(MeasurementSession.AnalysisMode));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json.ToJsonString()));
        var migrated = await MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken);
        Assert.Equal(MeasurementSessionFile.CurrentFormatVersion, migrated.FormatVersion);
        Assert.Equal(SessionAnalysisMode.Antenna, migrated.AnalysisMode);
        Assert.All(migrated.Points, point =>
        {
            Assert.Equal(PhaseSignConfidence.Ambiguous, point.PhaseSignConfidence);
            Assert.Equal(PhaseSignProvenance.Unknown, point.PhaseSignProvenance);
        });
    }

    [Fact]
    public async Task LegacyVersionTwoWithoutProfileSnapshotKeepsUnknownImpedanceReference()
    {
        var legacy = TestData.Session() with { FormatVersion = 2, ProfileSnapshot = null };
        using var stream = new MemoryStream();
        await MeasurementSessionFile.SaveAsync(stream, legacy, TestContext.Current.CancellationToken);
        stream.Position = 0;
        var migrated = await MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal(MeasurementSessionFile.CurrentFormatVersion, migrated.FormatVersion);
        Assert.Null(migrated.ProfileSnapshot);
        Assert.All(migrated.Points, point => Assert.Equal(PhaseSignConfidence.Ambiguous, point.PhaseSignConfidence));
    }

    [Fact]
    public async Task VersionTwoSessionPreservesAnalysisMode()
    {
        var session = TestData.Session() with { FormatVersion = 2, AnalysisMode = SessionAnalysisMode.Filter };
        using var stream = new MemoryStream();
        await MeasurementSessionFile.SaveAsync(stream, session, TestContext.Current.CancellationToken);
        stream.Position = 0;
        var loaded = await MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken);
        Assert.Equal(SessionAnalysisMode.Filter, loaded.AnalysisMode);
    }

    [Theory]
    [InlineData("dds")]
    [InlineData("range")]
    [InlineData("calibration")]
    [InlineData("color")]
    [InlineData("band")]
    public async Task SessionRejectsCorruptProfileSnapshot(string field)
    {
        var profile = DeviceProfile.Vna65MHz;
        profile = field switch
        {
            "dds" => profile with { DdsClockHz = -1 },
            "range" => profile with { MaximumFrequencyHz = 0 },
            "calibration" => profile with { LegacyCalibration = profile.LegacyCalibration with { InputVelocityFactor = 2 } },
            "color" => profile with { PlotColors = profile.PlotColors with { Swr = uint.MaxValue } },
            "band" => profile with { LegacyBandPresets = [new AmateurBandPreset("bad", AmateurBandGroup.Hf, 4_000_000, 3_000_000)] },
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => MeasurementSessionFile.SaveAsync(stream, TestData.Session(profile), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SessionRejectsUnknownVersionAndMalformedJson()
    {
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => MeasurementSessionFile.SaveAsync(output, TestData.Session() with { FormatVersion = 999 }, TestContext.Current.CancellationToken));
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("{invalid"));
        await Assert.ThrowsAsync<JsonException>(() => MeasurementSessionFile.LoadAsync(input, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SavingLegacySessionWithNullPointListReturnsInvalidDataInsteadOfNullReference()
    {
        var malformed = TestData.Session() with { FormatVersion = 2, Points = null! };
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() => MeasurementSessionFile.SaveAsync(output, malformed, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SessionLoadRejectsPointFrequencyOutsideSweepRange()
    {
        var session = TestData.Session();
        var points = session.Points.ToArray();
        points[2] = points[2] with { FrequencyHz = session.Sweep.StopFrequencyHz + 1 };
        var corrupted = session with { Points = points };
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(corrupted)));

        await Assert.ThrowsAsync<InvalidDataException>(() => MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SessionLoadObservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(TestData.Session())));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MeasurementSessionFile.LoadAsync(stream, cancellation.Token));
    }

    [Fact]
    public async Task DocumentedSessionExampleLoadsSuccessfully()
    {
        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", "demo.vnasession.json"));
        var session = await MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal("iw3hev-65mhz", session.ProfileId);
        Assert.Equal(2, session.Points.Count);
        Assert.Equal(65_000_000, session.Points[^1].FrequencyHz);
        Assert.Equal(0.66, session.ProfileSnapshot!.LegacyCalibration.InputVelocityFactor);
        foreach (var point in session.Points)
        {
            var calculated = RfSampleCalculator.FromReturnLossAndPhase(
                point.FrequencyHz, point.ReturnLossDb!.Value, point.PhaseDegrees!.Value, session.ProfileSnapshot);
            Assert.Equal(calculated.Swr!.Value, point.Swr!.Value, 8);
            Assert.Equal(calculated.ImpedanceOhms!.Value, point.ImpedanceOhms!.Value, 6);
            Assert.Equal(calculated.ResistanceOhms!.Value, point.ResistanceOhms!.Value, 6);
            Assert.Equal(calculated.ReactanceOhms!.Value, point.ReactanceOhms!.Value, 6);
        }
    }

    [Fact]
    public async Task SessionOperationsObserveCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var stream = new MemoryStream();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MeasurementSessionFile.SaveAsync(stream, TestData.Session(), cancellation.Token));
    }
}





