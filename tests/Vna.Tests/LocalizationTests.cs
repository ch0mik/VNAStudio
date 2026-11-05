using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vna.App;
using Vna.App.Plotting;
using Vna.App.ViewModels;
using Vna.Core;
using Vna.Devices;
using Vna.Devices.Transport;
using Vna.IO;
using Vna.Localization;
using Xunit;

namespace Vna.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void EnglishAndPolishResourcesHaveSameKeysAndFormatArguments()
    {
        var manager = new ResourceManager("Vna.Localization.Resources.Strings", typeof(Text).Assembly);
        var english = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var polish = manager.GetResourceSet(CultureInfo.GetCultureInfo("pl"), true, false)!;
        Assert.NotNull(polish);
        var englishValues = english.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
        var polishValues = polish.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
        Assert.Equal(englishValues.Keys.Order(), polishValues.Keys.Order());
        foreach (var (key, value) in englishValues)
        {
            Assert.False(string.IsNullOrWhiteSpace(value));
            Assert.False(string.IsNullOrWhiteSpace(polishValues[key]));
            Assert.Equal(CompositeFormat.Parse(value).MinimumArgumentCount,
                CompositeFormat.Parse(polishValues[key]).MinimumArgumentCount);
        }
    }

    [Theory]
    [InlineData("pl", "pl-PL")]
    [InlineData("pl-PL", "pl-PL")]
    [InlineData("en", "en-US")]
    [InlineData("en-GB", "en-US")]
    [InlineData("de-DE", "en-US")]
    [InlineData("", "en-US")]
    [InlineData("invalid_language_!", "en-US")]
    public void LanguageCodesNormalizeToSupportedResources(string input, string expected) =>
        Assert.Equal(expected, LocalizationService.NormalizeLanguage(input));

    [AvaloniaFact]
    public async Task LanguageSelectorUpdatesUiStatusLegendAndNumbersWithoutLosingData()
    {
        using var culture = new LanguageScope();
        await using var vm = TestData.ViewModel();
        var sweep = new SweepSettings(1_500_000, 5_500_000, 5);
        var session = TestData.Session() with
        {
            DataSource = "demo",
            Sweep = sweep,
            Points = Enumerable.Range(0, 5).Select(i => TestData.Point(sweep.FrequencyAt(i))).ToArray(),
        };
        vm.LoadSession(session);
        vm.PortName = "/dev/ttyUSB0";
        vm.PickMarker(1_500_000, false);
        var window = new MainWindow(vm) { Width = 1024, Height = 768 };
        try
        {
            window.Show();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            var selector = window.FindControl<ComboBox>("LanguageSelector")!;
            var start = window.FindControl<Button>("StartScanButton")!;
            Assert.Equal("▶   Rozpocznij skan", start.Content);
            Assert.Equal("1,5", vm.StartMHz);
            Assert.Equal("Faza", vm.Traces.Single(t => t.Trace == PlotTrace.Phase).DisplayName);

            selector.SelectedItem = vm.Languages.Single(language => language.Code == "en-US");
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal("en-US", vm.Localizer.LanguageCode);
            Assert.Equal("▶   Start scan", start.Content);
            Assert.Equal("1.5", vm.StartMHz);
            Assert.Equal("1.500 MHz", vm.Marker1Text);
            Assert.Equal("Phase", vm.Traces.Single(t => t.Trace == PlotTrace.Phase).DisplayName);
            Assert.Equal("Session loaded · 5 points", vm.Status);
            Assert.Equal("SESSION · DEMONSTRATION DATA", vm.DataSourceDisplay);
            Assert.Equal("en-US", window.FindControl<VnaPlotControl>("Plot")!.LanguageCode);
            Assert.Equal("/dev/ttyUSB0", vm.CreateAppSettings().PortName);
            Assert.Equal("en-US", vm.CreateAppSettings().Language);
            Assert.Equal(session.Points.ToArray(), vm.CreateSession()!.Points.ToArray());
            Assert.Equal("demo", vm.CreateSession()!.DataSource);            Assert.Equal(Text.Get("NoSessionFile"), vm.CurrentFileDisplay);
            vm.MarkSessionSaved("scan.vnasession.json");
            Assert.Equal("scan.vnasession.json", vm.CurrentFileDisplay);
            Assert.False(vm.HasUnsavedResult);

            vm.ReportLocalizedError("SaveRequiresComplete");
            selector.SelectedItem = vm.Languages.Single(language => language.Code == "pl-PL");
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal("▶   Rozpocznij skan", start.Content);
            Assert.Equal("1,5", vm.StartMHz);
            Assert.Equal("1,500 MHz", vm.Marker1Text);
            Assert.Equal("Wczytano sesję · 5 punktów", vm.Status);
            Assert.Equal("Do zapisu potrzebny jest kompletny skan lub wczytana sesja.", vm.Error);

            var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SweepSettings(2, 1, 2));
            vm.ReportException(error);
            selector.SelectedItem = vm.Languages.Single(language => language.Code == "en-US");
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal("The stop frequency must be greater than the start frequency.", vm.Error);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task SavedEnglishLanguageIsAppliedAtStartupAndOptionsReload()
    {
        using var culture = new LanguageScope();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Language"] = "en-US", ["PortName"] = "COM7", ["Show50OhmReference"] = "true", ["ReturnLossX2"] = "true", ["ShowFullMarkerValues"] = "false", ["ReturnLossDisplayOffsetDb"] = "2.5",
        }).Build();
        var services = new ServiceCollection();
        services.Configure<AppSettings>(config);
        using var provider = services.BuildServiceProvider();
        await using var vm = new MainWindowViewModel(new DemoVnaDevice(DeviceProfile.Vna65MHz),
            new SerialPortCatalog(), provider.GetRequiredService<IOptionsMonitor<AppSettings>>(), "unused-settings.json");
        Assert.Equal("en-US", vm.SelectedLanguage!.Code);
        Assert.Equal("Ready. Device not connected.", vm.Status);
        Assert.Equal("COM7", vm.PortName);
        Assert.True(vm.Show50OhmReference);
        Assert.True(vm.ReturnLossX2);
        Assert.False(vm.ShowFullMarkerValues);
        Assert.True(vm.ShowCompactMarkerValues);
        Assert.Equal(2.5, vm.ReturnLossDisplayOffsetDb);
        Assert.Equal("2.5", vm.ReturnLossDisplayOffsetText);
        Assert.True(vm.CreateAppSettings().Show50OhmReference);
        Assert.True(vm.CreateAppSettings().ReturnLossX2);
        config["Language"] = "pl-PL";
        config.Reload();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        Assert.Equal("pl-PL", vm.SelectedLanguage!.Code);
        Assert.Equal("Gotowy. Urządzenie nie jest połączone.", vm.Status);
        Assert.Equal("COM7", vm.PortName);
        Assert.True(vm.Show50OhmReference);
        Assert.True(vm.ReturnLossX2);
        Assert.False(vm.ShowFullMarkerValues);
        Assert.True(vm.ShowCompactMarkerValues);
        Assert.Equal(2.5, vm.ReturnLossDisplayOffsetDb);
        Assert.Equal("2,5", vm.ReturnLossDisplayOffsetText);
        Assert.True(vm.CreateAppSettings().Show50OhmReference);
        Assert.True(vm.CreateAppSettings().ReturnLossX2);
    }

    [Fact]
    public async Task SettingsKeepLanguageAndPortAndOldFilesDefaultToEnglish()
    {
        var path = Path.Combine(Path.GetTempPath(), $"vna-language-{Guid.NewGuid():N}.json");
        try
        {
            var settings = new AppSettings { PortName = "/dev/ttyACM0", Language = "en-US", Theme = "night-vision", Show50OhmReference = true, ShowXcXl = true, ReturnLossX2 = true, ShowFullMarkerValues = false, ReturnLossDisplayOffsetDb = 2.5, CableVelocityFactor = 0.81 };
            await AppSettingsFile.SaveAsync(settings, path, TestContext.Current.CancellationToken);
            var json = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            var restored = JsonSerializer.Deserialize<AppSettings>(json)!;
            Assert.Equal(settings.PortName, restored.PortName);
            Assert.Equal(settings.Language, restored.Language);
            Assert.Equal(settings.Theme, restored.Theme);
            Assert.True(restored.Show50OhmReference);
            Assert.True(restored.ShowXcXl);
            Assert.True(restored.ReturnLossX2);
            Assert.False(restored.ShowFullMarkerValues);
            Assert.Equal(2.5, restored.ReturnLossDisplayOffsetDb);
            Assert.Equal(0.81, restored.CableVelocityFactor);
            Assert.Equal(settings.TraceColors, restored.TraceColors);
            Assert.Equal("en-US", JsonSerializer.Deserialize<AppSettings>("{\"PortName\":\"COM5\"}")!.Language);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaTheory]
    [InlineData("pl-PL", "Częstotliwość końcowa musi być większa od początkowej.")]
    [InlineData("en-US", "The stop frequency must be greater than the start frequency.")]
    public async Task ValidationAndSessionErrorsUseSelectedLanguage(string language, string expected)
    {
        using var culture = new LanguageScope();
        LocalizationService.Instance.ChangeLanguage(language);
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new SweepSettings(2, 1, 2));
        Assert.Contains(expected, error.Message);
        using var stream = new MemoryStream();
        var sessionError = await Assert.ThrowsAsync<InvalidDataException>(() => MeasurementSessionFile.SaveAsync(
            stream, TestData.Session() with { FormatVersion = 999 }, TestContext.Current.CancellationToken));
        Assert.Equal(Text.Format("SessionFormatUnsupported", 999), sessionError.Message);
    }

    [AvaloniaFact]
    public void ChangingLanguageDoesNotChangeProcessCulture()
    {
        using var language = new LanguageScope();
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        LocalizationService.Instance.ChangeLanguage("en-US");
        Assert.Same(culture, CultureInfo.CurrentCulture);
        Assert.Same(uiCulture, CultureInfo.CurrentUICulture);
        Assert.Equal("Save measurement session", Text.Get("SaveSessionTitle"));
    }

    private sealed class LanguageScope : IDisposable
    {
        private readonly string _original = LocalizationService.Instance.LanguageCode;
        public void Dispose() => LocalizationService.Instance.ChangeLanguage(_original);
    }
}



