using System.IO.Ports;

namespace Vna.Devices.Transport;

/// <summary>Returns OS-native port names without converting them to Windows COM numbers.</summary>
public sealed class SerialPortCatalog : ISerialPortCatalog
{
    public async Task<IReadOnlyList<string>> GetAvailablePortNamesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ports = await Task.Run(SerialPort.GetPortNames, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return ports;
    }
}
