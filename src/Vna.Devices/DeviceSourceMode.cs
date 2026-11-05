namespace Vna.Devices;

public enum DeviceSourceMode
{
    Demo,
    Hardware,
    Playback,
}

public interface IDeviceSourceSelector
{
    DeviceSourceMode SourceMode { get; }
    bool SelectSource(DeviceSourceMode mode);
}

public interface ISessionPlaybackSource
{
    void SetSession(Vna.Core.MeasurementSession session);
}
