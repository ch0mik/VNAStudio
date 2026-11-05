namespace Vna.Devices.Transport;

public interface ISerialPortCatalog
{
    Task<IReadOnlyList<string>> GetAvailablePortNamesAsync(CancellationToken cancellationToken = default);
}
