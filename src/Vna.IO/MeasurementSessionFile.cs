using Vna.Localization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vna.Core;

namespace Vna.IO;

public static class MeasurementSessionFile
{
    public const int CurrentFormatVersion = 4;
    private const int LegacyFormatVersion = 1;
    private const int VersionTwoFormatVersion = 2;
    private const int PreviousFormatVersion = 3;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task SaveAsync(
        Stream destination,
        MeasurementSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(session);

        if (!destination.CanWrite)
            throw LocalizedErrors.Argument(nameof(destination), "StreamNotWritable");
        Validate(session);
        var current = UpgradeLegacyPhaseMetadata(session);
        Validate(current);
        await JsonSerializer.SerializeAsync(destination, current, Options, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<MeasurementSession> LoadAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
            throw LocalizedErrors.Argument(nameof(source), "StreamNotReadable");

        var session = await JsonSerializer.DeserializeAsync<MeasurementSession>(source, Options, cancellationToken)
            .ConfigureAwait(false) ?? throw LocalizedErrors.InvalidData("SessionEmpty");

        Validate(session);
        var upgraded = UpgradeLegacyPhaseMetadata(session);
        return session.FormatVersion == LegacyFormatVersion
            ? upgraded with { AnalysisMode = SessionAnalysisMode.Antenna }
            : upgraded;
    }

    private static MeasurementSession UpgradeLegacyPhaseMetadata(MeasurementSession session)
    {
        if (session.FormatVersion == PreviousFormatVersion)
            return session with { FormatVersion = CurrentFormatVersion };
        if (session.FormatVersion is not (LegacyFormatVersion or VersionTwoFormatVersion)) return session;

        var migratedPoints = session.Points.Select(point => point with
        {
            PhaseSignConfidence = point.PhaseDegrees is null
                ? PhaseSignConfidence.Missing
                : PhaseSignConfidence.Ambiguous,
            PhaseSignProvenance = PhaseSignProvenance.Unknown,
        }).ToArray();

        return session with
        {
            FormatVersion = CurrentFormatVersion,
            Points = migratedPoints,
        };
    }

    private static void Validate(MeasurementSession session)
    {
        if (session.FormatVersion is not (LegacyFormatVersion or VersionTwoFormatVersion or PreviousFormatVersion or CurrentFormatVersion))
            throw LocalizedErrors.InvalidData("SessionFormatUnsupported", session.FormatVersion);
        if (!Enum.IsDefined(session.AnalysisMode))
            throw LocalizedErrors.InvalidData("SessionAnalysisModeInvalid");
        if (string.IsNullOrWhiteSpace(session.ProfileId))
            throw LocalizedErrors.InvalidData("SessionProfileMissing");
        if (session.ProfileSnapshot is { } profileSnapshot && !IsValidProfileSnapshot(profileSnapshot, session.ProfileId))
            throw LocalizedErrors.InvalidData("SessionSnapshotInvalid");
        if (session.Sweep is null)
            throw LocalizedErrors.InvalidData("SessionSweepMissing");
        if (session.Points is null || session.Points.Count != session.Sweep.PointCount)
            throw LocalizedErrors.InvalidData("SessionPointCountMismatch");
        var previousFrequencyHz = -1L;
        for (var index = 0; index < session.Points.Count; index++)
        {
            var point = session.Points[index];
            if (point is null)
                throw LocalizedErrors.InvalidData("SessionPointEmpty", index);
            if (!Enum.IsDefined(point.Quality))
                throw LocalizedErrors.InvalidData("SessionPointQualityUnknown", index);
            if (!Enum.IsDefined(point.PhaseSignConfidence) || !Enum.IsDefined(point.PhaseSignProvenance))
                throw LocalizedErrors.InvalidData("SessionPointPhaseConfidenceUnknown", index);
            if (point.PhaseSignConfidence == PhaseSignConfidence.Verified && point.PhaseSignProvenance is
                PhaseSignProvenance.Unknown or PhaseSignProvenance.LegacyVna65Mhz or PhaseSignProvenance.ImportedUnverified)
                throw LocalizedErrors.InvalidData("SessionPointPhaseProvenanceMissing", index);
            if (point.PhaseDegrees is null && point.PhaseSignConfidence != PhaseSignConfidence.Missing)
                throw LocalizedErrors.InvalidData("SessionPointPhaseConfidenceMismatch", index);
            if (point.FrequencyHz <= previousFrequencyHz ||
                point.FrequencyHz < session.Sweep.StartFrequencyHz ||
                point.FrequencyHz > session.Sweep.StopFrequencyHz)
                throw LocalizedErrors.InvalidData("SessionPointFrequencyMismatch", index);
            previousFrequencyHz = point.FrequencyHz;
        }
    }

    private static bool IsValidProfileSnapshot(DeviceProfile profile, string profileId)
    {
        var calibration = profile.LegacyCalibration;
        var plotColors = profile.PlotColors;
        var generatorMemories = profile.GeneratorMemoryFrequenciesHz;
        var legacyBands = profile.LegacyBandPresets;
        if (!string.Equals(profile.Id, profileId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(profile.DisplayName) ||
            profile.MinimumFrequencyHz < 0 || profile.MaximumFrequencyHz <= profile.MinimumFrequencyHz ||
            profile.DefaultPointCount is < 2 or > 4000 || profile.DdsClockHz <= 0 ||
            profile.DdsPllPrescaler <= 0 || profile.StartPauseMilliseconds < 0 || profile.StepPauseMilliseconds < 0 ||
            !double.IsFinite(profile.ReferenceImpedanceOhms) || profile.ReferenceImpedanceOhms <= 0 ||
            !double.IsFinite(profile.MagnitudeAdcDbPerCount) || profile.MagnitudeAdcDbPerCount <= 0 ||
            !double.IsFinite(profile.PhaseAdcDegreesPerCount) || profile.PhaseAdcDegreesPerCount <= 0 ||
            calibration is null || plotColors is null || generatorMemories is null || legacyBands is null ||
            generatorMemories.Count is not (0 or 10) ||
            generatorMemories.Any(frequency => frequency < profile.MinimumFrequencyHz || frequency > profile.MaximumFrequencyHz) ||
            legacyBands.Any(band => band is null || string.IsNullOrWhiteSpace(band.Name) ||
                                    !Enum.IsDefined(band.Group) || band.StartFrequencyHz <= 0 ||
                                    band.StopFrequencyHz <= band.StartFrequencyHz))
            return false;

        return double.IsFinite(calibration.ReturnLossOffsetVnaDb) &&
               double.IsFinite(calibration.ReturnLossOffsetFilterDb) &&
               double.IsFinite(calibration.ReturnLossA) &&
               double.IsFinite(calibration.ReturnLossB) &&
               double.IsFinite(calibration.PhaseA) &&
               double.IsFinite(calibration.PhaseB) &&
               double.IsFinite(calibration.InputVelocityFactor) &&
               calibration.InputVelocityFactor is > 0 and <= 1 &&
               plotColors.Phase <= 0xFFFFFF &&
               plotColors.ReturnLoss <= 0xFFFFFF &&
               plotColors.Reactance <= 0xFFFFFF &&
               plotColors.Resistance <= 0xFFFFFF &&
               plotColors.Impedance <= 0xFFFFFF &&
               plotColors.Swr <= 0xFFFFFF &&
               plotColors.Background <= 0xFFFFFF;
    }
}
