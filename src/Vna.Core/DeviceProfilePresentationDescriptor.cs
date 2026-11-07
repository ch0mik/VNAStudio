namespace Vna.Core;

/// <summary>
/// Optional, model-neutral hints for presenting a device profile in the host application.
/// Color keys are stable trace names (for example, "Swr" or "ReturnLoss") and values are RGB hex.
/// </summary>
public sealed record DeviceProfilePresentationDescriptor(
    IReadOnlyList<AmateurBandPreset> FrequencyBands,
    IReadOnlyDictionary<string, string> TraceColors,
    double? DefaultCableVelocityFactor)
{
    public static DeviceProfilePresentationDescriptor Empty { get; } = new(
        Array.Empty<AmateurBandPreset>(),
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)),
        null);
}
