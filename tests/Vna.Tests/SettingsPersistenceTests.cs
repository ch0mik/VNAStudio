using System.Text.Json;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vna.App;
using Vna.App.ViewModels;
using Vna.Core;
using Vna.Devices;
using Vna.Devices.Transport;
using Vna.IO;
using Xunit;

namespace Vna.Tests;

public sealed class SettingsPersistenceTests
{
    [AvaloniaFact]
    public async Task FirstLaunchUsesEnglishAndDarkAndCloseRestoresChosenPreferences()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"vna-settings-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PortName"] = "COM9", ["Show50OhmReference"] = "true", ["ReturnLossX2"] = "true",
        }).Build();
        var services = new ServiceCollection();
        services.Configure<AppSettings>(config);
        using var provider = services.BuildServiceProvider();
        await using var vm = new MainWindowViewModel(new DemoVnaDevice(DeviceProfile.Vna65MHz),
            new SerialPortCatalog(), provider.GetRequiredService<IOptionsMonitor<AppSettings>>(), path);
        var window = new MainWindow(vm, persistSettingsOnClose: true);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => closed.TrySetResult();
        try
        {
            Assert.Equal("en-US", vm.SelectedLanguage!.Code);
            Assert.Equal("dark", vm.SelectedTheme.Code);
            Assert.Equal(AntennaChartView.Frequency, vm.SelectedChartView);
            window.Show();
            vm.SelectedLanguage = vm.Languages.Single(language => language.Code == "pl-PL");
            vm.SelectedTheme = vm.Themes.Single(theme => theme.Code == "light");
            vm.SelectedChartView = AntennaChartView.Both;
            window.Close();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            var saved = JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(path))!;
            Assert.Equal("pl-PL", saved.Language);
            Assert.Equal("light", saved.Theme);
            Assert.Equal("both", saved.AntennaChartView);
            Assert.Equal("COM9", saved.PortName);
            Assert.True(saved.Show50OhmReference);
            Assert.True(saved.ReturnLossX2);

            var restoredConfig = new ConfigurationBuilder().AddJsonFile(path).Build();
            var restoredServices = new ServiceCollection();
            restoredServices.Configure<AppSettings>(restoredConfig);
            using var restoredProvider = restoredServices.BuildServiceProvider();
            await using var restored = new MainWindowViewModel(new DemoVnaDevice(DeviceProfile.Vna65MHz),
                new SerialPortCatalog(), restoredProvider.GetRequiredService<IOptionsMonitor<AppSettings>>(), path);
            Assert.Equal("pl-PL", restored.SelectedLanguage!.Code);
            Assert.Equal("light", restored.SelectedTheme.Code);
            Assert.Equal(AntennaChartView.Both, restored.SelectedChartView);
            Assert.Equal("COM9", restored.PortName);
        }
        finally
        {
            window.Close();
            File.Delete(path);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }

    [AvaloniaFact]
    public async Task UnknownChartViewSettingFallsBackToFrequency()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AntennaChartView"] = "not-a-view",
        }).Build();
        var services = new ServiceCollection();
        services.Configure<AppSettings>(config);
        using var provider = services.BuildServiceProvider();
        await using var vm = new MainWindowViewModel(new DemoVnaDevice(DeviceProfile.Vna65MHz),
            new SerialPortCatalog(), provider.GetRequiredService<IOptionsMonitor<AppSettings>>(), "unused-settings.json");

        Assert.Equal(AntennaChartView.Frequency, vm.SelectedChartView);
        Assert.Equal("frequency", vm.CreateAppSettings().AntennaChartView);
    }

    [AvaloniaFact]
    public async Task SaveFailureRemainsVisibleAndDoesNotPreventAnotherClose()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"vna-settings-denied-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var services = new ServiceCollection();
        services.AddOptions<AppSettings>();
        using var provider = services.BuildServiceProvider();
        await using var vm = new MainWindowViewModel(new DemoVnaDevice(DeviceProfile.Vna65MHz),
            new SerialPortCatalog(), provider.GetRequiredService<IOptionsMonitor<AppSettings>>(), directory);
        var window = new MainWindow(vm, persistSettingsOnClose: true);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => closed.TrySetResult();
        var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.HasError) && vm.HasError) failed.TrySetResult();
        };
        try
        {
            window.Show();
            window.Close();
            await failed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            Assert.True(vm.HasError);
            Assert.True(window.IsVisible);
            Assert.True(window.IsEnabled);
            window.Close();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally
        {
            window.Close();
            Directory.Delete(directory);
        }
    }
}
