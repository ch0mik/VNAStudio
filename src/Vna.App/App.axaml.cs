using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vna.App.ViewModels;
using Vna.App.Infrastructure;
using Vna.Devices;
using Vna.Devices.Transport;
using Vna.Device.Vna65MHz;
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
                    services.AddSingleton<IValidateOptions<AppSettings>, AppSettingsOptionsValidator>();
                    services.AddOptions<AppSettings>().Bind(context.Configuration);
                    services.AddSingleton(LocalizationService.Instance);
                    // Plugins are added here explicitly as their implementations are migrated.
                    // Constructing the catalog validates all approved registrations at startup.
                    services.AddSingleton(provider => new DevicePluginCatalog(
                        provider.GetServices<IDevicePlugin>()));
                    services.Configure<LegacyAnalyzIniOptions>(context.Configuration);
                    services.Configure<Vna65MHzSerialOptions>(_ => { });
                    services.AddSingleton(provider => Vna65MHzProtocolSettings.FromLegacyProfile(
                        LegacyAnalyzIniProfileFactory.Create(
                            provider.GetRequiredService<IOptions<LegacyAnalyzIniOptions>>().Value)));
                    services.AddTransient<ISerialTransport, SerialPortTransport>();
                    services.AddSingleton<Func<ISerialTransport>>(provider =>
                        () => provider.GetRequiredService<ISerialTransport>());
                    services.AddSingleton(provider => new Vna65MHzPlugin(
                        provider.GetRequiredService<Vna65MHzProtocolSettings>(),
                        provider.GetRequiredService<Func<ISerialTransport>>(),
                        provider.GetRequiredService<IOptions<Vna65MHzSerialOptions>>()));
                    services.AddSingleton<IDevicePlugin>(provider => provider.GetRequiredService<Vna65MHzPlugin>());
                    services.AddSingleton(provider => ActiveDeviceSelection.Resolve(
                        provider.GetRequiredService<DevicePluginCatalog>(),
                        provider.GetRequiredService<IOptions<AppSettings>>().Value.DeviceSelection));
                    services.AddSingleton(provider => DeviceProfileCompatibilityFactory.Create(
                        provider.GetRequiredService<ActiveDeviceSelection>().Profile));
                    services.AddSingleton<IVnaDevice>(provider =>
                    {
                        var profile = provider.GetRequiredService<DeviceProfile>();
                        var selection = provider.GetRequiredService<ActiveDeviceSelection>();
                        var settings = provider.GetRequiredService<IOptionsMonitor<AppSettings>>().CurrentValue;
                        var mode = settings.MeasurementSource?.Trim().ToLowerInvariant() switch
                        {
                            "hardware" => DeviceSourceMode.Hardware,
                            "playback" => DeviceSourceMode.Playback,
                            _ => DeviceSourceMode.Demo,
                        };
                        return new VnaDeviceRouter(
                            new DemoVnaDevice(profile, selection.Profile),
                            new MeasurementSourceDeviceAdapter(profile,
                                selection.Plugin.CreateSource(selection.Profile), selection.PluginId),
                            mode,
                            new SessionPlaybackDevice(profile));
                    });
                    services.AddSingleton<ISerialPortCatalog, SerialPortCatalog>();
                    services.AddSingleton(provider => new MainWindowViewModel(
                        provider.GetRequiredService<IVnaDevice>(),
                        provider.GetRequiredService<ISerialPortCatalog>(),
                        provider.GetRequiredService<IOptionsMonitor<AppSettings>>(),
                        settingsPath,
                        provider.GetRequiredService<LocalizationService>(),
                        provider.GetRequiredService<ActiveDeviceSelection>().Profile,
                        provider.GetRequiredService<ActiveDeviceSelection>().PluginId,
                        provider.GetRequiredService<DevicePluginCatalog>()));
                })
                .Build();

            _host.Services.GetRequiredService<DevicePluginCatalog>();

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
