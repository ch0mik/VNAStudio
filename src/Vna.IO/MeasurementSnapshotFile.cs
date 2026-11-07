using System.Text.Json;
using System.Text.Json.Serialization;
using Vna.Core;
using Vna.Localization;

namespace Vna.IO;

/// <summary>Reads and writes named, immutable measurement comparison snapshots.</summary>
public static class MeasurementSnapshotFile
{
    public const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task SaveAsync(
        Stream destination,
        NamedMeasurementSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!destination.CanWrite)
            throw LocalizedErrors.Argument(nameof(destination), "StreamNotWritable");

        await using var sessionStream = new MemoryStream();
        await MeasurementSessionFile.SaveAsync(sessionStream, snapshot.Session, cancellationToken).ConfigureAwait(false);
        sessionStream.Position = 0;
        using var sessionDocument = await JsonDocument.ParseAsync(sessionStream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var document = new SnapshotDocument(CurrentFormatVersion, snapshot.Name,
            snapshot.DisplayReturnLossOffsetDb, sessionDocument.RootElement.Clone());
        await JsonSerializer.SerializeAsync(destination, document, Options, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<NamedMeasurementSnapshot> LoadAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
            throw LocalizedErrors.Argument(nameof(source), "StreamNotReadable");

        SnapshotDocument? document;
        try
        {
            document = await JsonSerializer.DeserializeAsync<SnapshotDocument>(source, Options, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw LocalizedErrors.InvalidData("SnapshotFileInvalid", exception.Message);
        }
        if (document is null)
            throw LocalizedErrors.InvalidData("SnapshotFileEmpty");
        if (document.FormatVersion != CurrentFormatVersion)
            throw LocalizedErrors.InvalidData("SnapshotFormatUnsupported", document.FormatVersion);
        if (string.IsNullOrWhiteSpace(document.Name) || document.Name.Trim() != document.Name || document.Name.Length > 120)
            throw LocalizedErrors.InvalidData("SnapshotNameInvalid");
        if (!double.IsFinite(document.DisplayReturnLossOffsetDb))
            throw LocalizedErrors.InvalidData("SnapshotDisplayOffsetInvalid");
        if (document.Session.ValueKind != JsonValueKind.Object)
            throw LocalizedErrors.InvalidData("SnapshotSessionMissing");

        await using var sessionStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(sessionStream))
        {
            document.Session.WriteTo(writer);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        sessionStream.Position = 0;
        MeasurementSession session;
        try
        {
            session = await MeasurementSessionFile.LoadAsync(sessionStream, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw LocalizedErrors.InvalidData("SnapshotSessionInvalid", exception.Message);
        }
        return NamedMeasurementSnapshot.Create(document.Name, session, document.DisplayReturnLossOffsetDb);
    }

    private sealed record SnapshotDocument(
        int FormatVersion, string Name, double DisplayReturnLossOffsetDb, JsonElement Session);
}
