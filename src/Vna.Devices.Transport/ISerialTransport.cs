namespace Vna.Devices.Transport;

/// <summary>Byte-stream transport only. It does not define VNA commands or frame boundaries.</summary>
public interface ISerialTransport : IAsyncDisposable
{
    bool IsOpen { get; }
    ValueTask OpenAsync(SerialConnectionSettings settings, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);
    ValueTask ReadExactlyAsync(Memory<byte> destination, TimeSpan timeout, CancellationToken cancellationToken = default);
    ValueTask CloseAsync();
}
