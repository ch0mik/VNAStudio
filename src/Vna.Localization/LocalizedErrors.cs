namespace Vna.Localization;

/// <summary>Preserves resource arguments while keeping the standard exception types used by callers.</summary>
public static class LocalizedErrors
{
    private const string MessageKey = "Vna.LocalizedMessage";

    private static T Attach<T>(T exception, string key, object?[] arguments) where T : Exception
    {
        exception.Data[MessageKey] = LocalizedMessage.Resource(key, arguments);
        return exception;
    }

    public static LocalizedMessage Message(Exception exception) =>
        exception.Data[MessageKey] as LocalizedMessage ?? LocalizedMessage.Literal(exception.Message);

    public static InvalidDataException InvalidData(string key, params object?[] arguments) =>
        Attach(new InvalidDataException(Text.Format(key, arguments)), key, arguments);

    public static InvalidOperationException InvalidOperation(string key, params object?[] arguments) =>
        Attach(new InvalidOperationException(Text.Format(key, arguments)), key, arguments);

    public static UnauthorizedAccessException AccessDenied(Exception innerException, string key, params object?[] arguments) =>
        Attach(new UnauthorizedAccessException(Text.Format(key, arguments), innerException), key, arguments);

    public static ArgumentException Argument(string? parameter, string key, params object?[] arguments) =>
        Attach(new ArgumentException(Text.Format(key, arguments), parameter), key, arguments);

    public static ArgumentOutOfRangeException ArgumentOutOfRange(string parameter, string key, params object?[] arguments) =>
        Attach(new ArgumentOutOfRangeException(parameter, Text.Format(key, arguments)), key, arguments);

    public static TimeoutException Timeout(string key, params object?[] arguments) =>
        Attach(new TimeoutException(Text.Format(key, arguments)), key, arguments);

    public static EndOfStreamException EndOfStream(string key, params object?[] arguments) =>
        Attach(new EndOfStreamException(Text.Format(key, arguments)), key, arguments);
}
