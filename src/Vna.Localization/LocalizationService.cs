using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Vna.Localization;

public sealed record LanguageOption(string Code, string NativeName)
{
    public override string ToString() => NativeName;
}

/// <summary>Shared RESX resources with notifications for live Avalonia bindings.</summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources = new("Vna.Localization.Resources.Strings", typeof(LocalizationService).Assembly);
    private CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

    private LocalizationService() { }

    public static LocalizationService Instance { get; } = new();
    public event PropertyChangedEventHandler? PropertyChanged;
    public CultureInfo Culture => _culture;
    public string LanguageCode => _culture.Name;
    public string this[string key] => Get(key);
    public IReadOnlyList<LanguageOption> Languages { get; } = Array.AsReadOnly(new[]
    {
        new LanguageOption("pl-PL", "Polski"),
        new LanguageOption("en-US", "English"),
    });

    public string Get(string key) => Resources.GetString(key, _culture)
        ?? throw new MissingManifestResourceException($"Missing localization resource: {key}");

    public string Format(string key, params object?[] arguments) => string.Format(_culture, Get(key), arguments);

    /// <summary>Changes the active language. Call on the UI thread when the service is bound to controls.</summary>
    public void ChangeLanguage(string? language)
    {
        var code = NormalizeLanguage(language);
        if (_culture.Name == code) return;
        _culture = CultureInfo.GetCultureInfo(code);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageCode)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return "en-US";
        try
        {
            return CultureInfo.GetCultureInfo(language).TwoLetterISOLanguageName == "pl" ? "pl-PL" : "en-US";
        }
        catch (CultureNotFoundException)
        {
            return "en-US";
        }
    }
}
