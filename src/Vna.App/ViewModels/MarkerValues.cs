namespace Vna.App.ViewModels;

public sealed record MarkerValues(
    string Frequency,
    string Swr,
    string Impedance,
    string ReturnLoss,
    string Phase,
    string Resistance,
    string Reactance)
{
    public string ReactanceComponent { get; init; } = "—";
    public static MarkerValues Empty { get; } = new("—", "—", "—", "—", "—", "—", "—");
}
