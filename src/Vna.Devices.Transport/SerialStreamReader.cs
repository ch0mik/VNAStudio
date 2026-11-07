using Vna.Localization;

namespace Vna.Devices.Transport;

/// <summary>Reads exactly the requested number of bytes from a potentially fragmented stream.</summary>
internal static class SerialStreamReader
{
    internal static async ValueTask ReadExactlyAsync(
        Stream stream,
        Memory<byte> destination,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Action abortRead)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(abortRead);
        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(timeout), "SerialReadTimeoutInvalid");
        if (destination.Length == 0) return;

        using var cancellationRegistration = cancellationToken.Register(static state => ((Action)state!)(), abortRead);
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
}
