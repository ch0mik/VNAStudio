using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vna.App;
using Vna.App.Plotting;
using Vna.App.Theming;
using Vna.App.ViewModels;
using Vna.Core;
using Vna.Devices;
using Vna.Devices.Transport;
using Vna.IO;
using Vna.Localization;
using Xunit;

namespace Vna.Tests;

public sealed class ThemeTests
{
    [Theory]
    [InlineData(null, "dark")]
    [InlineData("unknown", "dark")]
    [InlineData("light", "light")]
    [InlineData(" DARK ", "dark")]
    [InlineData("night-vision", "night-vision")]
    public void ThemeCodesNormalize(string? code, string expected) => Assert.Equal(expected, AppThemes.Normalize(code));

    [Fact]
    public void NightPlotUsesOnlyRedAndDistinctTracePatterns()
    {
        var palette = PlotPalette.FromProfile(DeviceProfile.Vna65MHz, AppThemeKind.NightVision);
        foreach (var value in new[] { palette.Background, palette.Grid, palette.Border, palette.AxisText, palette.AxisTitle,
                     palette.MinimumMarker, palette.Marker1, palette.Marker2 }.Concat(PlotSeriesCatalog.All.Select(s => palette.GetColor(s.Trace))))
        {
            var color = Color.Parse(value);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
        }
        Assert.Equal(6, PlotSeriesCatalog.All.Select(s => string.Join(",", palette.GetDashPattern(s.Trace))).Distinct().Count());
    }

    [Fact]
    public void DarkPaletteMakesDarkTracesVisibleAndLeavesProfileUnchanged()
    {
        var profile = DeviceProfile.Vna65MHz with { PlotColors = LegacyPlotColorSettings.Default with { Swr = 0, Background = 0xFFFFFF } };
        var palette = PlotPalette.FromProfile(profile, AppThemeKind.Dark);
        Assert.Equal("#101722", palette.Background);
        Assert.NotEqual("#000000", palette.GetColor(PlotTrace.Swr));
        Assert.NotEqual("#0000FF", palette.GetColor(PlotTrace.ReturnLoss));
        Assert.Equal((uint)0, profile.PlotColors.Swr);
        Assert.Equal("#000000", PlotPalette.FromProfile(profile).GetColor(PlotTrace.Swr));
        Assert.Equal("#FFFFFF", PlotPalette.FromProfile(profile).Background);
    }

    [AvaloniaFact]
    public async Task SelectorChangesEntireWindowAndPlotWithoutChangingSession()
    {
        await using var vm = TestData.ViewModel();
        var session = TestData.Session();
        vm.LoadSession(session);
        vm.PortName = "/dev/ttyUSB0";
        vm.PickMarker(2_000_000, false);
        vm.PickMarker(4_000_000, true);
        vm.ZoomToMarkersCommand.Execute(null);
        var points = vm.PlotPoints;
        vm.ReturnLossX2 = true;
        vm.ShowFullMarkerValues = false;
        var readout = vm.Marker1Readout;
        var window = new MainWindow(vm) { Width = 1024, Height = 768 };
        try
        {
            window.Show();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            var selector = window.FindControl<ComboBox>("ThemeSelector")!;
            var plot = window.FindControl<VnaPlotControl>("Plot")!;
            Assert.True(plot.ReturnLossX2);
            Assert.False(window.FindControl<Grid>("FullMarkerValues")!.IsVisible);
            Assert.True(window.FindControl<Grid>("CompactMarkerValues")!.IsVisible);
            CaptureTheme(window, "light", "regression-");
            selector.SelectedItem = vm.Themes.Single(theme => theme.Code == "dark");
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
            Assert.Equal(Color.Parse("#0D1117"), Assert.IsType<SolidColorBrush>(window.Background).Color);
            Assert.Equal("#101722", plot.Palette.Background);
            CaptureTheme(window, "dark", "regression-");

            selector.SelectedItem = vm.Themes.Single(theme => theme.Code == "night-vision");
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal(AppThemes.NightVision, window.ActualThemeVariant);
            Assert.True(plot.Palette.IsNightVision);
            var settingsPanel = window.FindControl<Border>("SettingsPanel")!;
            var viewport = settingsPanel.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
            foreach (var input in settingsPanel.GetVisualDescendants().OfType<Control>()
                         .Where(control => control is TextBox or ComboBox))
            {
                var position = input.TranslatePoint(new Point(input.Bounds.Width, 0), viewport)!.Value;
                Assert.True(position.X <= viewport.Bounds.Width - 10,
                    $"A settings field overlaps the scrollbar: {position.X} > {viewport.Bounds.Width - 10}.");
            }
            CaptureTheme(window, "night-vision", "regression-");
            Assert.Same(points, vm.PlotPoints);
            Assert.Equal(readout, vm.Marker1Readout);
            Assert.Equal(2_000_000, vm.ViewStartHz);
            Assert.Equal(4_000_000, vm.ViewStopHz);
            Assert.Equal(session.ProfileSnapshot, vm.CreateSession()!.ProfileSnapshot);
            Assert.Equal("/dev/ttyUSB0", vm.CreateAppSettings().PortName);
            Assert.Equal("night-vision", vm.CreateAppSettings().Theme);
            foreach (var trace in vm.Traces)
            {
                Assert.Equal(Color.Parse(plot.Palette.GetColor(trace.Trace)), Assert.IsType<SolidColorBrush>(trace.Brush).Color);
                Assert.Equal(plot.Palette.GetDashPattern(trace.Trace), trace.DashPattern.ToArray());
            }

            selector.SelectedItem = vm.Themes.Single(theme => theme.Code == "light");
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal(ThemeVariant.Light, window.ActualThemeVariant);
            Assert.Equal(Color.Parse("#F4F6FA"), Assert.IsType<SolidColorBrush>(window.Background).Color);
            Assert.Equal(PlotPalette.FromProfile(session.ProfileSnapshot!).GetColor(PlotTrace.Swr), plot.Palette.GetColor(PlotTrace.Swr));
            Assert.All(vm.Traces, trace => Assert.Empty(trace.DashPattern));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("night-vision")]
    public async Task LoadingSessionPreservesThemeAndOriginalStoredPalette(string theme)
    {
        await using var vm = TestData.ViewModel(new AppSettings { Theme = theme });
        var profile = DeviceProfile.Vna65MHz with { PlotColors = LegacyPlotColorSettings.Default with { Swr = 0x00FF00 } };
        vm.LoadSession(TestData.Session(profile));
        Assert.Equal(theme, vm.SelectedTheme.Code);
        Assert.Equal(profile, vm.CreateSession()!.ProfileSnapshot);
        Assert.Equal(theme == "night-vision", vm.PlotPalette.IsNightVision);
        vm.SelectedTheme = vm.Themes.Single(t => t.Code == "light");
        Assert.Equal("#00FF00", vm.PlotPalette.GetColor(PlotTrace.Swr));
    }

    [AvaloniaFact]
    public async Task OptionsReloadChangesThemeAndSavingPreservesOtherSettings()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Theme"] = "dark", ["Language"] = "pl-PL", ["PortName"] = "COM9",
        }).Build();
        var services = new ServiceCollection();
        services.Configure<AppSettings>(config);
        using var provider = services.BuildServiceProvider();
        await using var vm = new MainWindowViewModel(new DemoVnaDevice(DeviceProfile.Vna65MHz), new SerialPortCatalog(),
            provider.GetRequiredService<IOptionsMonitor<AppSettings>>(), "unused-settings.json");
        Assert.Equal("dark", vm.SelectedTheme.Code);
        config["Theme"] = "night-vision";
        config.Reload();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        Assert.True(vm.IsNightVision);
        var settings = vm.CreateAppSettings();
        Assert.Equal("COM9", settings.PortName);
        Assert.Equal("pl-PL", settings.Language);
        Assert.Equal("night-vision", settings.Theme);
        var restored = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;
        Assert.Equal(settings.PortName, restored.PortName);
        Assert.Equal(settings.Language, restored.Language);
        Assert.Equal(settings.Theme, restored.Theme);
        Assert.Equal(settings.TraceColors, restored.TraceColors);
        Assert.Equal("dark", JsonSerializer.Deserialize<AppSettings>("{\"PortName\":\"COM5\"}")!.Theme);
    }

    [AvaloniaFact]
    public async Task TraceColorsPersistResetAndRespectThemeOverrides()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        var swr = vm.Traces.Single(trace => trace.Trace == PlotTrace.Swr);
        swr.SelectedColor = "#000000";
        Assert.Equal("#000000", vm.PlotPalette.GetColor(PlotTrace.Swr));
        Assert.Equal("#000000", vm.CreateAppSettings().TraceColors["Swr"]);

        var settings = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(vm.CreateAppSettings()))!;
        await using var reloaded = TestData.ViewModel(settings);
        Assert.Equal("#000000", reloaded.Traces.Single(trace => trace.Trace == PlotTrace.Swr).SelectedColor);
        reloaded.SelectedTheme = reloaded.Themes.Single(theme => theme.Code == "dark");
        Assert.NotEqual("#000000", reloaded.PlotPalette.GetColor(PlotTrace.Swr));
        Assert.Equal("#000000", reloaded.Traces.Single(trace => trace.Trace == PlotTrace.Swr).SelectedColor);
        reloaded.SelectedTheme = reloaded.Themes.Single(theme => theme.Code == "night-vision");
        Assert.Equal("#C00000", reloaded.PlotPalette.GetColor(PlotTrace.Swr));
        Assert.False(reloaded.Traces.Single(trace => trace.Trace == PlotTrace.Swr).IsColorEditingEnabled);
        reloaded.SelectedTheme = reloaded.Themes.Single(theme => theme.Code == "light");
        Assert.Equal("#000000", reloaded.PlotPalette.GetColor(PlotTrace.Swr));

        reloaded.Traces.Single(trace => trace.Trace == PlotTrace.Swr).ResetColorCommand.Execute(null);
        Assert.Equal("#FF0000", reloaded.PlotPalette.GetColor(PlotTrace.Swr));
        Assert.DoesNotContain("Swr", reloaded.CreateAppSettings().TraceColors.Keys);
    }

    [AvaloniaFact]
    public async Task ThemeNamesFollowLanguageWithoutChangingSelectedTheme()
    {
        await using var vm = TestData.ViewModel(new AppSettings { Theme = "dark", Language = "pl-PL" });
        try
        {
            Assert.Equal("Ciemny", vm.SelectedTheme.DisplayName);
            vm.SelectedLanguage = vm.Languages.Single(l => l.Code == "en-US");
            Assert.Equal("Dark", vm.SelectedTheme.DisplayName);
            Assert.Equal("dark", vm.SelectedTheme.Code);
        }
        finally { vm.SelectedLanguage = vm.Languages.Single(l => l.Code == "pl-PL"); }
    }

    [AvaloniaFact]
    public async Task DenseNightTraceKeepsVisibleDashesAcrossSampleBoundaries()
    {
        var plot = new VnaPlotControl
        {
            Points = Enumerable.Range(0, 649).Select(i => TestData.Point(1_000_000 + i * 1000) with { ReturnLossDb = -11.3 }).ToArray(),
            VisibleTraces = PlotTrace.ReturnLoss,
            Palette = PlotPalette.FromProfile(DeviceProfile.Vna65MHz, AppThemeKind.NightVision),
        };
        var window = new Window { Content = plot, Width = 700, Height = 400, RequestedThemeVariant = AppThemes.NightVision };
        try
        {
            window.Show();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using var frame = window.GetLastRenderedFrame();
            Assert.NotNull(frame);
            using var png = new MemoryStream();
            frame.Save(png, PngBitmapEncoderOptions.Default);
            using var bitmap = SkiaSharp.SKBitmap.Decode(png.ToArray());
            var y = (int)Math.Floor(24 + 11.3 / 30 * (plot.Bounds.Height - 56));
            var pixels = Enumerable.Range(100, (int)plot.Bounds.Width - 180).Select(x => bitmap.GetPixel(x, y).Red > 80).ToArray();
            Assert.True(pixels.Count(value => value) > pixels.Length / 3, "The dashed trace has no visible strokes.");
            Assert.True(pixels.Count(value => !value) > pixels.Length / 5, "Dashes reset at sample boundaries and look solid.");
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task RenderThemePreviewsFromFullDemonstrationSweep()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await using var device = new DemoVnaDevice(DeviceProfile.Vna65MHz);
        await device.ConnectAsync(new DeviceConnectionOptions(string.Empty), cancellation);
        var sweep = new SweepSettings(1_000_000, 65_000_000, 649);
        var points = new List<MeasurementPoint>();
        await foreach (var point in device.SweepAsync(sweep, cancellation)) points.Add(point);
        await using var vm = TestData.ViewModel();
        vm.LoadSession(new MeasurementSession(1, device.Profile.Id, device.DataSourceName, DateTimeOffset.UnixEpoch, sweep, points)
        {
            ProfileSnapshot = device.Profile,
        });
        vm.PickMarker(14_200_000, false);
        vm.PickMarker(27_100_000, true);
        var window = new MainWindow(vm);
        try
        {
            window.Show();
            foreach (var theme in vm.Themes)
            {
                vm.SelectedTheme = theme;
                vm.Localizer.ChangeLanguage("en-US");
                await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
                Assert.Equal("VNA Studio", window.Title);
                var callsign = window.FindControl<TextBlock>("OperatorCallsign")!;
                Assert.Equal("SQ7MRU", callsign.Text);
                Assert.True(callsign.TranslatePoint(new Point(), window)!.Value.X > window.ClientSize.Width * 0.8);
                CaptureTheme(window, theme.Code);
                if (theme.Code == "dark")
                {
                    vm.SelectedChartView = AntennaChartView.Smith;
                    await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
                    CaptureTheme(window, "smith-dark");
                    vm.SelectedChartView = AntennaChartView.Both;
                    await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
                    CaptureTheme(window, "both-dark");
                    vm.SelectedChartView = AntennaChartView.Frequency;
                    await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
                }
                vm.Localizer.ChangeLanguage("pl-PL");
                await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
                CaptureTheme(window, theme.Code, "pl-");
            }
            window.Width = 1024;
            window.Height = 768;
            vm.Localizer.ChangeLanguage("en-US");
            vm.SelectedTheme = vm.Themes.Single(theme => theme.Code == "light");
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal(1024, window.ClientSize.Width);
            Assert.Equal(768, window.ClientSize.Height);
            CaptureTheme(window, "light", "minimum-");
            Assert.Equal(649, vm.PlotPoints.Count);
            Assert.Equal(PlotTrace.All, vm.VisibleTraces);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task NightControlsRemainRedDuringFocusHoverSelectionAndPopup()
    {
        await using var vm = TestData.ViewModel(new AppSettings { Theme = "night-vision" });
        vm.LoadSession(TestData.Session());
        var window = new MainWindow(vm);
        try
        {
            window.Show();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            var input = window.GetVisualDescendants().OfType<TextBox>().First();
            input.BringIntoView();
            input.Focus();
            input.SelectAll();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            AssertRedBrush(input.CaretBrush, "TextBox caret");
            AssertRedBrush(input.SelectionBrush, "TextBox selection");
            AssertRedBrush(input.SelectionForegroundBrush, "TextBox selected text");
            AssertRedControls(window);
            var selector = window.FindControl<ComboBox>("ThemeSelector")!;
            selector.BringIntoView();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            var hover = selector.TranslatePoint(new Point(20, 10), window)!.Value;
            window.MouseMove(hover);
            selector.IsDropDownOpen = true;
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            AssertRedControls(window);
            selector.IsDropDownOpen = false;
        }
        finally { window.Close(); }
    }

    private static void AssertRedControls(Window window)
    {
        foreach (var visual in window.GetVisualDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible && c.Opacity > 0))
        {
            var name = visual.GetType().Name + ":" + visual.Name;
            if (visual is TemplatedControl templated)
            {
                AssertRedBrush(templated.Background, name + " background");
                AssertRedBrush(templated.Foreground, name + " foreground");
                AssertRedBrush(templated.BorderBrush, name + " border");
            }
            if (visual is Border border) { AssertRedBrush(border.Background, name); AssertRedBrush(border.BorderBrush, name); }
            if (visual is TextBlock text) AssertRedBrush(text.Foreground, name);
            if (visual is ContentPresenter presenter) { AssertRedBrush(presenter.Background, name); AssertRedBrush(presenter.Foreground, name); }
            if (visual is Avalonia.Controls.Shapes.Shape shape) { AssertRedBrush(shape.Fill, name); AssertRedBrush(shape.Stroke, name); }
        }
    }

    private static void CaptureTheme(Window window, string code, string prefix = "")
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = window.GetLastRenderedFrame();
        Assert.NotNull(frame);
        var directory = Path.Combine(AppContext.BaseDirectory, "theme-previews");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, prefix + code + ".png");
        frame.Save(path, PngBitmapEncoderOptions.Default);
        if (code == "night-vision")
        {
            using var bitmap = SkiaSharp.SKBitmap.Decode(path);
            Assert.Equal(0, bitmap.Pixels.Count(pixel => pixel.Alpha > 0 && (pixel.Green > 0 || pixel.Blue > 0)));
        }
    }

    private static void AssertRedBrush(IBrush? brush, string name)
    {
        if (brush is ISolidColorBrush solid && solid.Color.A > 0)
            Assert.True(solid.Color.G == 0 && solid.Color.B == 0, $"{name} has a non-red color: {solid.Color}");
    }
}
