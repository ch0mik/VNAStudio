namespace Vna.Localization;

/// <summary>Keeps message arguments so an existing status can be translated again after a language change.</summary>
public sealed class LocalizedMessage
{
    private readonly string? _key;
    private readonly object?[] _arguments;
    private readonly string _literal;

    private LocalizedMessage(string? key, object?[] arguments, string literal) =>
        (_key, _arguments, _literal) = (key, arguments, literal);

    public static LocalizedMessage Resource(string key, params object?[] arguments) => new(key, arguments, string.Empty);
    public static LocalizedMessage Literal(string value) => new(null, Array.Empty<object?>(), value);
    public override string ToString() => _key is null ? _literal : Text.Format(_key, _arguments);
}
