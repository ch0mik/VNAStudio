using System.IO.Ports;

namespace Vna.Devices.Transport;

/// <summary>Explicit line settings for a serial device. Values must come from a verified device profile.</summary>
public sealed record SerialConnectionSettings(
    string PortName,
    int BaudRate,
    int DataBits,
    Parity Parity,
    StopBits StopBits,
    Handshake Handshake);
