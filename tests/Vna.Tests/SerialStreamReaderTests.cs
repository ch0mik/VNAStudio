using Vna.Devices.Transport;
using Xunit;

namespace Vna.Tests;

public sealed class SerialStreamReaderTests
{
    [Fact]
    public async Task FragmentedReadFillsRequestedFrameAndLeavesSurplusBytesUnread()
    {
        var stream = new ChunkedReadStream([1, 2, 3, 4, 5], maxChunkSize: 2);
        var frame = new byte[4];

        await SerialStreamReader.ReadExactlyAsync(
            stream, frame, TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken, () => stream.Dispose());

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, frame);
        var surplus = new byte[1];
        Assert.Equal(1, await stream.ReadAsync(surplus, TestContext.Current.CancellationToken));
        Assert.Equal(5, surplus[0]);
    }

    [Fact]
    public async Task TimeoutReportsTimeoutInsteadOfReturningPartialFrame()
    {
        await using var stream = new ChunkedReadStream([], maxChunkSize: 1, blockAfterPayload: true);

        var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
            await SerialStreamReader.ReadExactlyAsync(
                stream, new byte[4], TimeSpan.FromMilliseconds(30), CancellationToken.None, () => stream.Dispose()));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
    }

    [Fact]
    public async Task CancellationAbortsReadAndDoesNotReturnPartialFrame()
    {
        await using var stream = new ChunkedReadStream([0x11], maxChunkSize: 1, blockAfterPayload: true);
        var cancellationObserved = false;
        using var cancellation = new CancellationTokenSource();

        var read = SerialStreamReader.ReadExactlyAsync(
            stream,
            new byte[4],
            Timeout.InfiniteTimeSpan,
            cancellation.Token,
            () => cancellationObserved = true).AsTask();
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        Assert.True(cancellationObserved);
    }

    [Fact]
    public async Task ClosedStreamReportsPartialFrameLength()
    {
        await using var stream = new ChunkedReadStream([0x11, 0x22], maxChunkSize: 1);
        var exception = await Assert.ThrowsAsync<EndOfStreamException>(async () =>
            await SerialStreamReader.ReadExactlyAsync(
                stream, new byte[4], TimeSpan.FromSeconds(1), CancellationToken.None, () => stream.Dispose()));

        Assert.Contains("2", exception.Message);
        Assert.Contains("4", exception.Message);
    }

    private sealed class ChunkedReadStream(byte[] payload, int maxChunkSize, bool blockAfterPayload = false) : Stream
    {
        private int _offset;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => payload.Length;
        public override long Position { get => _offset; set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_offset >= payload.Length && blockAfterPayload)
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            if (_offset >= payload.Length) return 0;

            var count = Math.Min(Math.Min(buffer.Length, maxChunkSize), payload.Length - _offset);
            payload.AsMemory(_offset, count).CopyTo(buffer);
            _offset += count;
            return count;
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
