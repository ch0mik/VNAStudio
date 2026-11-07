using System.Globalization;
using System.Text;
using Vna.Core;

namespace Vna.IO;

/// <summary>
/// Reads and writes the Touchstone v1.x S1P/S2P subset. It preserves complex S values and a
/// common reference resistance; Touchstone 2.x and per-port reference conversion are not inferred.
/// </summary>
public static class TouchstoneFile
{
    private const int MaximumSamples = 1_000_000;
    private static readonly Encoding StrictAscii = Encoding.GetEncoding(
        "us-ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

    public static async Task<NetworkDataSet> LoadAsync(
        Stream source,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        if (!source.CanRead) throw new ArgumentException("The stream is not readable.", nameof(source));

        var portCount = GetPortCount(fileExtension);
        var recordTokenCount = 1 + (portCount == 1 ? 2 : 8);
        var numberBuffer = new List<string>(recordTokenCount);
        var samples = new List<NetworkParameterSample>();
        TouchstoneOptions? options = null;

        try
        {
            using var reader = new StreamReader(source, StrictAscii, detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096, leaveOpen: true);
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var content = RemoveComment(line).Trim();
                if (content.Length == 0) continue;

                if (options is null)
                {
                    if (!content.StartsWith('#'))
                        throw new InvalidDataException("A Touchstone v1 option line must be the first non-comment line.");
                    options = ParseOptions(content, portCount);
                    continue;
                }

                if (content.StartsWith('#') || content.StartsWith('['))
                    throw new InvalidDataException("Only Touchstone v1.x option-line files are supported.");

                foreach (var token in SplitTokens(content))
                {
                    numberBuffer.Add(token);
                    if (numberBuffer.Count != recordTokenCount) continue;
                    if (samples.Count >= MaximumSamples)
                        throw new InvalidDataException($"Touchstone files are limited to {MaximumSamples} samples.");
                    samples.Add(ParseSample(numberBuffer, portCount, options));
                    numberBuffer.Clear();
                }
            }
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Touchstone v1.x files must use ASCII text.", exception);
        }

        if (options is null)
            throw new InvalidDataException("The Touchstone file does not contain an option line.");
        if (numberBuffer.Count != 0)
            throw new InvalidDataException("The Touchstone file ends with an incomplete network sample.");

        var data = new NetworkDataSet(NetworkDataSet.CurrentSchemaVersion, portCount,
            options.ReferenceImpedanceOhms, samples);
        try
        {
            data.Validate();
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The Touchstone file contains invalid network data.", exception);
        }

        return data;
    }

    public static async Task SaveAsync(
        Stream destination,
        NetworkDataSet data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(data);
        if (!destination.CanWrite) throw new ArgumentException("The stream is not writable.", nameof(destination));
        data.Validate();

        using var writer = new StreamWriter(destination, StrictAscii, bufferSize: 4096, leaveOpen: true)
        {
            NewLine = "\n",
        };
        await writer.WriteLineAsync($"# Hz S RI R {Number(data.ReferenceImpedanceOhms)}".AsMemory(), cancellationToken)
            .ConfigureAwait(false);
        var builder = new StringBuilder();
        foreach (var sample in data.Samples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Clear();
            builder.Append(Number(sample.FrequencyHz));
            AppendCoefficient(builder, sample.S11!);
            if (data.PortCount == 2)
            {
                AppendCoefficient(builder, sample.S21!);
                AppendCoefficient(builder, sample.S12!);
                AppendCoefficient(builder, sample.S22!);
            }
            await writer.WriteLineAsync(builder.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static TouchstoneOptions ParseOptions(string line, int portCount)
    {
        var tokens = SplitTokens(line[1..]);
        var frequencyScale = 1_000_000_000d; // Touchstone v1 default: GHz.
        var dataFormat = NetworkDataFormat.MagnitudeAngle;
        var parameter = "S";
        var referenceValues = new List<double>();
        var sawFrequencyUnit = false;
        var sawDataFormat = false;
        var sawParameter = false;
        var sawReference = false;

        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index].ToUpperInvariant();
            switch (token)
            {
                case "HZ":
                    EnsureSingleOption(ref sawFrequencyUnit, "frequency unit");
                    frequencyScale = 1;
                    break;
                case "KHZ":
                    EnsureSingleOption(ref sawFrequencyUnit, "frequency unit");
                    frequencyScale = 1_000;
                    break;
                case "MHZ":
                    EnsureSingleOption(ref sawFrequencyUnit, "frequency unit");
                    frequencyScale = 1_000_000;
                    break;
                case "GHZ":
                    EnsureSingleOption(ref sawFrequencyUnit, "frequency unit");
                    frequencyScale = 1_000_000_000;
                    break;
                case "S":
                case "Y":
                case "Z":
                case "G":
                case "H":
                    EnsureSingleOption(ref sawParameter, "network parameter type");
                    parameter = token;
                    break;
                case "RI":
                    EnsureSingleOption(ref sawDataFormat, "network data format");
                    dataFormat = NetworkDataFormat.RealImaginary;
                    break;
                case "MA":
                    EnsureSingleOption(ref sawDataFormat, "network data format");
                    dataFormat = NetworkDataFormat.MagnitudeAngle;
                    break;
                case "DB":
                    EnsureSingleOption(ref sawDataFormat, "network data format");
                    dataFormat = NetworkDataFormat.DecibelAngle;
                    break;
                case "R":
                    if (sawReference) throw new InvalidDataException("The option line contains more than one reference resistance entry.");
                    sawReference = true;
                    while (index + 1 < tokens.Length && !IsOptionToken(tokens[index + 1]))
                    {
                        referenceValues.Add(ParseNumber(tokens[++index], "reference resistance"));
                    }
                    if (referenceValues.Count == 0)
                        throw new InvalidDataException("The R option must be followed by a positive reference resistance.");
                    if (referenceValues.Count > 1 && index < tokens.Length - 1)
                        throw new InvalidDataException("A multi-value per-port R option must be the final option-line entry.");
                    break;
                default:
                    throw new InvalidDataException($"Unsupported Touchstone option: {tokens[index]}.");
            }
        }

        if (parameter != "S")
            throw new InvalidDataException("Only Touchstone S-parameter data is supported.");
        if (referenceValues.Count is not (0 or 1) && referenceValues.Count != portCount)
            throw new InvalidDataException("The option line has an unsupported number of per-port reference resistances.");

        var referenceImpedance = referenceValues.Count == 0 ? 50 : referenceValues[0];
        if (referenceValues.Any(value => value <= 0) ||
            referenceValues.Any(value => value != referenceImpedance))
            throw new InvalidDataException("Only one positive common reference resistance for all ports is supported.");

        return new TouchstoneOptions(frequencyScale, dataFormat, referenceImpedance);
    }

    private static NetworkParameterSample ParseSample(
        IReadOnlyList<string> values,
        int portCount,
        TouchstoneOptions options)
    {
        var frequency = ParseNumber(values[0], "frequency") * options.FrequencyScale;
        if (!double.IsFinite(frequency) || frequency < 0)
            throw new InvalidDataException("Touchstone frequencies must convert to non-negative finite hertz values.");

        var coefficients = new ComplexCoefficient[portCount == 1 ? 1 : 4];
        for (var index = 0; index < coefficients.Length; index++)
            coefficients[index] = ParseCoefficient(values[1 + index * 2], values[2 + index * 2], options.DataFormat);

        return portCount == 1
            ? new NetworkParameterSample(frequency, coefficients[0])
            : new NetworkParameterSample(frequency, coefficients[0], coefficients[1], coefficients[2], coefficients[3]);
    }

    private static ComplexCoefficient ParseCoefficient(string firstToken, string secondToken, NetworkDataFormat format)
    {
        var first = ParseNumber(firstToken, "network parameter");
        var second = ParseNumber(secondToken, "network parameter");
        if (format == NetworkDataFormat.RealImaginary)
            return new ComplexCoefficient(first, second);

        var magnitude = format == NetworkDataFormat.MagnitudeAngle ? first : Math.Pow(10, first / 20);
        if (!double.IsFinite(magnitude) || magnitude < 0)
            throw new InvalidDataException("Touchstone magnitude must be finite and non-negative.");
        var radians = second * Math.PI / 180;
        return new ComplexCoefficient(magnitude * Math.Cos(radians), magnitude * Math.Sin(radians));
    }

    private static void AppendCoefficient(StringBuilder builder, ComplexCoefficient coefficient) =>
        builder.Append(' ').Append(Number(coefficient.Real)).Append(' ').Append(Number(coefficient.Imaginary));

    private static string RemoveComment(string line)
    {
        var commentIndex = line.IndexOf('!');
        return commentIndex < 0 ? line : line[..commentIndex];
    }

    private static string[] SplitTokens(string line) =>
        line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsOptionToken(string token) => token.ToUpperInvariant() is
        "HZ" or "KHZ" or "MHZ" or "GHZ" or "S" or "Y" or "Z" or "G" or "H" or "RI" or "MA" or "DB" or "R";

    private static int GetPortCount(string extension) => extension.Trim().TrimStart('.').ToLowerInvariant() switch
    {
        "s1p" => 1,
        "s2p" => 2,
        _ => throw new ArgumentException("Only .s1p and .s2p Touchstone files are supported.", nameof(extension)),
    };

    private static void EnsureSingleOption(ref bool seen, string description)
    {
        if (seen) throw new InvalidDataException($"The option line contains more than one {description}.");
        seen = true;
    }

    private static double ParseNumber(string token, string description) =>
        double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value
            : throw new InvalidDataException($"Touchstone {description} is not a finite invariant-culture number.");

    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private enum NetworkDataFormat
    {
        RealImaginary,
        MagnitudeAngle,
        DecibelAngle,
    }

    private sealed record TouchstoneOptions(double FrequencyScale, NetworkDataFormat DataFormat, double ReferenceImpedanceOhms);
}
