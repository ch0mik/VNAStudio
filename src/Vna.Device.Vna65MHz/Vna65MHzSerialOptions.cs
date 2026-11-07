namespace Vna.Device.Vna65MHz;

/// <summary>Serial protocol defaults retained from the supported VNA 1–65 MHz profile.</summary>
public sealed record Vna65MHzSerialOptions
{
    public int BaudRate { get; init; } = 115_200;
    public int DataBits { get; init; } = 8;
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(10);
}
