using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vna.App.ViewModels;
using Vna.Devices;
using Vna.Devices.Transport;
using Vna.Core;
using Vna.IO;
using Vna.Localization;

namespace Vna.App;

public sealed partial class App : Application
{
    private IHost? _host;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsPath = AppSettingsFile.GetPath();
            var userProfilePath = Path.Combine(AppContext.BaseDirectory, "analyz.ini");
            var profilePath = File.Exists(userProfilePath)
                ? userProfilePath
                : Path.Combine(AppContext.BaseDirectory, "Assets", "analyser306b-65mhz.ini");
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, configuration) =>
                {
                    // Load persisted choices at startup; keep subsequent UI edits until saved.
                    configuration.AddJsonFile(settingsPath, optional: true, reloadOnChange: false);
                    configuration.AddIniFile(profilePath, optional: false, reloadOnChange: false);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<AppSettings>(context.Configuration);
                    services.AddSingleton(LocalizationService.Instance);
                    services.Configure<LegacyAnalyzIniOptions>(context.Configuration);
                    services.Configure<MiniVnaSerialOptions>(_ => { });
                    services.AddSingleton(provider => LegacyAnalyzIniProfileFactory.Create(
                        provider.GetRequiredService<IOptions<LegacyAnalyzIniOptions>>().Value));
                    services.AddSingleton<ISerialTransport, SerialPortTransport>();
                    services.AddSingleton<IVnaDevice>(provider =>
                    {
                        var profile = provider.GetRequiredService<DeviceProfile>();
                        var settings = provider.GetRequiredService<IOptionsMonitor<AppSettings>>().CurrentValue;
                        var mode = settings.MeasurementSource?.Trim().ToLowerInvariant() switch
                        {
                            "hardware" => DeviceSourceMode.Hardware,
                            "playback" => DeviceSourceMode.Playback,
                            _ => DeviceSourceMode.Demo,
                        };
                        return new VnaDeviceRouter(
                            new DemoVnaDevice(profile),
                            new MiniVnaSerialDevice(profile,
                                provider.GetRequiredService<ISerialTransport>(),
                                provider.GetRequiredService<IOptions<MiniVnaSerialOptions>>()),
                            mode,
                            new SessionPlaybackDevice(profile));
                    });
                    services.AddSingleton<ISerialPortCatalog, SerialPortCatalog>();
                    services.AddSingleton(provider => new MainWindowViewModel(
                        provider.GetRequiredService<IVnaDevice>(),
                        provider.GetRequiredService<ISerialPortCatalog>(),
                        provider.GetRequiredService<IOptionsMonitor<AppSettings>>(),
                        settingsPath,
                        provider.GetRequiredService<LocalizationService>()));
                })
                .Build();

            desktop.MainWindow = new MainWindow(_host.Services.GetRequiredService<MainWindowViewModel>(),
                persistSettingsOnClose: true);
            desktop.Exit += async (_, _) =>
            {
                if (_host is null) return;
                await _host.StopAsync();
                if (_host is IAsyncDisposable asyncHost)
                    await asyncHost.DisposeAsync();
                else
                    _host.Dispose();
                _host = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
