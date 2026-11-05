using Microsoft.Extensions.Options;
using Vna.App.ViewModels;
using Vna.Core;
using Vna.Devices;
using Vna.Devices.Transport;
using Vna.IO;

namespace Vna.Tests;

internal static class TestData
{
    public static MeasurementPoint Point(long frequencyHz, double? swr = 2, SampleQuality quality = SampleQuality.Valid) =>
        new(frequencyHz, swr, -10, 90, 50, 50, 0, quality);

    public static MeasurementSession Session(DeviceProfile? profile = null)
    {
        profile ??= DeviceProfile.Vna65MHz;
        var sweep = new SweepSettings(1_000_000, 5_000_000, 5);
        return new MeasurementSession(1, profile.Id, "TEST", DateTimeOffset.UnixEpoch, sweep,
            Enumerable.Range(0, 5).Select(i => Point(sweep.FrequencyAt(i), 2 + i)).ToArray())
        {
            ProfileSnapshot = profile,
        };
    }

    // Existing measurement fixtures use a fixed culture/palette; startup tests pass AppSettings explicitly.
    public static MainWindowViewModel ViewModel(AppSettings? settings = null, IVnaDevice? device = null) => new(
        device ?? new DemoVnaDevice(DeviceProfile.Vna65MHz), new NoPorts(),
        new TestSettings(settings ?? new AppSettings { Language = "pl-PL", Theme = "light" }), "unused-settings.json");

    private sealed class NoPorts : ISerialPortCatalog
    {
        public Task<IReadOnlyList<string>> GetAvailablePortNamesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    private sealed class TestSettings(AppSettings settings) : IOptionsMonitor<AppSettings>
    {
        public AppSettings CurrentValue { get; } = settings;
        public AppSettings Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AppSettings, string?> listener) => null;
    }
}
