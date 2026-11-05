using Avalonia.Media;
using Vna.App.Plotting;
using Vna.Localization;
using Avalonia.Collections;
using Vna.App.Infrastructure;

namespace Vna.App.ViewModels;

public sealed class TraceOption(PlotTrace trace, string displayNameKey, PlotPalette palette) : ObservableObject
{
    private static readonly IReadOnlyList<string> AvailableColors = new[]
    {
        "#FF0000", "#0000FF", "#00AA00", "#00FFFF", "#FF00FF", "#FFFF00", "#D78502", "#19FF00",
        "#800080", "#000000", "#FFFFFF", "#008080", "#FF8000", "#808080", "#800000", "#000080",
    };
    private bool _isVisible = true;
    private IBrush _brush = new SolidColorBrush(Color.Parse(palette.GetColor(trace)));
    private AvaloniaList<double> _dashPattern = new(palette.GetDashPattern(trace));
    private string _selectedColor = palette.GetColor(trace);
    private bool _isColorEditingEnabled = !palette.IsNightVision;

    public TraceOption(
        PlotTrace trace,
        string displayNameKey,
        PlotPalette palette,
        string? profileColor = null,
        string? selectedColor = null)
        : this(trace, displayNameKey, palette)
    {
        DefaultColor = profileColor ?? palette.GetColor(trace);
        SelectedColor = selectedColor ?? DefaultColor;
        ColorChoices = AvailableColors.Append(DefaultColor).Append(SelectedColor)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        ResetColorCommand = new(() => SelectedColor = DefaultColor);
    }

    public PlotTrace Trace { get; } = trace;
    public string DefaultColor { get; } = palette.GetColor(trace);
    public IReadOnlyList<string> ColorChoices { get; private set; } = AvailableColors;
    public RelayCommand ResetColorCommand { get; private set; } = new(() => { });
    public string SelectedColor { get => _selectedColor; set => SetProperty(ref _selectedColor, value); }
    public bool IsColorEditingEnabled { get => _isColorEditingEnabled; private set => SetProperty(ref _isColorEditingEnabled, value); }
    public string DisplayName => Text.Get(displayNameKey);
    public void RefreshLanguage() => OnPropertyChanged(nameof(DisplayName));
    public IBrush Brush { get => _brush; private set => SetProperty(ref _brush, value); }
    public AvaloniaList<double> DashPattern { get => _dashPattern; private set => SetProperty(ref _dashPattern, value); }
    public void ApplyPalette(PlotPalette updatedPalette)
    {
        Brush = new SolidColorBrush(Color.Parse(updatedPalette.GetColor(Trace)));
        DashPattern = new(updatedPalette.GetDashPattern(Trace));
        IsColorEditingEnabled = !updatedPalette.IsNightVision;
    }
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }
}
