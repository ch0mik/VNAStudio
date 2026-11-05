using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Vna.App.Plotting;
using Vna.App.ViewModels;
using Vna.IO;
using Vna.Localization;

namespace Vna.App;

public sealed partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => DataContext as MainWindowViewModel
        ?? throw LocalizedErrors.InvalidOperation("MissingViewModel");
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly SemaphoreSlim _settingsSaveGate = new(1, 1);
    private readonly bool _persistSettingsOnClose;
    private bool _savingSettingsOnClose;
    private bool _allowClose;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        var plot = this.FindControl<VnaPlotControl>("Plot")
            ?? throw LocalizedErrors.InvalidOperation("MissingPlot");
        plot.MarkerPicked += (frequency, isMarker2) =>
        {
            if (DataContext is MainWindowViewModel viewModel) viewModel.PickMarker(frequency, isMarker2);
        };
        plot.MarkerStepRequested += (isMarker2, direction) =>
        {
            if (DataContext is MainWindowViewModel viewModel) viewModel.MoveMarkerSample(isMarker2, direction);
        };
        plot.ZoomRequested += zoomIn =>
        {
            if (DataContext is not MainWindowViewModel viewModel) return;
            var command = zoomIn ? viewModel.ZoomInCommand : viewModel.ZoomOutCommand;
            if (command.CanExecute(null)) command.Execute(null);
        };
        plot.PanRequested += deltaHz => (DataContext as MainWindowViewModel)?.PanFrequencyRange(deltaHz);
        var bothPlot = this.FindControl<VnaPlotControl>("PlotBoth");
        if (bothPlot is not null) WireFrequencyPlot(bothPlot);
        var smithPlot = this.FindControl<SmithPlotControl>("SmithPlot");
        if (smithPlot is not null) WireSmithPlot(smithPlot);
        var smithPlotBoth = this.FindControl<SmithPlotControl>("SmithPlotBoth");
        if (smithPlotBoth is not null) WireSmithPlot(smithPlotBoth);
        Closing += MainWindow_OnClosing;

        void WireFrequencyPlot(VnaPlotControl control)
        {
            control.MarkerPicked += (frequency, isMarker2) =>
            {
                if (DataContext is MainWindowViewModel viewModel) viewModel.PickMarker(frequency, isMarker2);
            };
            control.MarkerStepRequested += (isMarker2, direction) =>
            {
                if (DataContext is MainWindowViewModel viewModel) viewModel.MoveMarkerSample(isMarker2, direction);
            };
            control.ZoomRequested += zoomIn =>
            {
                if (DataContext is not MainWindowViewModel viewModel) return;
                var command = zoomIn ? viewModel.ZoomInCommand : viewModel.ZoomOutCommand;
                if (command.CanExecute(null)) command.Execute(null);
            };
            control.PanRequested += deltaHz => (DataContext as MainWindowViewModel)?.PanFrequencyRange(deltaHz);
        }

        void WireSmithPlot(SmithPlotControl control)
        {
            control.MarkerPicked += (frequency, isMarker2) =>
            {
                if (DataContext is MainWindowViewModel viewModel) viewModel.PickMarker(frequency, isMarker2);
            };
            control.MarkerStepRequested += (isMarker2, direction) =>
            {
                if (DataContext is MainWindowViewModel viewModel) viewModel.MoveMarkerSample(isMarker2, direction);
            };
            control.ZoomRequested += (factor, anchor, offsetX, offsetY) =>
                (DataContext as MainWindowViewModel)?.ChangeSmithZoom(factor, anchor, offsetX, offsetY);
            control.PanRequested += (deltaReal, deltaImaginary) =>
                (DataContext as MainWindowViewModel)?.PanSmith(deltaReal, deltaImaginary);
        }
    }

    public MainWindow(MainWindowViewModel viewModel, bool persistSettingsOnClose = false) : this()
    {
        DataContext = viewModel;
        _persistSettingsOnClose = persistSettingsOnClose;
    }

    private async void MainWindow_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_persistSettingsOnClose || _allowClose || DataContext is not MainWindowViewModel)
        {
            _lifetimeCancellation.Cancel();
            return;
        }

        e.Cancel = true;
        if (_savingSettingsOnClose) return;
        _savingSettingsOnClose = true;
        IsEnabled = false;
        try
        {
            await PersistConfigurationAsync(_lifetimeCancellation.Token);
            _allowClose = true;
            Dispatcher.UIThread.Post(Close);
        }
        catch (Exception exception)
        {
            ViewModel.ReportLocalizedError("SettingsSaveFailed", LocalizedErrors.Message(exception));
            // Keep the error visible; a subsequent Close can exit even if the path is unwritable.
            _allowClose = true;
            IsEnabled = true;
        }
        finally
        {
            _savingSettingsOnClose = false;
        }
    }

    private async Task PersistConfigurationAsync(CancellationToken cancellationToken)
    {
        var settings = ViewModel.CreateAppSettings();
        await _settingsSaveGate.WaitAsync(cancellationToken);
        try
        {
            await AppSettingsFile.SaveAsync(settings, ViewModel.ConfigurationPath, cancellationToken);
        }
        finally
        {
            _settingsSaveGate.Release();
        }
    }

    private async void SaveSession_OnClick(object? sender, RoutedEventArgs e)
    {
        var session = ViewModel.CreateSession();
        if (session is null)
        {
            ViewModel.ReportLocalizedError("SaveRequiresComplete");
            return;
        }

        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Text.Get("SaveSessionTitle"),
                SuggestedFileName = $"vna-{DateTime.Now:yyyyMMdd-HHmmss}.vnasession.json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(Text.Get("SessionFileType")) { Patterns = new[] { "*.vnasession.json", "*.json" } },
                },
            });
            if (file is null) return;
            _lifetimeCancellation.Token.ThrowIfCancellationRequested();
            await using var stream = await file.OpenWriteAsync();
            await MeasurementSessionFile.SaveAsync(stream, session, _lifetimeCancellation.Token);
            ViewModel.MarkSessionSaved(file.Name);
            ViewModel.ReportLocalizedStatus("SessionSaved", file.Name);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) return;
            ViewModel.ReportException(exception);
        }
    }

    private async void OpenSession_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Text.Get("OpenSessionTitle"),
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(Text.Get("SessionFileType")) { Patterns = new[] { "*.vnasession.json", "*.json" } },
                },
            });
            if (files.Count == 0) return;
            _lifetimeCancellation.Token.ThrowIfCancellationRequested();
            await using var stream = await files[0].OpenReadAsync();
            var session = await MeasurementSessionFile.LoadAsync(stream, _lifetimeCancellation.Token);
            _lifetimeCancellation.Token.ThrowIfCancellationRequested();
            ViewModel.LoadSession(session);
            ViewModel.MarkSessionSaved(files[0].Name);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) return;
            ViewModel.ReportLocalizedError("SessionOpenFailed", LocalizedErrors.Message(exception));
        }
    }

    private async void About_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = Text.Get("AboutTitle"),
            Width = 460,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Background,
            Foreground = Foreground,
            RequestedThemeVariant = ViewModel.ThemeVariant,
        };
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock { Text = Text.Get("AboutProduct"), FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold });
        content.Children.Add(new TextBlock { Text = Text.Format("AboutVersionFormat", typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "unknown"), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = Text.Get("AboutProtocolNote"), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        var close = new Button { Content = Text.Get("Close"), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Classes = { "Primary" } };
        close.Click += (_, _) => dialog.Close();
        content.Children.Add(close);
        dialog.Content = new Border { Padding = new Avalonia.Thickness(24), Child = content };
        await dialog.ShowDialog(this);
    }

    private async void OpenCsv_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Text.Get("OpenCsvTitle"),
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(Text.Get("CsvFileType")) { Patterns = new[] { "*.csv" } },
                },
            });
            if (files.Count == 0) return;
            _lifetimeCancellation.Token.ThrowIfCancellationRequested();
            await using var stream = await files[0].OpenReadAsync();
            var session = await MeasurementCsvFile.LoadAsync(stream, ViewModel.Profile, _lifetimeCancellation.Token);
            _lifetimeCancellation.Token.ThrowIfCancellationRequested();
            ViewModel.LoadSession(session);
            ViewModel.MarkSessionSaved(files[0].Name);
            ViewModel.ReportLocalizedStatus("CsvLoaded", files[0].Name, session.Points.Count);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) return;
            ViewModel.ReportLocalizedError("CsvOpenFailed", LocalizedErrors.Message(exception));
        }
    }

    private async void ExportCsv_OnClick(object? sender, RoutedEventArgs e)
    {
        var session = ViewModel.CreateSession();
        if (session is null)
        {
            ViewModel.ReportLocalizedError("SaveRequiresComplete");
            return;
        }

        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Text.Get("ExportCsvTitle"),
                SuggestedFileName = $"vna-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
                DefaultExtension = "csv",
                FileTypeChoices = new[] { new FilePickerFileType(Text.Get("CsvFileType")) { Patterns = new[] { "*.csv" } } },
            });
            if (file is null) return;
            _lifetimeCancellation.Token.ThrowIfCancellationRequested();
            await using var stream = await file.OpenWriteAsync();
            await MeasurementCsvFile.SaveAsync(stream, session, _lifetimeCancellation.Token);
            ViewModel.ReportLocalizedStatus("CsvSaved", file.Name);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) return;
            ViewModel.ReportException(exception);
        }
    }
    private async void SaveConfiguration_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await PersistConfigurationAsync(_lifetimeCancellation.Token);
            ViewModel.ReportLocalizedStatus("SettingsSaved");
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) return;
            ViewModel.ReportLocalizedError("SettingsSaveFailed", LocalizedErrors.Message(exception));
        }
    }

    private async void RefreshPorts_OnClick(object? sender, RoutedEventArgs e) =>
        await ViewModel.RefreshPortNamesAsync(_lifetimeCancellation.Token);
}



