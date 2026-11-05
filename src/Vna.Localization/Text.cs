namespace Vna.Localization;

/// <summary>Resource access for validation and transport errors outside the UI layer.</summary>
public static class Text
{
    public static string Get(string key) => LocalizationService.Instance.Get(key);
    public static string Format(string key, params object?[] arguments) => LocalizationService.Instance.Format(key, arguments);
}
