using Vna.Core;
using Vna.Localization;
using System.Globalization;

namespace Vna.App.ViewModels;

public sealed class ComparisonSnapshotOption(NamedMeasurementSnapshot snapshot, string color) : ObservableObject
{
    private bool _isVisible = true;
    private string _color = color;

    public NamedMeasurementSnapshot Snapshot { get; } = snapshot;
    public string Name => Snapshot.Name;
    public string ProvenanceHint
    {
        get
        {
            var session = Snapshot.Session;
            var referenceImpedance = session.ProfileDescriptorSnapshot is { } descriptor
                ? descriptor.ReferenceImpedanceOhms
                : session.ProfileSnapshot?.ReferenceImpedanceOhms;
            var referenceText = referenceImpedance is { } zref
                ? $"{zref.ToString("0.##", CultureInfo.InvariantCulture)} Ω"
                : Text.Get("SmithHoverReferenceUnknown");
            return Text.Format("ComparisonSnapshotProvenanceFormat", session.DataSource,
                session.PluginId ?? Text.Get("UnknownSource"), session.ProfileId, referenceText,
                session.CapturedAtUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
        }
    }
    public string Color
    {
        get => _color;
        private set => SetProperty(ref _color, value);
    }
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    internal void UpdateColor(string color) => Color = color;
    internal void RefreshLocalization() => OnPropertyChanged(nameof(ProvenanceHint));
}
