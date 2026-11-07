using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Vna.App;
using Vna.App.Plotting;
using Xunit;

namespace Vna.Tests;

public sealed class MainWindowResizeTests
{
    [AvaloniaFact]
    public async Task PlotAndSettingsRemainAvailableAtMinimumRestoredAndMaximizedSizes()
    {
        await using var viewModel = TestData.ViewModel();
        viewModel.LoadSession(TestData.Session());
        viewModel.PickMarker(2_000_000, isMarker2: false);
        viewModel.PickMarker(4_000_000, isMarker2: true);
        Assert.Equal(2_000_000, viewModel.Marker1Frequency);
        Assert.Equal(4_000_000, viewModel.Marker2Frequency);

        var window = new MainWindow(viewModel);
        try
        {
            window.Show();
            var settingsPanel = window.FindControl<Border>("SettingsPanel")!;
            var plot = window.FindControl<VnaPlotControl>("Plot")!;
            var markerTable = window.FindControl<Border>("MarkerValuesTable")!;

            await SetNormalSizeAndAssertLayoutAsync(window, settingsPanel, plot, markerTable, 1024, 768);
            SaveLayoutFrame(window, "minimum-1024x768.png");
            var minimumWidthPlot = plot.Bounds.Width;

            await SetNormalSizeAndAssertLayoutAsync(window, settingsPanel, plot, markerTable, 1360, 840);
            SaveLayoutFrame(window, "restored-1360x840.png");
            Assert.True(plot.Bounds.Width > minimumWidthPlot);

            window.WindowState = WindowState.Maximized;
            window.Width = 1920;
            window.Height = 1080;
            await UpdateLayoutAsync(window);
            Assert.Equal(WindowState.Maximized, window.WindowState);
            AssertLayoutAvailable(settingsPanel, plot, markerTable, 1200);
            SaveLayoutFrame(window, "maximized-1920x1080.png");

            window.WindowState = WindowState.Minimized;
            await UpdateLayoutAsync(window);
            Assert.Equal(WindowState.Minimized, window.WindowState);

            window.WindowState = WindowState.Normal;
            await SetNormalSizeAndAssertLayoutAsync(window, settingsPanel, plot, markerTable, 1200, 800);
        }
        finally
        {
            window.Close();
        }
    }

    private static async Task SetNormalSizeAndAssertLayoutAsync(
        MainWindow window,
        Border settingsPanel,
        VnaPlotControl plot,
        Border markerTable,
        double width,
        double height)
    {
        window.WindowState = WindowState.Normal;
        window.Width = width;
        window.Height = height;
        await UpdateLayoutAsync(window);
        AssertLayoutAvailable(settingsPanel, plot, markerTable, 200);
    }

    private static async Task UpdateLayoutAsync(MainWindow window)
    {
        await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }

    private static void SaveLayoutFrame(MainWindow window, string fileName)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "layout-review");
        Directory.CreateDirectory(directory);
        using var frame = window.GetLastRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(Path.Combine(directory, fileName), PngBitmapEncoderOptions.Default);
    }

    private static void AssertLayoutAvailable(
        Border settingsPanel,
        VnaPlotControl plot,
        Border markerTable,
        double minimumPlotWidth)
    {
        Assert.True(settingsPanel.IsVisible);
        Assert.True(settingsPanel.Bounds.Width >= 300);
        Assert.True(settingsPanel.Bounds.Height >= 400);
        Assert.True(plot.IsVisible);
        Assert.True(plot.Bounds.Width > minimumPlotWidth);
        Assert.True(plot.Bounds.Height > 150);
        Assert.True(markerTable.IsVisible);
        Assert.True(markerTable.Bounds.Width > minimumPlotWidth);
        Assert.True(markerTable.Bounds.Height > 20);
    }
}
