using Vna.Localization;
using Vna.Core;

namespace Vna.Devices;

/// <summary>Synthetic display data only. This is not a model of the hardware protocol or an instrument reading.</summary>
public sealed class DemoVnaDevice : IVnaDevice
{
    private readonly DeviceProfile _profile;
    private readonly DeviceProfileDescriptor? _descriptor;
    private readonly double? _referenceImpedanceOhms;
    private bool _isConnected;

    public DemoVnaDevice(DeviceProfile profile, DeviceProfileDescriptor? descriptor = null)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _descriptor = descriptor;
        if (descriptor is not null && !string.Equals(profile.Id, descriptor.Id, StringComparison.Ordinal))
            throw new ArgumentException("The demo profile descriptor does not match the compatibility profile.", nameof(descriptor));
        _referenceImpedanceOhms = descriptor is null ? profile.ReferenceImpedanceOhms : descriptor.ReferenceImpedanceOhms;
    }

    public DeviceProfile Profile => _profile;
    public string DataSourceName => "demo";

    public ValueTask ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        _isConnected = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        CancellationToken cancellationToken = default) =>
        SweepAsync(settings, DeviceSweepMode.Reflection, cancellationToken);

    public async IAsyncEnumerable<MeasurementPoint> SweepAsync(
        SweepSettings settings,
        DeviceSweepMode mode,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            throw LocalizedErrors.InvalidOperation("DeviceConnectFirst");
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));

        if (_descriptor is { } descriptor)
            settings.ValidateAgainst(descriptor);
        else
            settings.ValidateAgainst(Profile);

        for (var index = 0; index < settings.PointCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return mode == DeviceSweepMode.Reflection
                ? CreateIllustrativePoint(settings.FrequencyAt(index), _referenceImpedanceOhms)
                : CreateIllustrativeTransmissionPoint(settings.FrequencyAt(index));

            if ((index & 31) == 31)
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask DisposeAsync() => DisconnectAsync();

    private static MeasurementPoint CreateIllustrativePoint(long frequencyHz, double? referenceImpedanceOhms)
    {
        var frequencyMHz = frequencyHz / 1_000_000.0;

        // Invented resonances keep the offline UI useful; none of these values came from the user's VNA.
        var dip14MHz = Math.Exp(-Math.Pow((frequencyMHz - 14.2) / 1.15, 2));
        var dip27MHz = Math.Exp(-Math.Pow((frequencyMHz - 27.1) / 1.9, 2));
        var reflectionMagnitude = Math.Clamp(0.74 - 0.53 * Math.Max(dip14MHz, dip27MHz), 0.03, 0.92);
        var phase = 90 + 45 * Math.Tanh((frequencyMHz - 14.2) / 0.7)
                       - 35 * Math.Tanh((frequencyMHz - 27.1) / 1.1);
        var signedReturnLossDb = 20 * Math.Log10(reflectionMagnitude);

        var swr = reflectionMagnitude < 1 ? (1 + reflectionMagnitude) / (1 - reflectionMagnitude) : (double?)null;
        var point = referenceImpedanceOhms is { } reference
            ? RfSampleCalculator.FromReturnLossAndPhase(frequencyHz, signedReturnLossDb, phase, reference)
            : new MeasurementPoint(frequencyHz, swr, signedReturnLossDb, phase, null, null, null);
        return point with
        {
            PhaseSignConfidence = PhaseSignConfidence.Verified,
            PhaseSignProvenance = PhaseSignProvenance.AnalyticDemo,
        };
    }

    private static MeasurementPoint CreateIllustrativeTransmissionPoint(long frequencyHz)
    {
        var frequencyMHz = frequencyHz / 1_000_000.0;
        var bandCenterMHz = 14.2;
        var normalizedDistance = Math.Abs(frequencyMHz - bandCenterMHz) / 0.8;
        var transmissionDb = -1.5 - 24 * Math.Min(1, normalizedDistance * normalizedDistance);
        return new MeasurementPoint(frequencyHz, null, null, null, null, null, null)
        {
            PhaseSignConfidence = PhaseSignConfidence.Missing,
            TransmissionDb = transmissionDb,
        };
    }
}
