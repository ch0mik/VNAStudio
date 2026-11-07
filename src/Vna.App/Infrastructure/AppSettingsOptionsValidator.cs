using Microsoft.Extensions.Options;
using Vna.IO;

namespace Vna.App.Infrastructure;

public sealed class AppSettingsOptionsValidator : IValidateOptions<AppSettings>
{
    public ValidateOptionsResult Validate(string? name, AppSettings settings) =>
        AppSettingsValidation.TryValidate(settings, out var error)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(error);
}
