namespace Vna.Devices;

/// <summary>Serial defaults verified from the PA7N miniVNA client and Analyser 306b references.</summary>
public sealed record MiniVnaSerialOptions
{
    public int BaudRate { get; init; } = 115_200;
    public int DataBits { get; init; } = 8;
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(10);
}
