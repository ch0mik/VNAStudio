using Vna.Localization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vna.Core;

namespace Vna.IO;

public static class MeasurementSessionFile
{
    public const int CurrentFormatVersion = 6;
    private const int LegacyFormatVersion = 1;
    private const int VersionTwoFormatVersion = 2;
    private const int VersionThreeFormatVersion = 3;
    private const int VersionFourFormatVersion = 4;
    private const int VersionFiveFormatVersion = 5;

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
        var current = UpgradeSessionMetadata(session);
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
        var upgraded = UpgradeSessionMetadata(session);
        return session.FormatVersion == LegacyFormatVersion
            ? upgraded with { AnalysisMode = SessionAnalysisMode.Antenna }
            : upgraded;
    }

    private static MeasurementSession UpgradeSessionMetadata(MeasurementSession session)
    {
        var upgraded = session;
        if (session.FormatVersion is VersionThreeFormatVersion or VersionFourFormatVersion or VersionFiveFormatVersion)
        {
            upgraded = session with { FormatVersion = CurrentFormatVersion };
        }
        else if (session.FormatVersion is LegacyFormatVersion or VersionTwoFormatVersion)
        {
            var migratedPoints = session.Points.Select(point => point with
            {
                PhaseSignConfidence = point.PhaseDegrees is null
                    ? PhaseSignConfidence.Missing
                    : PhaseSignConfidence.Ambiguous,
                PhaseSignProvenance = PhaseSignProvenance.Unknown,
            }).ToArray();

            upgraded = session with
            {
                FormatVersion = CurrentFormatVersion,
                Points = migratedPoints,
            };
        }

        if (session.FormatVersion <= VersionFourFormatVersion && string.IsNullOrWhiteSpace(upgraded.PluginId) &&
            string.Equals(upgraded.ProfileId, LegacyDevicePluginIds.Vna65MHzProfile, StringComparison.Ordinal) &&
            string.Equals(upgraded.DataSource, "hardware", StringComparison.OrdinalIgnoreCase))
        {
            upgraded = upgraded with { PluginId = LegacyDevicePluginIds.Vna65MHz };
        }

        if (upgraded.ProfileDescriptorSnapshot is null && upgraded.ProfileSnapshot is { } legacyProfile)
            upgraded = upgraded with { ProfileDescriptorSnapshot = CreateProfileDescriptorSnapshot(legacyProfile) };

        return upgraded;
    }

    private static void Validate(MeasurementSession session)
    {
        if (session.FormatVersion is not (LegacyFormatVersion or VersionTwoFormatVersion or VersionThreeFormatVersion or
            VersionFourFormatVersion or VersionFiveFormatVersion or CurrentFormatVersion))
            throw LocalizedErrors.InvalidData("SessionFormatUnsupported", session.FormatVersion);
        if (!Enum.IsDefined(session.AnalysisMode))
            throw LocalizedErrors.InvalidData("SessionAnalysisModeInvalid");
        if (!Enum.IsDefined(session.FrequencyGridConvention))
            throw LocalizedErrors.InvalidData("SessionFrequencyGridConventionInvalid");
        if (!Enum.IsDefined(session.MeasurementDataKind))
            throw LocalizedErrors.InvalidData("SessionMeasurementDataKindInvalid");
        if ((session.MeasurementDataKind == MeasurementDataKind.ScalarTransmissionMagnitude &&
             session.AnalysisMode != SessionAnalysisMode.Filter) ||
            (session.MeasurementDataKind == MeasurementDataKind.ReflectionDerived &&
             session.AnalysisMode == SessionAnalysisMode.Filter))
            throw LocalizedErrors.InvalidData("SessionMeasurementDataKindModeMismatch");
        if (string.IsNullOrWhiteSpace(session.ProfileId))
            throw LocalizedErrors.InvalidData("SessionProfileMissing");
        if (session.PluginId is not null &&
            (string.IsNullOrWhiteSpace(session.PluginId) || session.PluginId.Trim() != session.PluginId))
            throw LocalizedErrors.InvalidData("SessionPluginIdInvalid");
        if (session.ProfileSnapshot is { } profileSnapshot && !IsValidProfileSnapshot(profileSnapshot, session.ProfileId))
            throw LocalizedErrors.InvalidData("SessionSnapshotInvalid");
        if (session.ProfileDescriptorSnapshot is { } descriptorSnapshot &&
            !IsValidProfileDescriptorSnapshot(descriptorSnapshot, session.ProfileId))
            throw LocalizedErrors.InvalidData("SessionDescriptorSnapshotInvalid");
        if (session.CalibrationSnapshot is { } calibrationSnapshot && !IsValidCalibrationSnapshot(calibrationSnapshot))
            throw LocalizedErrors.InvalidData("SessionCalibrationSnapshotInvalid");
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
            if (point.Quality == SampleQuality.Valid && session.MeasurementDataKind == MeasurementDataKind.ScalarTransmissionMagnitude &&
                (point.TransmissionDb is null || point.Swr is not null || point.ReturnLossDb is not null ||
                 point.PhaseDegrees is not null || point.ImpedanceOhms is not null ||
                 point.ResistanceOhms is not null || point.ReactanceOhms is not null))
                throw LocalizedErrors.InvalidData("SessionScalarDataShapeInvalid", index);
            if (point.Quality == SampleQuality.Valid && session.MeasurementDataKind == MeasurementDataKind.ReflectionDerived &&
                point.TransmissionDb is not null)
                throw LocalizedErrors.InvalidData("SessionReflectionDataShapeInvalid", index);
            if (point.Quality == SampleQuality.Valid && session.MeasurementDataKind == MeasurementDataKind.ReflectionDerived &&
                point.Swr is null && point.ReturnLossDb is null && point.PhaseDegrees is null &&
                point.ImpedanceOhms is null && point.ResistanceOhms is null && point.ReactanceOhms is null)
                throw LocalizedErrors.InvalidData("SessionReflectionDataMissing", index);
            if (point.FrequencyHz <= previousFrequencyHz ||
                point.FrequencyHz < session.Sweep.StartFrequencyHz ||
                point.FrequencyHz > session.Sweep.StopFrequencyHz)
                throw LocalizedErrors.InvalidData("SessionPointFrequencyMismatch", index);
            previousFrequencyHz = point.FrequencyHz;
        }
    }

    private static bool IsValidCalibrationSnapshot(MeasurementCalibrationSnapshot snapshot)
    {
        if (snapshot.SchemaVersion != MeasurementCalibrationSnapshot.CurrentSchemaVersion ||
            snapshot.Adjustments is null || snapshot.Adjustments.Count != Enum.GetValues<MeasurementAdjustmentComponent>().Length)
            return false;

        var seenComponents = new HashSet<MeasurementAdjustmentComponent>();
        foreach (var adjustment in snapshot.Adjustments)
        {
            if (adjustment is null || !Enum.IsDefined(adjustment.Component) ||
                !Enum.IsDefined(adjustment.Status) || !Enum.IsDefined(adjustment.Origin) ||
                !Enum.IsDefined(adjustment.Evidence) || !seenComponents.Add(adjustment.Component) ||
                adjustment.Parameters is null)
                return false;

            var seenParameters = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in adjustment.Parameters)
            {
                if (parameter is null || string.IsNullOrWhiteSpace(parameter.Name) ||
                    string.IsNullOrWhiteSpace(parameter.Unit) || !double.IsFinite(parameter.Value) ||
                    !seenParameters.Add(parameter.Name))
                    return false;
                if (parameter.Uncertainty is { } uncertainty &&
                    (!double.IsFinite(uncertainty.StandardUncertainty) || uncertainty.StandardUncertainty < 0 ||
                     string.IsNullOrWhiteSpace(uncertainty.Method)))
                    return false;
            }

            if (adjustment.Status == MeasurementAdjustmentStatus.Applied &&
                adjustment.Evidence == MeasurementAdjustmentEvidence.KnownAbsent)
                return false;
            if (adjustment.Status == MeasurementAdjustmentStatus.NotApplied && adjustment.Parameters.Count != 0)
                return false;
        }

        return seenComponents.Count == Enum.GetValues<MeasurementAdjustmentComponent>().Length;
    }

    private static bool IsValidProfileDescriptorSnapshot(DeviceProfileDescriptor profile, string profileId)
    {
        var capabilities = profile.Capabilities;
        var connectors = profile.Connectors;
        var frequencies = profile.FrequencyPresetsHz;
        var presentation = profile.Presentation;
        if (!string.Equals(profile.Id, profileId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(profile.DisplayName) || profile.SchemaVersion < 1 ||
            profile.MinimumFrequencyHz < 0 || profile.MaximumFrequencyHz <= profile.MinimumFrequencyHz ||
            profile.DefaultPointCount is < 2 or > 4000 ||
            profile.ReferenceImpedanceOhms is { } reference && (!double.IsFinite(reference) || reference <= 0) ||
            capabilities is null || connectors is null || frequencies is null || presentation is null ||
            frequencies.Any(frequency => frequency < profile.MinimumFrequencyHz || frequency > profile.MaximumFrequencyHz) ||
            capabilities.Any(capability => capability is null || string.IsNullOrWhiteSpace(capability.Id) ||
                                            !Enum.IsDefined(capability.Kind) || !Enum.IsDefined(capability.Availability) ||
                                            !Enum.IsDefined(capability.Evidence)) ||
            capabilities.Select(capability => capability.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != capabilities.Count ||
            connectors.Any(connector => connector is null || string.IsNullOrWhiteSpace(connector.Id) ||
                                        string.IsNullOrWhiteSpace(connector.Label) || !Enum.IsDefined(connector.Role) ||
                                        !Enum.IsDefined(connector.LabelEvidence) || !Enum.IsDefined(connector.RoleEvidence)) ||
            connectors.Select(connector => connector.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != connectors.Count ||
            presentation.FrequencyBands is null || presentation.TraceColors is null ||
            presentation.DefaultCableVelocityFactor is { } velocityFactor &&
            (!double.IsFinite(velocityFactor) || velocityFactor is <= 0 or > 1) ||
            presentation.FrequencyBands.Any(band => band is null || string.IsNullOrWhiteSpace(band.Name) ||
                                                    !Enum.IsDefined(band.Group) || band.StartFrequencyHz <= 0 ||
                                                    band.StopFrequencyHz <= band.StartFrequencyHz) ||
            presentation.TraceColors.Any(pair => string.IsNullOrWhiteSpace(pair.Key) ||
                pair.Key.Trim() != pair.Key || !IsRgbHexColor(pair.Value)) ||
            presentation.TraceColors.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != presentation.TraceColors.Count)
            return false;

        return true;
    }

    private static bool IsRgbHexColor(string? value) => value is { Length: 7 } && value[0] == '#' &&
        uint.TryParse(value.AsSpan(1), System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out _);

    private static DeviceProfileDescriptor CreateProfileDescriptorSnapshot(DeviceProfile profile)
    {
        static string ToRgbHex(uint colorRef) =>
            $"#{(byte)colorRef:X2}{(byte)(colorRef >> 8):X2}{(byte)(colorRef >> 16):X2}";

        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Phase"] = ToRgbHex(profile.PlotColors.Phase),
            ["ReturnLoss"] = ToRgbHex(profile.PlotColors.ReturnLoss),
            ["Reactance"] = ToRgbHex(profile.PlotColors.Reactance),
            ["Resistance"] = ToRgbHex(profile.PlotColors.Resistance),
            ["Impedance"] = ToRgbHex(profile.PlotColors.Impedance),
            ["Swr"] = ToRgbHex(profile.PlotColors.Swr),
            ["Background"] = ToRgbHex(profile.PlotColors.Background),
        };

        return new DeviceProfileDescriptor(profile.Id, profile.DisplayName, 1,
            profile.MinimumFrequencyHz, profile.MaximumFrequencyHz, profile.DefaultPointCount,
            profile.ReferenceImpedanceOhms, Array.AsReadOnly(profile.Capabilities.ToArray()),
            Array.AsReadOnly(profile.Connectors.ToArray()))
        {
            FrequencyPresetsHz = Array.AsReadOnly(profile.GeneratorMemoryFrequenciesHz.ToArray()),
            Presentation = new DeviceProfilePresentationDescriptor(
                Array.AsReadOnly(profile.LegacyBandPresets.ToArray()),
                new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(colors),
                profile.LegacyCalibration.InputVelocityFactor),
        };
    }

    private static bool IsValidProfileSnapshot(DeviceProfile profile, string profileId)
    {
        var calibration = profile.LegacyCalibration;
        var plotColors = profile.PlotColors;
        var generatorMemories = profile.GeneratorMemoryFrequenciesHz;
        var legacyBands = profile.LegacyBandPresets;
        var capabilities = profile.Capabilities;
        var connectors = profile.Connectors;
        if (!string.Equals(profile.Id, profileId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(profile.DisplayName) ||
            profile.MinimumFrequencyHz < 0 || profile.MaximumFrequencyHz <= profile.MinimumFrequencyHz ||
            profile.DefaultPointCount is < 2 or > 4000 || profile.DdsClockHz <= 0 ||
            profile.DdsPllPrescaler <= 0 || profile.StartPauseMilliseconds < 0 || profile.StepPauseMilliseconds < 0 ||
            !double.IsFinite(profile.ReferenceImpedanceOhms) || profile.ReferenceImpedanceOhms <= 0 ||
            !double.IsFinite(profile.MagnitudeAdcDbPerCount) || profile.MagnitudeAdcDbPerCount <= 0 ||
            !double.IsFinite(profile.PhaseAdcDegreesPerCount) || profile.PhaseAdcDegreesPerCount <= 0 ||
            calibration is null || plotColors is null || generatorMemories is null || legacyBands is null ||
            capabilities is null || connectors is null ||
            generatorMemories.Count is not (0 or 10) ||
            generatorMemories.Any(frequency => frequency < profile.MinimumFrequencyHz || frequency > profile.MaximumFrequencyHz) ||
            legacyBands.Any(band => band is null || string.IsNullOrWhiteSpace(band.Name) ||
                                    !Enum.IsDefined(band.Group) || band.StartFrequencyHz <= 0 ||
                                    band.StopFrequencyHz <= band.StartFrequencyHz) ||
            capabilities.Any(capability => capability is null || string.IsNullOrWhiteSpace(capability.Id) ||
                                            !Enum.IsDefined(capability.Kind) || !Enum.IsDefined(capability.Availability) ||
                                            !Enum.IsDefined(capability.Evidence)) ||
            capabilities.Select(capability => capability.Id).Distinct(StringComparer.Ordinal).Count() != capabilities.Count ||
            connectors.Any(connector => connector is null || string.IsNullOrWhiteSpace(connector.Id) ||
                                        string.IsNullOrWhiteSpace(connector.Label) || !Enum.IsDefined(connector.Role) ||
                                        !Enum.IsDefined(connector.LabelEvidence) || !Enum.IsDefined(connector.RoleEvidence)) ||
            connectors.Select(connector => connector.Id).Distinct(StringComparer.Ordinal).Count() != connectors.Count)
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
