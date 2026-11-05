using Vna.Localization;
using System.IO.Ports;

namespace Vna.Devices.Transport;

/// <summary>
/// Cross-platform serial byte stream. The port name is passed to System.IO.Ports unchanged;
/// protocol framing, baud rate and other line settings must be supplied by a verified profile.
/// </summary>
public sealed class SerialPortTransport : ISerialTransport
{
    private SerialPort? _port;
    private Stream? _stream;

    public bool IsOpen => _port?.IsOpen == true;

    public async ValueTask OpenAsync(SerialConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();
        if (IsOpen)
            throw LocalizedErrors.InvalidOperation("SerialAlreadyOpen");
        if (string.IsNullOrWhiteSpace(settings.PortName))
            throw LocalizedErrors.Argument(nameof(settings), "SerialPortRequired");
        if (settings.BaudRate <= 0)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(settings), "SerialBaudInvalid");
        if (settings.DataBits is < 5 or > 8)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(settings), "SerialDataBitsInvalid");

        var port = new SerialPort(settings.PortName, settings.BaudRate, settings.Parity, settings.DataBits, settings.StopBits)
        {
            Handshake = settings.Handshake,
        };

        try
        {
            // SerialPort.Open is synchronous and has no cancellation overload; dispose the port if
            // the caller stops while a driver is opening it.
            using var cancellationRegistration = cancellationToken.Register(static state =>
                DisposePort((SerialPort)state!), port);
            var openTask = Task.Run(port.Open, CancellationToken.None);
            try
            {
                await openTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _ = openTask.ContinueWith(
                    static task => _ = task.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                throw;
            }
            cancellationToken.ThrowIfCancellationRequested();
            _port = port;
            _stream = port.BaseStream;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            DisposePort(port);
            throw new OperationCanceledException(cancellationToken);
        }
        catch (UnauthorizedAccessException exception)
        {
            DisposePort(port);
            throw LocalizedErrors.AccessDenied(exception, "SerialPortAccessDenied", settings.PortName, exception.Message);
        }
        catch
        {
            DisposePort(port);
            throw;
        }
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        var stream = GetOpenStream();
        var port = _port ?? throw LocalizedErrors.InvalidOperation("SerialNotOpen");
        using var cancellationRegistration = cancellationToken.Register(static state =>
            DisposePort((SerialPort)state!), port);
        try
        {
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    public async ValueTask ReadExactlyAsync(
        Memory<byte> destination,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(timeout), "SerialReadTimeoutInvalid");
        if (destination.Length == 0) return;

        var stream = GetOpenStream();
        var port = _port ?? throw LocalizedErrors.InvalidOperation("SerialNotOpen");
        using var cancellationRegistration = cancellationToken.Register(static state =>
            DisposePort((SerialPort)state!), port);
        using var timeoutSource = new CancellationTokenSource();
        if (timeout != Timeout.InfiniteTimeSpan) timeoutSource.CancelAfter(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        var received = 0;
        while (received < destination.Length)
        {
            int count;
            try
            {
                count = await stream.ReadAsync(destination[received..], linkedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw LocalizedErrors.Timeout("SerialReadTimeout", timeout);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (count == 0)
                throw LocalizedErrors.EndOfStream("SerialStreamClosed", received, destination.Length);

            received += count;
        }
    }

    public ValueTask CloseAsync()
    {
        _stream = null;
        _port?.Dispose();
        _port = null;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => CloseAsync();

    private Stream GetOpenStream() => _stream is { CanRead: true, CanWrite: true } stream
        ? stream
        : throw LocalizedErrors.InvalidOperation("SerialNotOpen");

    private static void DisposePort(SerialPort port)
    {
        try { port.Dispose(); }
        catch (Exception) { }
    }
}
