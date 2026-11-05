using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Vna.App;
using Vna.App.Plotting;
using Vna.App.ViewModels;
using Vna.App.Theming;
using Vna.IO;
using Vna.Core;
using Vna.Devices;
using Xunit;
using System.Diagnostics;

namespace Vna.Tests;

public sealed class PlotInteractionTests
{
    [Theory]
    [InlineData(3_842_000L, 7_148_000L, 2)]
    [InlineData(18_582_000L, 21_532_000L, 7)]
    [InlineData(5_000_000L, 5_000_000L, null)]
    [InlineData(null, 5_000_000L, null)]
    public void MarkerQEstimateReproducesScreenshotHintWithoutClaimingResonance(
        long? first, long? second, int? expected)
    {
        Assert.Equal(expected, MarkerCalculations.EstimateMarkerQ(first, second));
        Assert.Equal(expected, MarkerCalculations.EstimateMarkerQ(second, first));
    }

    [AvaloniaFact]
    public async Task DemoSweepReportsPointProgressAndCompletedScanDuration()
    {
        await using var vm = TestData.ViewModel();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.IsScanning) && !vm.IsScanning) completed.TrySetResult();
        };
        vm.StartCommand.Execute(null);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(vm.Profile.DefaultPointCount, vm.ScanProgressValue);
        Assert.Equal(vm.Profile.DefaultPointCount, vm.ScanProgressMaximum);
        Assert.Contains("ms", vm.ScanProgressDisplay, StringComparison.Ordinal);
        Assert.True(vm.HasUnsavedResult);
    }

    [AvaloniaFact]
    public async Task ClearingHoldAutomaticallyStartsContinuousScanningUntilHoldIsRestored()
    {
        await using var vm = TestData.ViewModel();
        vm.StartMHz = "1";
        vm.StopMHz = "5";
        vm.PointCount = "5";
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedSweeps = 0;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.PlotPoints) && vm.PlotPoints.Count == 5)
            {
                completedSweeps++;
                if (completedSweeps == 2) vm.IsHold = true;
            }
            if (args.PropertyName == nameof(vm.IsScanning) && !vm.IsScanning) stopped.TrySetResult();
        };

        vm.IsHold = false;
        vm.HoldCommand.Execute(null);
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(2, completedSweeps);
        Assert.True(vm.IsHold);
        Assert.False(vm.IsContinuousScan);
        Assert.False(vm.IsScanning);
        Assert.Equal(5, vm.PlotPoints.Count);
    }

    [AvaloniaFact]
    public async Task ContinuousSweepCommitsEachCompleteResultAndHoldStopsAfterCurrentSweep()
    {
        await using var vm = TestData.ViewModel();
        vm.StartMHz = "1";
        vm.StopMHz = "5";
        vm.PointCount = "5";
        vm.IsContinuousScan = true;
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedSweeps = 0;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.PlotPoints) && vm.PlotPoints.Count == 5)
            {
                completedSweeps++;
                if (completedSweeps == 2) vm.IsHold = true;
            }
            if (args.PropertyName == nameof(vm.IsScanning) && !vm.IsScanning) stopped.TrySetResult();
        };

        vm.StartCommand.Execute(null);
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(2, completedSweeps);
        Assert.False(vm.IsScanning);
        Assert.Equal(5, vm.ScanProgressValue);
        Assert.Equal(5, vm.PlotPoints.Count);
        Assert.Contains("Hold", vm.ContinuousScanHint, StringComparison.OrdinalIgnoreCase);
    }

    [AvaloniaFact]
    public async Task ContinuousSweepCanBeHeldDuringPauseBeforeNextSweep()
    {
        await using var vm = TestData.ViewModel();
        vm.StartMHz = "1";
        vm.StopMHz = "5";
        vm.PointCount = "5";
        vm.IsContinuousScan = true;
        var firstCompleteSweep = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedSweeps = 0;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.PlotPoints) && vm.PlotPoints.Count == 5)
            {
                completedSweeps++;
                firstCompleteSweep.TrySetResult();
            }
            if (args.PropertyName == nameof(vm.IsScanning) && !vm.IsScanning) stopped.TrySetResult();
        };

        vm.StartCommand.Execute(null);
        await firstCompleteSweep.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        vm.IsHold = true;
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(1, completedSweeps);
        Assert.False(vm.IsScanning);
        Assert.Equal(5, vm.PlotPoints.Count);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContinuousSweepCancellationOrFailureKeepsPreviousCompleteResult(bool failSecondSweep)
    {
        var device = new InterruptedSecondSweepDevice(failSecondSweep);
        await using var vm = TestData.ViewModel(device: device);
        vm.StartMHz = "1";
        vm.StopMHz = "5";
        vm.PointCount = "5";
        vm.IsContinuousScan = true;
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondSweepStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeSweeps = 0;
        var previousDuration = string.Empty;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.PlotPoints) && vm.PlotPoints.Count == 5)
                completeSweeps++;
            if (args.PropertyName == nameof(vm.ScanProgressDisplay) && completeSweeps > 0 && vm.ScanProgressValue == 5)
                previousDuration = vm.ScanProgressDisplay.Split('·').Last().Trim();
            if (args.PropertyName == nameof(vm.ScanProgressValue) && vm.IsScanning &&
                completeSweeps > 0 && vm.ScanProgressValue == 1)
                secondSweepStarted.TrySetResult();
            if (args.PropertyName == nameof(vm.IsScanning) && !vm.IsScanning) stopped.TrySetResult();
        };

        vm.StartCommand.Execute(null);
        if (failSecondSweep)
        {
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        else
        {
            await secondSweepStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(1, vm.ScanProgressValue);
            vm.StopCommand.Execute(null);
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, completeSweeps);
        Assert.Equal(5, vm.PlotPoints.Count);
        Assert.Equal(5, vm.CreateSession()!.Points.Count);
        Assert.Equal(previousDuration, vm.ScanProgressDisplay.Split('·').Last().Trim());
        Assert.False(device.IsConnected);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledOrFailedSecondSweepKeepsLastCompleteDataAndDuration(bool failSecondSweep)
    {
        var device = new InterruptedSecondSweepDevice(failSecondSweep);
        await using var vm = TestData.ViewModel(device: device);
        vm.PointCount = "5";
        vm.StartMHz = "1";
        vm.StopMHz = "5";
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var partialSecondSweep = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.IsScanning) && !vm.IsScanning) completed.TrySetResult();
            if (args.PropertyName == nameof(vm.ScanProgressValue) && vm.IsScanning && vm.ScanProgressValue == 1)
                partialSecondSweep.TrySetResult();
        };

        vm.StartCommand.Execute(null);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var completePoints = vm.PlotPoints.ToArray();
        Assert.Equal(5, completePoints.Length);
        var previousDuration = vm.ScanProgressDisplay.Split('·').Last().Trim();

        completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.StartCommand.Execute(null);
        await partialSecondSweep.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, vm.ScanProgressValue);
        Assert.Equal(completePoints, vm.PlotPoints.ToArray());
        if (failSecondSweep)
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        else
            vm.StopCommand.Execute(null);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(completePoints, vm.PlotPoints.ToArray());
        Assert.Equal(previousDuration, vm.ScanProgressDisplay.Split('·').Last().Trim());
        Assert.Equal(5, vm.CreateSession()!.Points.Count);
    }

    [Theory]
    [InlineData(3_842_000L, 7_148_000L, 3_306_000L, 2)]
    [InlineData(18_582_000L, 21_532_000L, 2_950_000L, 7)]
    public async Task MarkerReadoutsReproduceBandwidthAndQShownInReferenceScreenshots(
        long startHz, long stopHz, long expectedBandwidthHz, int expectedQ)
    {
        await using var vm = TestData.ViewModel();
        var profile = DeviceProfile.Vna65MHz;
        var sweep = new SweepSettings(startHz, stopHz, 2);
        var session = new MeasurementSession(2, profile.Id, "reference-screenshot", DateTimeOffset.UnixEpoch,
            sweep, new[] { TestData.Point(startHz), TestData.Point(stopHz) }) { ProfileSnapshot = profile };
        vm.LoadSession(session);
        vm.PickMarker(startHz, isMarker2: false);
        vm.PickMarker(stopHz, isMarker2: true);
        Assert.Equal(expectedBandwidthHz, vm.BandwidthHz);
        Assert.Equal(expectedQ, vm.MarkerQEstimate);
    }

    [AvaloniaFact]
    public async Task BandwidthFormattingFollowsTheSelectedLanguageImmediately()
    {
        await using var vm = TestData.ViewModel();
        var profile = DeviceProfile.Vna65MHz;
        var sweep = new SweepSettings(3_842_000, 7_148_000, 2);
        vm.LoadSession(new MeasurementSession(2, profile.Id, "test", DateTimeOffset.UnixEpoch, sweep,
            new[] { TestData.Point(3_842_000), TestData.Point(7_148_000) }) { ProfileSnapshot = profile });
        vm.PickMarker(3_842_000, isMarker2: false);
        vm.PickMarker(7_148_000, isMarker2: true);
        Assert.Equal("3,306 MHz", vm.BandwidthText);
        vm.Localizer.ChangeLanguage("en-US");
        Assert.Equal("3.306 MHz", vm.BandwidthText);
    }

    [AvaloniaFact]
    public async Task CableLengthModeEstimatesFromSameTypeResonanceMarkerSpacingAndPersistedVelocityFactor()
    {
        await using var vm = TestData.ViewModel();
        var profile = DeviceProfile.Vna65MHz;
        var sweep = new SweepSettings(1_000_000, 9_000_000, 9);
        var session = new MeasurementSession(2, profile.Id, "cable-test", DateTimeOffset.UnixEpoch, sweep,
            new[] { 1d, 4d, 1d, 4d, 1d, 4d, 1d, 4d, 1d }
                .Select((swr, index) => TestData.Point(sweep.FrequencyAt(index), swr)).ToArray())
        {
            ProfileSnapshot = profile,
        };
        vm.LoadSession(session);
        vm.PickMarker(2_000_000, isMarker2: false);
        vm.PickMarker(4_000_000, isMarker2: true);
        vm.CableVelocityFactorText = "0.66";
        vm.Localizer.ChangeLanguage("en-US");

        vm.SelectCableLengthModeCommand.Execute(null);

        var expectedMeters = 0.66 * CableLengthCalculator.SpeedOfLightMetersPerSecond / 4_000_000;
        Assert.True(vm.IsCableLengthAnalysisMode);
        Assert.False(vm.IsUnavailableMode);
        Assert.Equal(expectedMeters, vm.CableLengthMeters!.Value, 9);
        Assert.Equal($"{expectedMeters.ToString("0.###", System.Globalization.CultureInfo.GetCultureInfo("en-US"))} m",
            vm.CableLengthResultText);
        Assert.Equal(0.66, vm.CreateAppSettings().CableVelocityFactor);

        vm.PickMarker(8_000_000, isMarker2: true);
        Assert.Null(vm.CableLengthMeters);
        Assert.Contains("adjacent", vm.CableLengthResultText, StringComparison.OrdinalIgnoreCase);

        vm.CableVelocityFactorText = "1.5";
        Assert.Null(vm.CableLengthMeters);
        Assert.Contains("greater than 0", vm.CableLengthResultText, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task XtalModeCalculatesDocumentedManualResonanceQExample()
    {
        await using var vm = TestData.ViewModel();
        vm.ResonanceFrequencyMHz = "14.4";
        vm.LowerEdgeMHz = "14.312";
        vm.UpperEdgeMHz = "14.4816";
        vm.Localizer.ChangeLanguage("en-US");
        vm.SelectXtalModeCommand.Execute(null);

        Assert.True(vm.IsXtalAnalysisMode);
        Assert.False(vm.IsUnavailableMode);
        Assert.Equal(84.90566037735849, vm.ResonanceQ!.Value, 8);
        Assert.Equal("Q = 84.9", vm.ResonanceQResultText);
        Assert.False(vm.CanSaveSession);

        vm.ResonanceFrequencyMHz = "0.5";
        Assert.Null(vm.ResonanceQ);
        vm.ResonanceFrequencyMHz = "14.4";
        vm.LowerEdgeMHz = "14.5";
        Assert.Null(vm.ResonanceQ);
        vm.LowerEdgeMHz = "14.312";
        vm.UpperEdgeMHz = "66";
        Assert.Null(vm.ResonanceQ);
    }

    [AvaloniaFact]
    public async Task GeneratorModeLoadsTenIniMemoriesButDoesNotPretendToEnableHardwareOutput()
    {
        await using var vm = TestData.ViewModel();
        Assert.Equal(10, vm.GeneratorMemories.Count);
        Assert.Equal(27_195_000, vm.GeneratorMemories[7].FrequencyHz);
        vm.SelectedGeneratorMemory = vm.GeneratorMemories[7];
        vm.Localizer.ChangeLanguage("en-US");
        Assert.Equal("27.195", vm.GeneratorFrequencyMHz);
        Assert.True(vm.IsGeneratorFrequencyValid);
        vm.SelectGeneratorModeCommand.Execute(null);
        Assert.True(vm.IsGeneratorAnalysisMode);
        Assert.False(vm.IsUnavailableMode);
        Assert.False(vm.CanEnableGeneratorOutput);
        Assert.False(vm.CanSaveSession);
        vm.GeneratorFrequencyMHz = "65.001";
        Assert.False(vm.IsGeneratorFrequencyValid);
        Assert.False(vm.CanEnableGeneratorOutput);
    }

    [AvaloniaFact]
    public async Task AmateurBandPresetsSetOnlyProfileValidSweepEdges()
    {
        await using var vm = TestData.ViewModel();
        Assert.Equal(9, vm.VisibleAmateurBands.Count);
        vm.SelectedBandPreset = vm.VisibleAmateurBands.Single(band => band.Name == "80 m");
        Assert.Equal(3.5, double.Parse(vm.StartMHz, vm.Localizer.Culture));
        Assert.Equal(3.8, double.Parse(vm.StopMHz, vm.Localizer.Culture));

        vm.SelectVhfBandsCommand.Execute(null);
        Assert.Empty(vm.VisibleAmateurBands);
        Assert.Contains("65", vm.BandGroupHint, StringComparison.Ordinal);
        Assert.Equal(65_000_000, vm.Profile.MaximumFrequencyHz);
    }

    [AvaloniaFact]
    public async Task LegacyBandRangesOverrideShortcutsAndOutOfProfileBandsAreHidden()
    {
        var profile = DeviceProfile.Vna65MHz with
        {
            LegacyBandPresets =
            [
                new AmateurBandPreset("custom HF", AmateurBandGroup.Hf, 3_500_000, 3_800_000),
                new AmateurBandPreset("legacy 2 m", AmateurBandGroup.Vhf, 144_000_000, 146_000_000),
            ],
        };
        await using var vm = TestData.ViewModel(device: new DemoVnaDevice(profile));

        Assert.Collection(vm.VisibleAmateurBands, band => Assert.Equal("custom HF", band.Name));
        vm.SelectedBandPreset = vm.VisibleAmateurBands[0];
        Assert.Equal((3.5).ToString("0.###", vm.Localizer.Culture), vm.StartMHz);
        Assert.Equal((3.8).ToString("0.###", vm.Localizer.Culture), vm.StopMHz);

        vm.SelectedBandGroup = AmateurBandGroup.Vhf;
        Assert.Empty(vm.VisibleAmateurBands);
    }

    [AvaloniaFact]
    public async Task OptionalXcXlReadoutConvertsSignedReactanceUsingMarkerFrequency()
    {
        await using var vm = TestData.ViewModel(new AppSettings { ShowXcXl = true, Language = "pl-PL" });
        var session = TestData.Session();
        var points = session.Points.ToArray();
        points[0] = points[0] with { ReactanceOhms = -10 };
        points[1] = points[1] with { ReactanceOhms = 10 };
        vm.LoadSession(session with { Points = points });
        vm.PickMarker(1_000_000, isMarker2: false);
        Assert.EndsWith("pF", vm.Marker1Values.ReactanceComponent, StringComparison.Ordinal);
        Assert.Contains("15", vm.Marker1Values.ReactanceComponent, StringComparison.Ordinal);
        vm.PickMarker(2_000_000, isMarker2: false);
        Assert.EndsWith("µH", vm.Marker1Values.ReactanceComponent, StringComparison.Ordinal);
        Assert.Contains("0,796", vm.Marker1Values.ReactanceComponent, StringComparison.Ordinal);
        Assert.True(vm.ShowXcXl);
    }

    [AvaloniaFact]
    public void WindowLoadsCompiledXamlWithoutGeneratedFieldsOrViewModel()
    {
        var window = new MainWindow();
        Assert.NotNull(window.FindControl<VnaPlotControl>("Plot"));
        Assert.Equal(1024, window.MinWidth);
        Assert.Equal(768, window.MinHeight);
        window.Close();
    }

    [AvaloniaFact]
    public async Task ReturnLossOffsetRecalculatesDisplayedQuantitiesWithoutChangingSessionSamples()
    {
        await using var vm = TestData.ViewModel();
        var session = TestData.Session();
        session = session with
        {
            ProfileSnapshot = session.ProfileSnapshot! with { ReferenceImpedanceOhms = 75 },
            Points = session.Points.Select(point => point with
            {
                PhaseSignConfidence = PhaseSignConfidence.Verified,
                PhaseSignProvenance = PhaseSignProvenance.ReferenceLoadValidation,
            }).ToArray(),
        };
        vm.LoadSession(session);
        var original = session.Points[0];
        vm.PickMarker(original.FrequencyHz, isMarker2: false);
        vm.ReturnLossDisplayOffsetText = "-2";

        var displayed = vm.PlotPoints[0];
        var expected = RfSampleCalculator.FromReturnLossAndPhase(
            original.FrequencyHz, original.ReturnLossDb!.Value - 2, original.PhaseDegrees!.Value, session.ProfileSnapshot!);
        Assert.Equal(original.ReturnLossDb, vm.Points[0].ReturnLossDb);
        Assert.Equal(expected.ReturnLossDb, displayed.ReturnLossDb);
        Assert.Equal(expected.Swr, displayed.Swr);
        Assert.Equal(expected.ImpedanceOhms, displayed.ImpedanceOhms);
        Assert.Equal(original.PhaseSignConfidence, displayed.PhaseSignConfidence);
        Assert.Equal(original.PhaseSignProvenance, displayed.PhaseSignProvenance);
        Assert.Equal(expected.ReturnLossDb!.Value.ToString("0.0", vm.Localizer.Culture), vm.Marker1Values.ReturnLoss);
        Assert.Equal(-2, vm.CreateAppSettings().ReturnLossDisplayOffsetDb);

        vm.ReturnLossDisplayOffsetText = "not a number";
        Assert.False(vm.IsReturnLossDisplayOffsetValid);
        Assert.Equal(expected.ReturnLossDb, vm.PlotPoints[0].ReturnLossDb);
        Assert.Equal(-2, vm.CreateAppSettings().ReturnLossDisplayOffsetDb);
    }

    [AvaloniaFact]
    public async Task LoadedSessionWithoutProfileSnapshotDoesNotAcquireCurrentProfileWhenSavedAgain()
    {
        await using var vm = TestData.ViewModel();
        var legacy = TestData.Session() with { FormatVersion = 3, ProfileSnapshot = null };
        vm.LoadSession(legacy);

        Assert.Null(vm.CreateSession()?.ProfileSnapshot);
    }

    [AvaloniaTheory]
    [InlineData(1024, 768, 1, AppThemeKind.Light)]
    [InlineData(1440, 900, 1.25, AppThemeKind.Dark)]
    [InlineData(1920, 1080, 1.5, AppThemeKind.NightVision)]
    public void SmithControlRendersResponsivelyWithThemeAndDpi(int width, int height, double scaling, AppThemeKind theme)
    {
        var profile = DeviceProfile.Vna65MHz;
        var chart = new SmithPlotControl
        {
            ReferenceImpedanceOhms = profile.ReferenceImpedanceOhms,
            Palette = PlotPalette.FromProfile(profile, theme),
            LanguageCode = "en-US",
            Points =
            [
                TestData.Point(1_000_000) with { PhaseSignConfidence = PhaseSignConfidence.Ambiguous },
                TestData.Point(2_000_000) with { ReturnLossDb = 0.001, PhaseSignConfidence = PhaseSignConfidence.Ambiguous },
                TestData.Point(3_000_000) with { PhaseDegrees = null, PhaseSignConfidence = PhaseSignConfidence.Missing },
            ],
        };
        var window = new Window { Content = chart };
        try
        {
            window.Show();
            window.SetRenderScaling(scaling);
            window.Width = width;
            window.Height = height;
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();

            Assert.Equal(width, chart.Bounds.Width, precision: 0);
            Assert.Equal(height, chart.Bounds.Height, precision: 0);
            Assert.Equal(1, chart.CountOutsideUnitCircle);
            Assert.True(chart.Bounds.Width >= 1024);
            Assert.True(chart.Bounds.Height >= 768);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task SmithControlRendersMaximumSweepWithoutDroppingSamples()
    {
        var sweep = new SweepSettings(1_000_000, 65_000_000, 4000);
        var points = Enumerable.Range(0, sweep.PointCount)
            .Select(index => TestData.Point(sweep.FrequencyAt(index)))
            .ToArray();
        var control = new SmithPlotControl
        {
            Points = points,
            ReferenceImpedanceOhms = 50,
            Palette = PlotPalette.FromProfile(DeviceProfile.Vna65MHz, AppThemeKind.Dark),
            LanguageCode = "en-US",
            Width = 1000,
            Height = 600,
        };
        var window = new Window { Content = control, Width = 1024, Height = 768 };
        try
        {
            window.Show();
            var timer = Stopwatch.StartNew();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            timer.Stop();
            using var frame = window.GetLastRenderedFrame();
            Assert.NotNull(frame);
            Assert.Equal(4000, control.Points.Count);
            Console.WriteLine($"Smith chart 4000-sample Avalonia render: {timer.Elapsed.TotalMilliseconds:F1} ms.");
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), $"4000-point Smith render took {timer.Elapsed}.");
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task SmithViewSharesFrequencyWindowAndMarkersAndSupportsZoomPan()
    {
        await using var viewModel = TestData.ViewModel();
        var sourceSession = TestData.Session();
        viewModel.LoadSession(sourceSession with
        {
            ProfileSnapshot = sourceSession.ProfileSnapshot! with { ReferenceImpedanceOhms = 75 },
            Points = sourceSession.Points.Select((point, index) => point with
            {
                ReturnLossDb = -5 - index * 2,
                PhaseDegrees = 20 + index * 25,
            }).ToArray(),
        });
        var originalPoints = viewModel.Points.ToArray();
        var window = new MainWindow(viewModel);
        try
        {
            window.Show();
            window.Width = 1024;
            window.Height = 768;
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();

            var frequency = window.FindControl<VnaPlotControl>("Plot")!;
            var smith = window.FindControl<SmithPlotControl>("SmithPlot")!;
            Assert.True(frequency.IsVisible);
            Assert.False(smith.IsVisible);
            Assert.Equal(viewModel.PlotPoints, frequency.Points);

            viewModel.SelectSmithChartCommand.Execute(null);
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.False(frequency.IsVisible);
            Assert.True(smith.IsVisible);
            Assert.Equal(75, smith.ReferenceImpedanceOhms);

            var selectedPoint = viewModel.PlotPoints[2];
            Assert.True(SmithChartMath.TryGetReflectionFromSignedS11(
                selectedPoint.ReturnLossDb!.Value, selectedPoint.PhaseDegrees!.Value, out var reflection));
            var plotAreaWidth = smith.Bounds.Width - 68;
            var plotAreaHeight = smith.Bounds.Height - 68;
            var transform = new SmithChartTransform(plotAreaWidth, plotAreaHeight);
            var pixel = transform.ToPixel(reflection);
            var target = smith.TranslatePoint(new Point(34 + pixel.X, 34 + pixel.Y), window)!.Value;
            window.MouseDown(target, MouseButton.Left);
            window.MouseUp(target, MouseButton.Left);
            Assert.Equal(selectedPoint.FrequencyHz, viewModel.Marker1Frequency);

            var mirroredPoint = viewModel.PlotPoints[3];
            Assert.True(SmithChartMath.TryGetReflectionFromSignedS11(
                mirroredPoint.ReturnLossDb!.Value, mirroredPoint.PhaseDegrees!.Value, out var mirroredReflection));
            var mirrorPixel = transform.ToPixel(mirroredReflection.Conjugate);
            var mirrorTarget = smith.TranslatePoint(new Point(34 + mirrorPixel.X, 34 + mirrorPixel.Y), window)!.Value;
            window.MouseDown(mirrorTarget, MouseButton.Right);
            window.MouseUp(mirrorTarget, MouseButton.Right);
            Assert.Equal(mirroredPoint.FrequencyHz, viewModel.Marker2Frequency);
            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, string.Empty);
            Assert.Equal(viewModel.PlotPoints[4].FrequencyHz, viewModel.Marker2Frequency);

            var zoomButton = window.FindControl<Button>("SmithZoomInButton")!;
            var zoomButtonPosition = zoomButton.TranslatePoint(
                new Point(zoomButton.Bounds.Width / 2, zoomButton.Bounds.Height / 2), window)!.Value;
            window.MouseDown(zoomButtonPosition, MouseButton.Left);
            window.MouseUp(zoomButtonPosition, MouseButton.Left);
            Assert.Equal(2, viewModel.SmithZoom);
            var center = smith.TranslatePoint(new Point(smith.Bounds.Width / 2, smith.Bounds.Height / 2), window)!.Value;
            var panned = new Point(center.X + 30, center.Y + 20);
            window.MouseDown(center, MouseButton.Middle);
            window.MouseMove(panned, RawInputModifiers.MiddleMouseButton);
            window.MouseUp(panned, MouseButton.Middle);
            Assert.NotEqual(0, viewModel.SmithPanReal);
            Assert.NotEqual(0, viewModel.SmithPanImaginary);
            viewModel.ResetSmithViewCommand.Execute(null);

            viewModel.SelectBothChartsCommand.Execute(null);
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var frequencyBoth = window.FindControl<VnaPlotControl>("PlotBoth")!;
            var smithBoth = window.FindControl<SmithPlotControl>("SmithPlotBoth")!;
            Assert.True(frequencyBoth.IsVisible);
            Assert.True(smithBoth.IsVisible);
            Assert.Equal(viewModel.PlotPoints, smithBoth.Points);

            var oldViewStart = viewModel.ViewStartHz;
            var oldViewStop = viewModel.ViewStopHz;
            viewModel.ChangeSmithZoom(2, new SmithComplex(0.1, 0.2), 0, 0);
            Assert.Equal(2, viewModel.SmithZoom);
            Assert.Equal(oldViewStart, viewModel.ViewStartHz);
            Assert.Equal(oldViewStop, viewModel.ViewStopHz);
            viewModel.PanSmith(0.1, -0.2);
            Assert.Equal(0.2, viewModel.SmithPanReal, 12);
            Assert.Equal(0, viewModel.SmithPanImaginary, 12);

            viewModel.ZoomInCommand.Execute(null);
            Assert.True(viewModel.ViewStopHz > viewModel.ViewStartHz);
            Assert.Equal(originalPoints, viewModel.Points.ToArray());
            Assert.Equal(viewModel.ViewStartHz, smithBoth.ViewStartHz);
            Assert.Equal(viewModel.ViewStopHz, smithBoth.ViewStopHz);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task FrequencyPlotPansHorizontallyAfterZoomAndKeepsTheVisibleSpan()
    {
        await using var viewModel = TestData.ViewModel();
        viewModel.LoadSession(TestData.Session());
        viewModel.ZoomInCommand.Execute(null);
        var initialStart = viewModel.ViewStartHz;
        var initialStop = viewModel.ViewStopHz;
        var visibleSpan = initialStop - initialStart;
        Assert.True(visibleSpan > 0);

        var window = new MainWindow(viewModel);
        try
        {
            window.Show();
            window.Width = 1024;
            window.Height = 768;
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            var plot = window.FindControl<VnaPlotControl>("Plot")!;
            var initial = plot.TranslatePoint(new Point(plot.Bounds.Width * 0.65, plot.Bounds.Height * 0.5), window)!.Value;
            var moved = new Point(initial.X + 100, initial.Y);
            window.MouseDown(initial, MouseButton.Middle);
            window.MouseMove(moved, RawInputModifiers.MiddleMouseButton);
            window.MouseUp(moved, MouseButton.Middle);

            Assert.True(viewModel.ViewStartHz < initialStart);
            Assert.Equal(visibleSpan, viewModel.ViewStopHz - viewModel.ViewStartHz);

            viewModel.PanFrequencyRange(long.MaxValue);
            Assert.Equal(viewModel.Profile.MaximumFrequencyHz, viewModel.ViewStopHz);
            Assert.Equal(visibleSpan, viewModel.ViewStopHz - viewModel.ViewStartHz);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1024, 768, 1)]
    [InlineData(1440, 900, 1.25)]
    [InlineData(1920, 1080, 1.5)]
    public async Task MarkersCanBeDraggedAfterResizeAtDifferentDpi(int width, int height, double scaling)
    {
        await using var viewModel = TestData.ViewModel();
        viewModel.LoadSession(TestData.Session());
        Assert.Equal(5, viewModel.ScanProgressValue);
        Assert.Equal(5, viewModel.ScanProgressMaximum);
        Assert.Contains("5", viewModel.ScanProgressDisplay, StringComparison.Ordinal);
        var window = new MainWindow(viewModel);
        try
        {
            window.Show();
            window.SetRenderScaling(scaling);
            window.Width = width;
            window.Height = height;
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var plot = window.FindControl<VnaPlotControl>("Plot")!;
            Assert.True(plot.Bounds.Width > 200);
            Assert.True(plot.Bounds.Height > 100);
            var initial = plot.TranslatePoint(new Point(68 + (plot.Bounds.Width - 130) * 0.25, 50), window)!.Value;
            var moved = plot.TranslatePoint(new Point(68 + (plot.Bounds.Width - 130) * 0.75, 50), window)!.Value;

            window.MouseDown(initial, MouseButton.Left);
            Assert.Equal(2_000_000, viewModel.Marker1Frequency);
            window.MouseMove(moved, RawInputModifiers.LeftMouseButton);
            window.MouseUp(moved, MouseButton.Left);
            Assert.Equal(4_000_000, viewModel.Marker1Frequency);

            window.MouseDown(initial, MouseButton.Right);
            window.MouseUp(initial, MouseButton.Right);
            Assert.Equal(2_000_000, viewModel.Marker2Frequency);
            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, string.Empty);
            Assert.Equal(3_000_000, viewModel.Marker2Frequency);
            window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, string.Empty);
            Assert.Equal(2_000_000, viewModel.Marker2Frequency);
            viewModel.MoveMarkerSample(isMarker2: false, direction: -1);
            Assert.Equal(3_000_000, viewModel.Marker1Frequency);
            viewModel.MoveMarkerSample(isMarker2: false, direction: 1);
            Assert.Equal(4_000_000, viewModel.Marker1Frequency);
            viewModel.MoveMarkerSample(isMarker2: false, direction: 1);
            Assert.Equal(5_000_000, viewModel.Marker1Frequency);
            viewModel.MoveMarkerSample(isMarker2: false, direction: 1);
            Assert.Equal(5_000_000, viewModel.Marker1Frequency);
            viewModel.ZoomToMarkersCommand.Execute(null);
            Assert.Equal(2_000_000, viewModel.ViewStartHz);
            Assert.Equal(5_000_000, viewModel.ViewStopHz);
            Assert.Equal(3_000_000, viewModel.BandwidthHz);
            viewModel.SaveViewRangeCommand.Execute(null);
            viewModel.ClearZoomCommand.Execute(null);
            viewModel.RecallViewRangeCommand.Execute(null);
            Assert.Equal(2_000_000, viewModel.ViewStartHz);
            Assert.Equal(5_000_000, viewModel.ViewStopHz);
            viewModel.ShowProfileRangeCommand.Execute(null);
            Assert.Equal(viewModel.Profile.MinimumFrequencyHz, viewModel.ViewStartHz);
            Assert.Equal(viewModel.Profile.MaximumFrequencyHz, viewModel.ViewStopHz);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ZoomHistoryRestoresNestedRangesAndFullScan()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        vm.PickMarker(5_000_000, false);
        vm.PickMarker(2_000_000, true);
        vm.ZoomToMarkersCommand.Execute(null);
        Assert.Equal(2_000_000, vm.ViewStartHz);
        Assert.Equal(5_000_000, vm.ViewStopHz);
        vm.PickMarker(3_000_000, false);
        vm.PickMarker(4_000_000, true);
        vm.ZoomToMarkersCommand.Execute(null);
        vm.RestoreZoomCommand.Execute(null);
        Assert.Equal(2_000_000, vm.ViewStartHz);
        Assert.Equal(5_000_000, vm.ViewStopHz);
        vm.RestoreZoomCommand.Execute(null);
        Assert.Equal(0, vm.ViewStartHz);
        Assert.Equal(0, vm.ViewStopHz);
        Assert.False(vm.RestoreZoomCommand.CanExecute(null));
        vm.ZoomToMarkersCommand.Execute(null);
        vm.ClearZoomCommand.Execute(null);
        Assert.False(vm.CanRestoreZoom);
    }

    [AvaloniaFact]
    public async Task LoadedSessionRestoresSamePaletteForChartAndLegend()
    {
        await using var vm = TestData.ViewModel();
        var profile = DeviceProfile.Vna65MHz with
        {
            PlotColors = LegacyPlotColorSettings.Default with { Swr = 0x00FF00, Background = 0x010203 },
        };
        vm.LoadSession(TestData.Session(profile));
        Assert.Equal("#00FF00", vm.PlotPalette.GetColor(PlotTrace.Swr));
        Assert.Equal("#030201", vm.PlotPalette.Background);
        var legendBrush = Assert.IsType<SolidColorBrush>(vm.Traces.Single(t => t.Trace == PlotTrace.Swr).Brush);
        Assert.Equal(Color.Parse("#00FF00"), legendBrush.Color);
        Assert.Equal(profile, vm.CreateSession()!.ProfileSnapshot);
    }

    [AvaloniaFact]
    public async Task IncompatibleSessionDoesNotReplaceDisplayedData()
    {
        await using var vm = TestData.ViewModel();
        var original = TestData.Session();
        vm.LoadSession(original);
        var incompatible = TestData.Session(DeviceProfile.Vna65MHz with { Id = "another-device" });
        Assert.Throws<InvalidDataException>(() => vm.LoadSession(incompatible));
        Assert.Equal(original.Points.ToArray(), vm.PlotPoints.ToArray());
    }

    [AvaloniaFact]
    public async Task OfflineSessionPlaybackLoadsStoredSamplesWithoutOpeningSerialPort()
    {
        await using var vm = TestData.ViewModel();
        var stored = TestData.Session() with { DataSource = "Recorded fixture" };

        vm.LoadSession(stored);

        Assert.True(vm.IsSessionLoaded);
        Assert.Equal(stored.Points.ToArray(), vm.PlotPoints.ToArray());
        Assert.Equal("Recorded fixture", vm.DataSource);
        Assert.Equal(stored.Sweep, vm.CreateSession()!.Sweep);
    }

    [AvaloniaFact]
    public async Task InvalidReadoutsDoNotDisplayNanOrFalseMinimum()
    {
        await using var vm = TestData.ViewModel();
        var session = TestData.Session();
        vm.LoadSession(session with
        {
            Points = session.Points.Select(p => p with { Swr = double.NaN, ReactanceOhms = null }).ToArray(),
        });
        vm.PickMarker(1_000_000, false);
        Assert.Null(vm.MinimumSwrFrequency);
        Assert.Equal("—", vm.MinimumSwrText);
        Assert.DoesNotContain("NaN", vm.Marker1Readout);
        Assert.Contains("SWR —", vm.Marker1Readout);
        Assert.Contains("|X| —", vm.Marker1Readout);
    }

    [AvaloniaFact]
    public async Task MarkerTableShowsAllValuesFromTheSelectedSampleAndDashesForMissingData()
    {
        await using var vm = TestData.ViewModel();
        var session = TestData.Session();
        vm.LoadSession(session);
        vm.PickMarker(3_000_000, false);

        var values = vm.Marker1Values;
        Assert.Contains("3", values.Frequency);
        Assert.Equal("4,00", values.Swr);
        Assert.Equal("50,0", values.Impedance);
        Assert.Equal("-10,0", values.ReturnLoss);
        Assert.Equal("90,0", values.Phase);
        Assert.Equal("50,0", values.Resistance);
        Assert.Equal("0,0", values.Reactance);
        Assert.Equal(MarkerValues.Empty, vm.Marker2Values);

        var points = session.Points.ToArray();
        points[2] = points[2] with
        {
            Quality = SampleQuality.Missing,
            Swr = double.NaN,
            ReturnLossDb = null,
            ImpedanceOhms = null,
            PhaseDegrees = null,
            ResistanceOhms = null,
            ReactanceOhms = null,
        };
        vm.LoadSession(session with { Points = points });
        vm.PickMarker(3_000_000, false);
        Assert.Equal("—", vm.Marker1Values.Swr);
        Assert.Equal("—", vm.Marker1Values.Impedance);
        Assert.Equal("—", vm.Marker1Values.ReturnLoss);
        Assert.Equal("—", vm.Marker1Values.Phase);
        Assert.Equal("—", vm.Marker1Values.Resistance);
        Assert.Equal("—", vm.Marker1Values.Reactance);
    }

    [AvaloniaFact]
    public async Task MarkerVisibilityAndTraceCommandsPreserveMeasurementState()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        vm.PickMarker(2_000_000, false);
        vm.PickMarker(4_000_000, true);
        var originalPoints = vm.PlotPoints;

        vm.IsMarker1Visible = false;
        Assert.Equal(2_000_000, vm.Marker1Frequency);
        Assert.Equal(4_000_000, vm.Marker2Frequency);
        Assert.Equal(originalPoints, vm.PlotPoints);

        vm.HideAllTracesCommand.Execute(null);
        Assert.Equal(PlotTrace.None, vm.VisibleTraces);
        vm.ShowAllTracesCommand.Execute(null);
        Assert.Equal(PlotTrace.All, vm.VisibleTraces);
        Assert.Equal(2_000_000, vm.Marker1Frequency);
        Assert.Equal(4_000_000, vm.Marker2Frequency);
    }

    [AvaloniaFact]
    public async Task MinimumSwrVisibilityDoesNotChangeResultAndCommandMovesM1ToVisibleMinimum()
    {
        await using var vm = TestData.ViewModel();
        Assert.False(vm.SetMarker1ToMinimumSwrCommand.CanExecute(null));
        vm.LoadSession(TestData.Session());
        Assert.True(vm.SetMarker1ToMinimumSwrCommand.CanExecute(null));
        vm.PickMarker(2_000_000, false);
        vm.PickMarker(4_000_000, true);
        vm.ZoomToMarkersCommand.Execute(null);

        vm.IsMinimumSwrVisible = false;
        Assert.Equal(1_000_000, vm.MinimumSwrFrequency);
        vm.SetMarker1ToMinimumSwrCommand.Execute(null);
        Assert.Equal(1_000_000, vm.Marker1Frequency);
        Assert.Equal(0, vm.ViewStartHz);
        Assert.Equal(0, vm.ViewStopHz);
        vm.RestoreZoomCommand.Execute(null);
        Assert.Equal(2_000_000, vm.ViewStartHz);
        Assert.Equal(4_000_000, vm.ViewStopHz);

        var noValidMinimum = TestData.Session() with
        {
            Points = TestData.Session().Points.Select(point => point with { Swr = null }).ToArray(),
        };
        vm.LoadSession(noValidMinimum);
        Assert.False(vm.SetMarker1ToMinimumSwrCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task UnsupportedAnalysisModeHidesAntennaPlotInsteadOfReusingItsData()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        var points = vm.PlotPoints;
        var window = new MainWindow(vm) { Width = 1024, Height = 768 };
        try
        {
            window.Show();
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            var plot = window.FindControl<VnaPlotControl>("Plot")!;
            var table = window.FindControl<Border>("MarkerValuesTable")!;
            var filter = window.FindControl<Button>("FilterModeButton")!;

            filter.Command!.Execute(null);
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal(AnalysisMode.Filter, vm.SelectedMode);
            Assert.True(vm.CanSaveSession);
            Assert.False(plot.IsEffectivelyVisible);
            Assert.False(table.IsEffectivelyVisible);
            Assert.True(vm.IsFilterAnalysisMode);
            Assert.Same(points, vm.PlotPoints);

            window.FindControl<Button>("AntennaModeButton")!.Command!.Execute(null);
            await Dispatcher.UIThread.InvokeAsync(window.UpdateLayout, DispatcherPriority.Background);
            Assert.Equal(AnalysisMode.Antenna, vm.SelectedMode);
            Assert.True(vm.CanSaveSession);
            Assert.True(plot.IsEffectivelyVisible);
            Assert.Same(points, vm.PlotPoints);
        }
        finally { window.Close(); }
    }

    [Fact]
    public async Task CableLossModeCanSaveAndReloadReflectionSession()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        vm.SelectCableLossModeCommand.Execute(null);

        var session = vm.CreateSession();
        Assert.NotNull(session);
        Assert.Equal(SessionAnalysisMode.CableLoss, session.AnalysisMode);
        using var stream = new MemoryStream();
        await MeasurementSessionFile.SaveAsync(stream, session, TestContext.Current.CancellationToken);
        stream.Position = 0;
        var loaded = await MeasurementSessionFile.LoadAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal(SessionAnalysisMode.CableLoss, loaded.AnalysisMode);
        Assert.Equal(session.Points.ToArray(), loaded.Points.ToArray());
    }

    [Fact]
    public async Task CableLossFixtureConfirmationResetsWhenModeChangesOrSessionLoads()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session() with { DataSource = "hardware", AnalysisMode = SessionAnalysisMode.CableLoss });
        Assert.True(vm.CanConfirmCableLossFixture);
        vm.IsCableLossFixtureReady = true;
        Assert.True(vm.IsCableLossPlotAvailable);

        vm.SelectAntennaModeCommand.Execute(null);
        vm.SelectCableLossModeCommand.Execute(null);
        Assert.False(vm.IsCableLossFixtureReady);
        Assert.False(vm.IsCableLossPlotAvailable);

        vm.IsCableLossFixtureReady = true;
        var cableSession = TestData.Session() with { DataSource = "hardware", AnalysisMode = SessionAnalysisMode.CableLoss };
        vm.LoadSession(cableSession);
        Assert.False(vm.IsCableLossFixtureReady);
        Assert.False(vm.IsCableLossPlotAvailable);
    }

    [AvaloniaFact]
    public async Task CableLossDoesNotTreatDemoOrUnidentifiedSessionSamplesAsCableMeasurements()
    {
        await using var vm = TestData.ViewModel();
        vm.LoadSession(TestData.Session());
        vm.SelectCableLossModeCommand.Execute(null);
        vm.IsCableLossFixtureReady = true;

        Assert.False(vm.CanConfirmCableLossFixture);
        Assert.False(vm.IsCableLossPlotAvailable);
        Assert.Contains("demonstracyjne", vm.CableLossFixtureRequiredText, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InterruptedSecondSweepDevice : IVnaDevice
    {
        private readonly bool _failSecondSweep;
        private int _sweepCount;
        private bool _connected;

        public InterruptedSecondSweepDevice(bool failSecondSweep) => _failSecondSweep = failSecondSweep;

        public DeviceProfile Profile => DeviceProfile.Vna65MHz;
        public string DataSourceName => "controlled-test-device";
        public bool IsConnected => _connected;

        public ValueTask ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _connected = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _connected = false;
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<MeasurementPoint> SweepAsync(
            SweepSettings settings,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_connected) throw new InvalidOperationException("Device is disconnected.");
            var attempt = Interlocked.Increment(ref _sweepCount);
            if (attempt == 1)
            {
                for (var index = 0; index < settings.PointCount; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return TestData.Point(settings.FrequencyAt(index), 2 + index);
                }
                yield break;
            }

            yield return TestData.Point(settings.FrequencyAt(0), 9);
            if (_failSecondSweep) throw new IOException("Synthetic acquisition failure.");
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        public ValueTask DisposeAsync() => DisconnectAsync();
    }
}









