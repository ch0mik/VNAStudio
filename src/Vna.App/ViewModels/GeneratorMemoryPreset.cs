using System.Globalization;

namespace Vna.App.ViewModels;

public sealed record GeneratorMemoryPreset(int Index, long FrequencyHz)
{
    public string DisplayText => $"M{Index} · {(FrequencyHz / 1_000_000.0).ToString("0.###", CultureInfo.InvariantCulture)} MHz";
}
