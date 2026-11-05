using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Vna.App;
using Vna.App.Infrastructure;
using Vna.App.Plotting;
using Xunit;

namespace Vna.Tests;

public sealed class PlotZoomTests
{
    [AvaloniaFact]
    public async Task ScaleZoomKeepsDataAndMarkersAndSharesTheViewHistory()
    {
        await using var vm = TestData.ViewModel();
        Assert.False(vm.ZoomInCommand.CanExecute(null));
        Assert.False(vm.ZoomOutCommand.CanExecute(null));
        vm.LoadSession(TestData.Session());
        vm.PickMarker(2_000_000, false);
        vm.PickMarker(4_000_000, true);
        var points = vm.PlotPoints;
        var start = vm.StartMHz;
        var stop = vm.StopMHz;
        vm.ZoomInCommand.Execute(null);
        Assert.Equal(2_000_000, vm.ViewStartHz);
        Assert.Equal(4_000_000, vm.ViewStopHz);
        vm.ZoomOutCommand.Execute(null);
        Assert.Equal(1_000_000, vm.ViewStartHz);
        Assert.Equal(5_000_000, vm.ViewStopHz);
        vm.RestoreZoomCommand.Execute(null);
        Assert.Equal(2_000_000, vm.ViewStartHz);
        Assert.Equal(4_000_000, vm.ViewStopHz);
        vm.RestoreZoomCommand.Execute(null);
        Assert.Equal(0, vm.ViewStartHz);
        Assert.Equal(0, vm.ViewStopHz);
        Assert.Same(points, vm.PlotPoints);
        Assert.Equal(2_000_000, vm.Marker1Frequency);
        Assert.Equal(4_000_000, vm.Marker2Frequency);
        Assert.Equal(start, vm.StartMHz);
        Assert.Equal(stop, vm.StopMHz);
    }

    [AvaloniaFact]
    public async Task ScaleZoomStopsAtOneHertzAndTheProfileBoundary()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        for (var attempt = 0; attempt < 64 && vm.CanZoomIn; attempt++) vm.ZoomInCommand.Execute(null);
        Assert.Equal(1, vm.ViewStopHz - vm.ViewStartHz);
        Assert.False(vm.ZoomInCommand.CanExecute(null));
        for (var attempt = 0; attempt < 64 && vm.CanZoomOut; attempt++)
        {
            vm.ZoomOutCommand.Execute(null);
            Assert.InRange(vm.ViewStartHz, vm.Profile.MinimumFrequencyHz, vm.Profile.MaximumFrequencyHz);
            Assert.InRange(vm.ViewStopHz, vm.ViewStartHz + 1, vm.Profile.MaximumFrequencyHz);
        }
        Assert.Equal(vm.Profile.MinimumFrequencyHz, vm.ViewStartHz);
        Assert.Equal(vm.Profile.MaximumFrequencyHz, vm.ViewStopHz);
        Assert.False(vm.ZoomOutCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task ButtonsAndPlotKeysZoomAtTheMinimumWindowSize()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        var window = new MainWindow(vm) { Width = 1024, Height = 768 };
        try
        {
            window.Show();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            var plus = window.FindControl<Button>("ZoomInButton")!;
            var minus = window.FindControl<Button>("ZoomOutButton")!;
            var plot = window.FindControl<VnaPlotControl>("Plot")!;
            var plusPosition = plus.TranslatePoint(new Point(plus.Bounds.Width / 2, plus.Bounds.Height / 2), window)!.Value;
            window.MouseDown(plusPosition, MouseButton.Left);
            window.MouseUp(plusPosition, MouseButton.Left);
            Assert.Equal(2_000_000, plot.ViewStartHz);
            Assert.Equal(4_000_000, plot.ViewStopHz);
            var minusPosition = minus.TranslatePoint(new Point(minus.Bounds.Width / 2, minus.Bounds.Height / 2), window)!.Value;
            window.MouseDown(minusPosition, MouseButton.Left);
            window.MouseUp(minusPosition, MouseButton.Left);
            Assert.Equal(1_000_000, plot.ViewStartHz);
            Assert.Equal(5_000_000, plot.ViewStopHz);
            plot.Focus();
            window.KeyPress(Key.Add, RawInputModifiers.None, default, "+");
            Assert.Equal(2_000_000, plot.ViewStartHz);
            window.KeyPress(Key.Subtract, RawInputModifiers.None, default, "-");
            Assert.Equal(1_000_000, plot.ViewStartHz);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task SameSweepRefreshPreservesZoomAndChangedSweepResetsIt()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        vm.ZoomInCommand.Execute(null);
        await ((AsyncRelayCommand)vm.StartCommand).ExecuteAsync();
        Assert.False(vm.HasError, vm.Error);
        Assert.Equal(2_000_000, vm.ViewStartHz);
        Assert.Equal(4_000_000, vm.ViewStopHz);
        vm.StartMHz = "2";
        vm.StopMHz = "4";
        await ((AsyncRelayCommand)vm.StartCommand).ExecuteAsync();
        Assert.False(vm.HasError, vm.Error);
        Assert.Equal(0, vm.ViewStartHz);
        Assert.Equal(0, vm.ViewStopHz);
    }
}
