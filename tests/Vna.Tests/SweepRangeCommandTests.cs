using Avalonia.Headless.XUnit;
using Vna.Core;
using Xunit;

namespace Vna.Tests;

public sealed class SweepRangeCommandTests
{
    [AvaloniaFact]
    public async Task FullRangeResetsSweepLimitsAndMarkerRangeUsesAscendingFrequencies()
    {
        await using var viewModel = TestData.ViewModel();
        viewModel.StartMHz = "10";
        viewModel.StopMHz = "12";

        viewModel.SetFullSweepRangeCommand.Execute(null);

        Assert.Equal(1, Parse(viewModel.StartMHz, viewModel.Localizer.Culture));
        Assert.Equal(65, Parse(viewModel.StopMHz, viewModel.Localizer.Culture));

        var session = TestData.Session();
        viewModel.LoadSession(session);
        viewModel.PickMarker(5_000_000, isMarker2: true);
        viewModel.PickMarker(2_000_000, isMarker2: false);
        Assert.True(viewModel.CanSetSweepRangeToMarkers);

        viewModel.SetSweepRangeToMarkersCommand.Execute(null);

        Assert.Equal(2, Parse(viewModel.StartMHz, viewModel.Localizer.Culture));
        Assert.Equal(5, Parse(viewModel.StopMHz, viewModel.Localizer.Culture));
    }

    private static double Parse(string value, System.Globalization.CultureInfo culture) =>
        double.Parse(value, culture);
}
