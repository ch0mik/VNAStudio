using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Vna.App;
using Vna.App.Plotting;
using Vna.Core;
using Vna.Devices;
using Xunit;

namespace Vna.Tests;

public sealed class PlotPerformanceMeasurementTests
{
    [AvaloniaFact]
    public async Task Render649SyntheticSamplesAndRecordLocalHeadlessTiming()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sweep = new SweepSettings(1_000_000, 65_000_000, 649);
        await using var demo = new DemoVnaDevice(Vna65MHzDeviceProfile.Default);
        await demo.ConnectAsync(new DeviceConnectionOptions(string.Empty), cancellationToken);
        var points = await demo.SweepAsync(sweep, cancellationToken).ToListAsync(cancellationToken);

        await using var viewModel = TestData.ViewModel();
        viewModel.LoadSession(TestData.Session() with { Sweep = sweep, Points = points, DataSource = "demo" });
        viewModel.PickMarker(sweep.FrequencyAt(130), isMarker2: false);
        viewModel.PickMarker(sweep.FrequencyAt(270), isMarker2: true);
        Assert.Equal(649, viewModel.PlotPoints.Count);
        Assert.Equal(PlotTrace.All, viewModel.VisibleTraces);

        var window = new MainWindow(viewModel) { Width = 1440, Height = 900 };
        try
        {
            window.Show();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            await RenderFrameAsync(window);

            const int measuredFrames = 20;
            var elapsedMilliseconds = new double[measuredFrames];
            for (var index = 0; index < measuredFrames; index++)
            {
                var stopwatch = Stopwatch.StartNew();
                await RenderFrameAsync(window);
                stopwatch.Stop();
                elapsedMilliseconds[index] = stopwatch.Elapsed.TotalMilliseconds;
            }

            Array.Sort(elapsedMilliseconds);
            var median = (elapsedMilliseconds[(measuredFrames / 2) - 1] + elapsedMilliseconds[measuredFrames / 2]) / 2;
            var report = $"Avalonia headless render timing; synthetic DemoVnaDevice data; points={points.Count}; visibleTraces={viewModel.VisibleTraces}; window=1440x900 DIP; measuredFrames={measuredFrames}; medianInvalidateLayoutRenderAndCaptureMs={median:F3}; valuesMs={string.Join(",", elapsedMilliseconds.Select(value => value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)))}";

            var directory = Path.Combine(AppContext.BaseDirectory, "layout-review");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "plot-649-performance.txt"), report);
            using var frame = window.GetLastRenderedFrame();
            Assert.NotNull(frame);
            frame.Save(Path.Combine(directory, "plot-649.png"), PngBitmapEncoderOptions.Default);
        }
        finally
        {
            window.Close();
        }
    }

    private static async Task RenderFrameAsync(MainWindow window)
    {
        window.FindControl<Vna.App.Plotting.VnaPlotControl>("Plot")!.InvalidateVisual();
        await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = window.GetLastRenderedFrame();
        Assert.NotNull(frame);
    }
}
