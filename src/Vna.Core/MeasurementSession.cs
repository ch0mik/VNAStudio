namespace Vna.Core;

public enum SessionAnalysisMode { Antenna, CableLoss, CableLength, Xtal, Filter, Generator }

public sealed record MeasurementSession(
    int FormatVersion,
    string ProfileId,
    string DataSource,
    DateTimeOffset CapturedAtUtc,
    SweepSettings Sweep,
    IReadOnlyList<MeasurementPoint> Points)
{
    public DeviceProfile? ProfileSnapshot { get; init; }
    public SessionAnalysisMode AnalysisMode { get; init; } = SessionAnalysisMode.Antenna;
}
