using Vna.Localization;
namespace Vna.Core;

public sealed record SweepSettings
{
    public SweepSettings(long startFrequencyHz, long stopFrequencyHz, int pointCount)
    {
        if (startFrequencyHz < 0)
            throw new ArgumentOutOfRangeException(nameof(startFrequencyHz));
        if (stopFrequencyHz <= startFrequencyHz)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(stopFrequencyHz), "SweepStopGreater");
        if (pointCount is < 2 or > 4000)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(pointCount), "SweepPointCount");

        StartFrequencyHz = startFrequencyHz;
        StopFrequencyHz = stopFrequencyHz;
        PointCount = pointCount;
    }

    public long StartFrequencyHz { get; }
    public long StopFrequencyHz { get; }
    public int PointCount { get; }

    public long FrequencyAt(int index)
    {
        if ((uint)index >= (uint)PointCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return StartFrequencyHz + (long)Math.Round(
            (StopFrequencyHz - StartFrequencyHz) * (double)index / (PointCount - 1),
            MidpointRounding.AwayFromZero);
    }

    public void ValidateAgainst(DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ValidateAgainst(profile.MinimumFrequencyHz, profile.MaximumFrequencyHz);
    }

    public void ValidateAgainst(DeviceProfileDescriptor profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ValidateAgainst(profile.MinimumFrequencyHz, profile.MaximumFrequencyHz);
    }

    private void ValidateAgainst(long minimumFrequencyHz, long maximumFrequencyHz)
    {
        if (StartFrequencyHz < minimumFrequencyHz || StopFrequencyHz > maximumFrequencyHz)
            throw LocalizedErrors.ArgumentOutOfRange(nameof(StartFrequencyHz), "SweepRange", minimumFrequencyHz / 1_000_000.0, maximumFrequencyHz / 1_000_000.0);
    }
}
