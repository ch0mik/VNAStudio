using System.Globalization;
using System.Text;
using Vna.Core;

namespace Vna.IO;

/// <summary>Reads and writes VNA Studio CSV and the identified Analyser 306b antenna CSV columns.</summary>
public static class MeasurementCsvFile
{
    private static readonly string[] Header =
    [
        "Frequency_Hz", "SWR", "Impedance_Ohm", "ReturnLoss_dB", "Phase_Degrees",
        "Resistance_Ohm", "Reactance_Ohm", "Quality", "Transmission_dB", "AnalysisMode",
    ];
    private static readonly string[] PreviousStudioHeader =
    [
        "Frequency_Hz", "SWR", "Impedance_Ohm", "ReturnLoss_dB", "Phase_Degrees",
        "Resistance_Ohm", "Reactance_Ohm", "Quality",
    ];

    public static async Task SaveAsync(Stream destination, MeasurementSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(session);
        if (!destination.CanWrite) throw new ArgumentException("The stream is not writable.", nameof(destination));
        if (session.Points is null) throw new ArgumentException("The session has no points.", nameof(session));

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Header));
        foreach (var point in session.Points)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Append(point.FrequencyHz.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Number(point.Swr)).Append(',')
                .Append(Number(point.ImpedanceOhms)).Append(',')
                .Append(Number(point.ReturnLossDb)).Append(',')
                .Append(Number(point.PhaseDegrees)).Append(',')
                .Append(Number(point.ResistanceOhms)).Append(',')
                .Append(Number(point.ReactanceOhms)).Append(',')
                .Append(point.Quality.ToString()).Append(',')
                .Append(Number(point.TransmissionDb)).Append(',')
                .Append(session.AnalysisMode.ToString()).AppendLine();
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(builder.ToString());
        await destination.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<MeasurementSession> LoadAsync(
        Stream source,
        DeviceProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(profile);
        if (!source.CanRead) throw new ArgumentException("The stream is not readable.", nameof(source));

        using var reader = new StreamReader(source, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096, leaveOpen: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The CSV file is empty.");
        var headers = SplitCsvLine(headerLine).Select(NormalizeHeader).ToArray();
        var frequencyColumn = FindColumn(headers, "Frequency_Hz", "Frequency(Hz)");
        var returnLossColumn = FindColumn(headers, "ReturnLoss_dB", "Return Loss(dB)");
        var phaseColumn = FindOptionalColumn(headers, "Phase_Degrees", "Phase(deg)");
        var swrColumn = FindOptionalColumn(headers, "SWR");
        var impedanceColumn = FindOptionalColumn(headers, "Impedance_Ohm");
        var resistanceColumn = FindOptionalColumn(headers, "Resistance_Ohm");
        var reactanceColumn = FindOptionalColumn(headers, "Reactance_Ohm");
        var qualityColumn = FindOptionalColumn(headers, "Quality");
        var transmissionColumn = FindOptionalColumn(headers, "Transmission_dB");
        var analysisModeColumn = FindOptionalColumn(headers, "AnalysisMode");

        var isStudioFormat = headers.SequenceEqual(Header, StringComparer.OrdinalIgnoreCase);
        var isPreviousStudioFormat = headers.SequenceEqual(PreviousStudioHeader, StringComparer.OrdinalIgnoreCase);
        var isAnyStudioFormat = isStudioFormat || isPreviousStudioFormat;
        if (frequencyColumn < 0 || (!isAnyStudioFormat && returnLossColumn < 0))
            throw new InvalidDataException("CSV must contain Frequency(Hz) and Return Loss(dB) columns.");

        var expectedColumnCount = isStudioFormat ? Header.Length : isPreviousStudioFormat ? PreviousStudioHeader.Length : headers.Length;
        if (isAnyStudioFormat)
        {
            frequencyColumn = 0;
            swrColumn = 1;
            impedanceColumn = 2;
            returnLossColumn = 3;
            phaseColumn = 4;
            resistanceColumn = 5;
            reactanceColumn = 6;
            qualityColumn = 7;
            if (isStudioFormat)
            {
                transmissionColumn = 8;
                analysisModeColumn = 9;
            }
        }
        else if (headers.Length is < 2 or > 3 || (headers.Length == 3 && phaseColumn < 0))
        {
            throw new InvalidDataException("Only VNA Studio CSV and the identified two/three-column Analyser antenna CSV are supported.");
        }

        var points = new List<MeasurementPoint>();
        SessionAnalysisMode? csvMode = null;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = SplitCsvLine(line);
            if (fields.Length != expectedColumnCount || points.Count >= 4000)
                throw new InvalidDataException("CSV row has an unsupported number of fields or the file exceeds 4000 samples.");
            var frequency = ParseFrequency(fields[frequencyColumn]);
            var returnLoss = ParseOptionalNumber(fields[returnLossColumn]);
            var phase = phaseColumn >= 0 ? ParseOptionalNumber(fields[phaseColumn]) : null;
            if (analysisModeColumn >= 0)
            {
                if (!Enum.TryParse<SessionAnalysisMode>(fields[analysisModeColumn], ignoreCase: true, out var parsedMode) ||
                    !Enum.IsDefined(parsedMode) || csvMode is not null && csvMode != parsedMode)
                    throw new InvalidDataException("CSV contains an unsupported or inconsistent analysis mode.");
                csvMode = parsedMode;
            }
            var quality = qualityColumn >= 0
                ? ParseQuality(fields[qualityColumn])
                : returnLoss is null && phase is null ? SampleQuality.Missing : SampleQuality.Valid;

            points.Add(new MeasurementPoint(
                FrequencyHz: frequency,
                Swr: swrColumn >= 0 ? ParseOptionalNumber(fields[swrColumn]) : null,
                ReturnLossDb: returnLoss,
                PhaseDegrees: phase,
                ImpedanceOhms: impedanceColumn >= 0 ? ParseOptionalNumber(fields[impedanceColumn]) : null,
                ResistanceOhms: resistanceColumn >= 0 ? ParseOptionalNumber(fields[resistanceColumn]) : null,
                ReactanceOhms: reactanceColumn >= 0 ? ParseOptionalNumber(fields[reactanceColumn]) : null,
                Quality: quality)
            {
                PhaseSignConfidence = phase is null ? PhaseSignConfidence.Missing : PhaseSignConfidence.Ambiguous,
                PhaseSignProvenance = phase is null ? PhaseSignProvenance.Unknown : PhaseSignProvenance.ImportedUnverified,
                TransmissionDb = transmissionColumn >= 0 ? ParseOptionalNumber(fields[transmissionColumn]) : null,
            });
        }

        if (points.Count < 2) throw new InvalidDataException("CSV must contain at least two measurement rows.");
        var sweep = new SweepSettings(points[0].FrequencyHz, points[^1].FrequencyHz, points.Count);
        sweep.ValidateAgainst(profile);
        for (var index = 0; index < points.Count; index++)
        {
            if (points[index].FrequencyHz != sweep.FrequencyAt(index))
                throw new InvalidDataException("CSV frequencies must be strictly increasing and evenly spaced across the sweep.");
        }

        return new MeasurementSession(MeasurementSessionFile.CurrentFormatVersion, profile.Id,
            isAnyStudioFormat ? "VNA Studio CSV" : "Analyser 306b antenna CSV",
            DateTimeOffset.UtcNow, sweep, points)
        {
            ProfileSnapshot = profile,
            AnalysisMode = csvMode ?? SessionAnalysisMode.Antenna,
        };
    }

    private static int FindColumn(IReadOnlyList<string> headers, params string[] names)
    {
        var index = FindOptionalColumn(headers, names);
        if (index < 0) throw new InvalidDataException($"Required CSV column is missing: {string.Join(" / ", names)}.");
        return index;
    }

    private static int FindOptionalColumn(IReadOnlyList<string> headers, params string[] names)
    {
        for (var index = 0; index < headers.Count; index++)
            if (names.Contains(headers[index], StringComparer.OrdinalIgnoreCase))
                return index;
        return -1;
    }

    private static string NormalizeHeader(string value) => value.Trim().Trim('"').Trim();

    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                fields.Add(field.ToString().Trim());
                field.Clear();
            }
            else field.Append(character);
        }
        if (quoted) throw new InvalidDataException("CSV contains an unterminated quoted field.");
        fields.Add(field.ToString().Trim());
        return fields.ToArray();
    }

    private static long ParseFrequency(string value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var frequency) && frequency > 0
            ? frequency
            : throw new InvalidDataException("CSV contains an invalid frequency in Hz.");

    private static double? ParseOptionalNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
            return number;
        throw new InvalidDataException("CSV contains a non-finite or invalid numeric value.");
    }

    private static SampleQuality ParseQuality(string value) =>
        Enum.TryParse<SampleQuality>(value, ignoreCase: true, out var quality) && Enum.IsDefined(quality)
            ? quality
            : throw new InvalidDataException("CSV contains an unknown sample quality.");

    private static string Number(double? value) => value is { } number && double.IsFinite(number)
        ? number.ToString("R", CultureInfo.InvariantCulture)
        : string.Empty;
}
