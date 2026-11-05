using Avalonia.Styling;
using Vna.App.ViewModels;
using Vna.Localization;

namespace Vna.App.Theming;

public enum AppThemeKind { Light, Dark, NightVision }

public sealed class ThemeOption(string code, string nameKey, AppThemeKind kind, ThemeVariant variant) : ObservableObject
{
    public string Code { get; } = code;
    public AppThemeKind Kind { get; } = kind;
    public ThemeVariant Variant { get; } = variant;
    public string DisplayName => Text.Get(nameKey);
    public override string ToString() => DisplayName;
    public void RefreshLanguage() => OnPropertyChanged(nameof(DisplayName));
}

public static class AppThemes
{
    public static ThemeVariant NightVision { get; } = new("NightVision", ThemeVariant.Dark);

    public static IReadOnlyList<ThemeOption> CreateOptions() => Array.AsReadOnly(new[]
    {
        new ThemeOption("light", "ThemeLight", AppThemeKind.Light, ThemeVariant.Light),
        new ThemeOption("dark", "ThemeDark", AppThemeKind.Dark, ThemeVariant.Dark),
        new ThemeOption("night-vision", "ThemeNightVision", AppThemeKind.NightVision, NightVision),
    });

    public static string Normalize(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "light" => "light",
        "night-vision" => "night-vision",
        _ => "dark",
    };
}
