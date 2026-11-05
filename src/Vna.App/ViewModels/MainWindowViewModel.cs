using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Threading;
using Avalonia.Media;
using Microsoft.Extensions.Options;
using Vna.App.Infrastructure;
using Vna.App.Plotting;
using Vna.Core;
using Vna.Devices;
using Vna.Devices.Transport;
using Vna.IO;
using Vna.Localization;
using System.ComponentModel;
using Avalonia.Styling;
using Vna.App.Theming;

namespace Vna.App.ViewModels;

public enum AntennaChartView { Frequency, Smith, Both }

public sealed class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IVnaDevice _device;
    private readonly IDeviceSourceSelector? _deviceSourceSelector;
    private readonly ISerialPortCatalog _portCatalog;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly Stack<(long Start, long Stop)> _zoomHistory = new();
    private readonly IDisposable? _settingsSubscription;
    private CultureInfo _inputCulture;
    private ThemeOption _selectedTheme;
    private TaskCompletionSource? _scanStopped;
    private TaskCompletionSource? _portDiscoveryStopped;
    private string _portName;
    private string? _selectedDiscoveredPort;
    private string _dataSource;
    private DeviceProfile? _profileSnapshot;
    private readonly Dictionary<PlotTrace, string> _traceColorOverrides;
    private bool _updatingTraceColors;
    private PlotPalette _plotPalette = Vna.App.Plotting.PlotPalette.LegacyDefault;
    private bool _isSessionLoaded;
    private DateTimeOffset _capturedAtUtc = DateTimeOffset.UtcNow;
    private string? _currentSessionFileName;
    private bool _hasUnsavedResult;
    private SweepSettings? _lastSweep;
    private CancellationTokenSource? _scanCancellation;
    private string _startMHz = "1";
    private string _stopMHz = "65";
    private string _pointCount = "649";
    private bool _isScanning;
    private bool _isHold = true;
    private bool _isMarker1Visible = true;
    private bool _isMarker2Visible = true;
    private bool _isMinimumSwrVisible = true;
    private bool _show50OhmReference;
    private bool _showXcXl;
    private bool _returnLossX2;
    private bool _showFullMarkerValues = true;
    private AntennaChartView _selectedChartView = AntennaChartView.Frequency;
    private double _smithZoom = 1;
    private double _smithPanReal;
    private double _smithPanImaginary;
    private string _returnLossDisplayOffsetText = "0";
    private double _returnLossDisplayOffsetDb;
    private bool _isReturnLossDisplayOffsetValid = true;
    private string _cableVelocityFactorText;
    private string _resonanceFrequencyMHz = string.Empty;
    private string _lowerEdgeMHz = string.Empty;
    private string _upperEdgeMHz = string.Empty;
    private string _generatorFrequencyMHz = string.Empty;
    private GeneratorMemoryPreset? _selectedGeneratorMemory;
    private AmateurBandGroup _selectedBandGroup = AmateurBandGroup.Hf;
    private AmateurBandPreset? _selectedBandPreset;
    private AnalysisMode _selectedMode = AnalysisMode.Antenna;
    private bool _isRefreshingPorts;
    private DeviceSourceMode _measurementSourceMode;
    private bool _isDeviceConnected;
    private bool _isConnectingDevice;
    private (long Start, long Stop)? _savedViewRange;
    private bool _isDisposed;
    private TimeSpan? _lastScanDuration;
    private int _scanProgressCount;
    private int _scanProgressMaximum;
    private LocalizedMessage _status = LocalizedMessage.Resource("StatusReady");
    private LocalizedMessage _error = LocalizedMessage.Literal(string.Empty);
    private long? _marker1;
    private long? _marker2;
    private long _viewStartHz;
    private long _viewStopHz;
    private IReadOnlyList<MeasurementPoint> _plotPoints = Array.Empty<MeasurementPoint>();
    private IReadOnlyList<MeasurementPoint> _analysisPlotPoints = Array.Empty<MeasurementPoint>();
    private bool _cableLossFixtureReady;
    private bool _cableLossHardwareDataAvailable;

    public MainWindowViewModel(
        IVnaDevice device,
        ISerialPortCatalog portCatalog,
        IOptionsMonitor<AppSettings> settings,
        string configurationPath,
        LocalizationService? localizer = null)
    {
        _device = device;
        _deviceSourceSelector = device as IDeviceSourceSelector;
        _portCatalog = portCatalog;
        ArgumentNullException.ThrowIfNull(settings);
        Localizer = localizer ?? LocalizationService.Instance;
        Localizer.ChangeLanguage(settings.CurrentValue.Language);
        _inputCulture = Localizer.Culture;
        _selectedTheme = Themes.First(theme => theme.Code == AppThemes.Normalize(settings.CurrentValue.Theme));
        _portName = settings.CurrentValue.PortName ?? string.Empty;
        _measurementSourceMode = ParseMeasurementSource(settings.CurrentValue.MeasurementSource);
        if (_deviceSourceSelector?.SelectSource(_measurementSourceMode) == false)
            _measurementSourceMode = DeviceSourceMode.Demo;
        Show50OhmReference = settings.CurrentValue.Show50OhmReference;
        ShowXcXl = settings.CurrentValue.ShowXcXl;
        ReturnLossX2 = settings.CurrentValue.ReturnLossX2;
        ShowFullMarkerValues = settings.CurrentValue.ShowFullMarkerValues;
        _selectedChartView = ParseChartView(settings.CurrentValue.AntennaChartView);
        _cableVelocityFactorText = GetCableVelocityFactor(settings.CurrentValue.CableVelocityFactor, device.Profile)
            .ToString("0.###", Localizer.Culture);
        _dataSource = device.DataSourceName;
        _profileSnapshot = device.Profile;
        ConfigurationPath = configurationPath;
        _settingsSubscription = settings.OnChange(updated =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_isDisposed) return;
                PortName = updated.PortName ?? string.Empty;
                var sourceOption = MeasurementSources.FirstOrDefault(option =>
                    option.Mode == ParseMeasurementSource(updated.MeasurementSource));
                if (sourceOption is not null) SelectedMeasurementSource = sourceOption;
                Show50OhmReference = updated.Show50OhmReference;
                ShowXcXl = updated.ShowXcXl;
                ReturnLossX2 = updated.ReturnLossX2;
                ShowFullMarkerValues = updated.ShowFullMarkerValues;
                SelectedChartView = ParseChartView(updated.AntennaChartView);
                Localizer.ChangeLanguage(updated.Language);
                ReturnLossDisplayOffsetText = updated.ReturnLossDisplayOffsetDb.ToString("0.###", Localizer.Culture);
                CableVelocityFactorText = GetCableVelocityFactor(updated.CableVelocityFactor, device.Profile)
                    .ToString("0.###", Localizer.Culture);
                SelectedTheme = Themes.First(theme => theme.Code == AppThemes.Normalize(updated.Theme));
                ApplyTraceColorOverrides(updated.TraceColors);
            }));
        Profile = device.Profile;
        ReturnLossDisplayOffsetText = settings.CurrentValue.ReturnLossDisplayOffsetDb.ToString("0.###", Localizer.Culture);
        GeneratorMemories = Array.AsReadOnly(Profile.GeneratorMemoryFrequenciesHz
            .Select((frequency, index) => new GeneratorMemoryPreset(index, frequency)).ToArray());
        _traceColorOverrides = ParseTraceColorOverrides(settings.CurrentValue.TraceColors);
        var profilePalette = Vna.App.Plotting.PlotPalette.FromProfile(Profile);
        PlotPalette = Vna.App.Plotting.PlotPalette.FromProfile(Profile, _selectedTheme.Kind, _traceColorOverrides);
        _startMHz = (Profile.MinimumFrequencyHz / 1_000_000.0).ToString("0.###", Localizer.Culture);
        _stopMHz = (Profile.MaximumFrequencyHz / 1_000_000.0).ToString("0.###", Localizer.Culture);
        _pointCount = Profile.DefaultPointCount.ToString(Localizer.Culture);
        Traces = new ObservableCollection<TraceOption>
        {
            CreateTrace(PlotTrace.Swr, "TraceSwr"),
            CreateTrace(PlotTrace.ReturnLoss, "TraceReturnLoss"),
            CreateTrace(PlotTrace.Phase, "TracePhase"),
            CreateTrace(PlotTrace.Impedance, "TraceImpedance"),
            CreateTrace(PlotTrace.Resistance, "TraceResistance"),
            CreateTrace(PlotTrace.Reactance, "TraceReactance"),
        };
        Points.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ScanProgressValue));
            OnPropertyChanged(nameof(ScanProgressDisplay));
            OnPropertyChanged(nameof(CanSaveSession));
            UpdateScaleZoomAvailability();
        };
        foreach (var trace in Traces) trace.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TraceOption.IsVisible)) OnPropertyChanged(nameof(VisibleTraces));
            if (args.PropertyName == nameof(TraceOption.SelectedColor) && !_updatingTraceColors)
                SetTraceColorOverride(trace.Trace, trace.SelectedColor);
        };

        TraceOption CreateTrace(PlotTrace trace, string displayNameKey)
        {
            var defaultColor = profilePalette.GetColor(trace);
            return new TraceOption(trace, displayNameKey, PlotPalette, defaultColor,
                _traceColorOverrides.GetValueOrDefault(trace, defaultColor));
        }
        SelectAntennaModeCommand = new RelayCommand(() => SelectedMode = AnalysisMode.Antenna);
        SelectCableLossModeCommand = new RelayCommand(() => SelectedMode = AnalysisMode.CableLoss);
        SelectCableLengthModeCommand = new RelayCommand(() => SelectedMode = AnalysisMode.CableLength);
        SelectXtalModeCommand = new RelayCommand(() => SelectedMode = AnalysisMode.Xtal);
        SelectFilterModeCommand = new RelayCommand(() => SelectedMode = AnalysisMode.Filter);
        SelectGeneratorModeCommand = new RelayCommand(() => SelectedMode = AnalysisMode.Generator);
        SelectFrequencyChartCommand = new RelayCommand(() => SelectedChartView = AntennaChartView.Frequency);
        SelectSmithChartCommand = new RelayCommand(() => SelectedChartView = AntennaChartView.Smith);
        SelectBothChartsCommand = new RelayCommand(() => SelectedChartView = AntennaChartView.Both);
        SmithZoomInCommand = new RelayCommand(() => ChangeSmithZoom(2));
        SmithZoomOutCommand = new RelayCommand(() => ChangeSmithZoom(0.5));
        ResetSmithViewCommand = new RelayCommand(ResetSmithView);
        SelectHfBandsCommand = new RelayCommand(() => SelectedBandGroup = AmateurBandGroup.Hf);
        SelectVhfBandsCommand = new RelayCommand(() => SelectedBandGroup = AmateurBandGroup.Vhf);
        ConnectDeviceCommand = new AsyncRelayCommand(ConnectDeviceAsync, () => CanConnectDevice);
        DisconnectDeviceCommand = new AsyncRelayCommand(DisconnectDeviceAsync, () => CanDisconnectDevice);
        StartCommand = new AsyncRelayCommand(StartScanAsync, () => !IsScanning && !_isConnectingDevice && !_isDisposed);
        HoldCommand = new AsyncRelayCommand(HandleHoldChangedAsync, () => !_isDisposed);
        StopCommand = new RelayCommand(StopScanning, () => IsScanning);
        ClearZoomCommand = new RelayCommand(ResetZoom);
        ShowProfileRangeCommand = new RelayCommand(ShowProfileRange);
        SetFullSweepRangeCommand = new RelayCommand(SetFullSweepRange, () => !IsScanning && !_isDisposed);
        SetSweepRangeToMarkersCommand = new RelayCommand(SetSweepRangeToMarkers, () => CanSetSweepRangeToMarkers);
        SaveViewRangeCommand = new RelayCommand(SaveViewRange);
        RecallViewRangeCommand = new RelayCommand(RecallViewRange, () => _savedViewRange is not null);
        RestoreZoomCommand = new RelayCommand(RestoreZoom, () => CanRestoreZoom);
        ZoomToMarkersCommand = new RelayCommand(ZoomToMarkers, () => CanZoomToMarkers);
        ZoomInCommand = new RelayCommand(() => ChangeZoom(zoomIn: true), () => CanZoomIn);
        ZoomOutCommand = new RelayCommand(() => ChangeZoom(zoomIn: false), () => CanZoomOut);
        ShowAllTracesCommand = new RelayCommand(() => SetTraceVisibility(true));
        HideAllTracesCommand = new RelayCommand(() => SetTraceVisibility(false));
        MoveMarker1PreviousCommand = new RelayCommand(() => MoveMarkerSample(false, -1));
        MoveMarker1NextCommand = new RelayCommand(() => MoveMarkerSample(false, 1));
        MoveMarker2PreviousCommand = new RelayCommand(() => MoveMarkerSample(true, -1));
        MoveMarker2NextCommand = new RelayCommand(() => MoveMarkerSample(true, 1));
        SetMarker1ToMinimumSwrCommand = new RelayCommand(SetMarker1ToMinimumSwr,
            () => !IsScanning && MinimumSwrFrequency is not null);
        Localizer.PropertyChanged += OnLanguageChanged;
    }

    public LocalizationService Localizer { get; }
    public IReadOnlyList<LanguageOption> Languages => Localizer.Languages;
    public LanguageOption? SelectedLanguage
    {
        get => Languages.First(language => language.Code == Localizer.LanguageCode);
        set { if (value is not null) Localizer.ChangeLanguage(value.Code); }
    }
    public IReadOnlyList<ThemeOption> Themes { get; } = AppThemes.CreateOptions();
    public ThemeOption SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (value is null || !Themes.Contains(value) || !SetProperty(ref _selectedTheme, value)) return;
            ApplyProfilePalette(_profileSnapshot ?? Profile);
            OnPropertyChanged(nameof(ThemeVariant));
            OnPropertyChanged(nameof(IsNightVision));
        }
    }
    public ThemeVariant ThemeVariant => SelectedTheme.Variant;
    public bool IsNightVision => SelectedTheme.Kind == AppThemeKind.NightVision;
    public AppSettings CreateAppSettings() => new()
    {
        PortName = PortName,
        MeasurementSource = _measurementSourceMode.ToString().ToLowerInvariant(),
        Show50OhmReference = Show50OhmReference,
        ShowXcXl = ShowXcXl,
        ReturnLossX2 = ReturnLossX2,
        ShowFullMarkerValues = ShowFullMarkerValues,
        ReturnLossDisplayOffsetDb = _returnLossDisplayOffsetDb,
        CableVelocityFactor = TryParseCableVelocityFactor(CableVelocityFactorText, Localizer.Culture),
        Language = Localizer.LanguageCode,
        Theme = SelectedTheme.Code,
        AntennaChartView = SelectedChartView.ToString().ToLowerInvariant(),
        TraceColors = _traceColorOverrides.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
    };
    public DeviceProfile Profile { get; }
    public PlotPalette PlotPalette { get => _plotPalette; private set => SetProperty(ref _plotPalette, value); }
    public AntennaChartView SelectedChartView
    {
        get => _selectedChartView;
        set
        {
            if (!Enum.IsDefined(value) || !SetProperty(ref _selectedChartView, value)) return;
            OnPropertyChanged(nameof(IsSingleChartView));
            OnPropertyChanged(nameof(IsFrequencyChartVisible));
            OnPropertyChanged(nameof(IsSmithChartVisible));
            OnPropertyChanged(nameof(IsBothChartViews));
            OnPropertyChanged(nameof(IsFrequencyChartSelected));
            OnPropertyChanged(nameof(IsSmithChartSelected));
        }
    }
    private static AntennaChartView ParseChartView(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "smith" => AntennaChartView.Smith,
        "both" => AntennaChartView.Both,
        _ => AntennaChartView.Frequency,
    };
    public bool IsSingleChartView => SelectedChartView != AntennaChartView.Both;
    public bool IsFrequencyChartVisible => SelectedChartView is AntennaChartView.Frequency or AntennaChartView.Both;
    public bool IsSmithChartVisible => SelectedChartView is AntennaChartView.Smith or AntennaChartView.Both;
    public bool IsBothChartViews => SelectedChartView == AntennaChartView.Both;
    public bool IsFrequencyChartSelected => SelectedChartView == AntennaChartView.Frequency;
    public bool IsSmithChartSelected => SelectedChartView == AntennaChartView.Smith;
    public double SmithZoom { get => _smithZoom; private set => SetProperty(ref _smithZoom, value); }
    public double SmithPanReal { get => _smithPanReal; private set => SetProperty(ref _smithPanReal, value); }
    public double SmithPanImaginary { get => _smithPanImaginary; private set => SetProperty(ref _smithPanImaginary, value); }
    public double? SmithReferenceImpedanceOhms => _profileSnapshot?.ReferenceImpedanceOhms;
    public ObservableCollection<MeasurementPoint> Points { get; } = new();
    public ObservableCollection<string> AvailablePortNames { get; } = new();
    public IReadOnlyList<MeasurementPoint> PlotPoints { get => _plotPoints; private set => SetProperty(ref _plotPoints, value); }
    public IReadOnlyList<MeasurementPoint> AnalysisPlotPoints
    {
        get => _analysisPlotPoints;
        private set
        {
            if (!SetProperty(ref _analysisPlotPoints, value)) return;
            OnPropertyChanged(nameof(CanConfirmCableLossFixture));
            OnPropertyChanged(nameof(IsCableLossPlotAvailable));
            OnPropertyChanged(nameof(CableLossFixtureRequiredText));
        }
    }
    public bool IsCableLossFixtureReady
    {
        get => _cableLossFixtureReady;
        set
        {
            if (!SetProperty(ref _cableLossFixtureReady, value)) return;
            OnPropertyChanged(nameof(IsCableLossPlotAvailable));
        }
    }
    public bool CanConfirmCableLossFixture => _cableLossHardwareDataAvailable && AnalysisPlotPoints.Any(point => point.CableLossDb is not null);
    public bool IsCableLossPlotAvailable => CanConfirmCableLossFixture && IsCableLossFixtureReady;
    public string CableLossFixtureRequiredText => CanConfirmCableLossFixture
        ? Text.Get("CableLossFixtureRequired")
        : Text.Get("CableLossHardwareDataRequired");
    public string CableLossMarker1Text => FormatCableLossMarker(Marker1Frequency);
    public string CableLossMarker2Text => FormatCableLossMarker(Marker2Frequency);
    public ObservableCollection<TraceOption> Traces { get; }
    public RelayCommand SelectAntennaModeCommand { get; }
    public AsyncRelayCommand ConnectDeviceCommand { get; }
    public AsyncRelayCommand DisconnectDeviceCommand { get; }
    public RelayCommand SelectCableLossModeCommand { get; }
    public RelayCommand SelectCableLengthModeCommand { get; }
    public RelayCommand SelectXtalModeCommand { get; }
    public RelayCommand SelectFilterModeCommand { get; }
    public RelayCommand SelectGeneratorModeCommand { get; }
    public RelayCommand SelectFrequencyChartCommand { get; }
    public RelayCommand SelectSmithChartCommand { get; }
    public RelayCommand SelectBothChartsCommand { get; }
    public RelayCommand SmithZoomInCommand { get; }
    public RelayCommand SmithZoomOutCommand { get; }
    public RelayCommand ResetSmithViewCommand { get; }
    public RelayCommand SelectHfBandsCommand { get; }
    public RelayCommand SelectVhfBandsCommand { get; }
    public ICommand StartCommand { get; }
    public AsyncRelayCommand HoldCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearZoomCommand { get; }
    public RelayCommand ShowProfileRangeCommand { get; }
    public RelayCommand SetFullSweepRangeCommand { get; }
    public RelayCommand SetSweepRangeToMarkersCommand { get; }
    public RelayCommand SaveViewRangeCommand { get; }
    public RelayCommand RecallViewRangeCommand { get; }
    public RelayCommand RestoreZoomCommand { get; }
    public RelayCommand ZoomToMarkersCommand { get; }
    public RelayCommand ZoomInCommand { get; }
    public RelayCommand ZoomOutCommand { get; }
    public RelayCommand ShowAllTracesCommand { get; }
    public RelayCommand HideAllTracesCommand { get; }
    public RelayCommand MoveMarker1PreviousCommand { get; }
    public RelayCommand MoveMarker1NextCommand { get; }
    public RelayCommand MoveMarker2PreviousCommand { get; }
    public RelayCommand MoveMarker2NextCommand { get; }
    public RelayCommand SetMarker1ToMinimumSwrCommand { get; }
    public PlotTrace VisibleTraces => Traces.Where(t => t.IsVisible).Aggregate(PlotTrace.None, (mask, t) => mask | t.Trace);
    public bool IsMarker1Visible { get => _isMarker1Visible; set => SetProperty(ref _isMarker1Visible, value); }
    public bool IsMarker2Visible { get => _isMarker2Visible; set => SetProperty(ref _isMarker2Visible, value); }
    public bool IsMinimumSwrVisible { get => _isMinimumSwrVisible; set => SetProperty(ref _isMinimumSwrVisible, value); }
    public bool Show50OhmReference { get => _show50OhmReference; set => SetProperty(ref _show50OhmReference, value); }
    public bool ShowXcXl { get => _showXcXl; set => SetProperty(ref _showXcXl, value); }
    public bool ReturnLossX2 { get => _returnLossX2; set => SetProperty(ref _returnLossX2, value); }
    public bool ShowFullMarkerValues
    {
        get => _showFullMarkerValues;
        set
        {
            if (!SetProperty(ref _showFullMarkerValues, value)) return;
            OnPropertyChanged(nameof(ShowCompactMarkerValues));
        }
    }
    public bool ShowCompactMarkerValues => !ShowFullMarkerValues;
    public string ReturnLossDisplayOffsetText
    {
        get => _returnLossDisplayOffsetText;
        set
        {
            if (!SetProperty(ref _returnLossDisplayOffsetText, value)) return;
            var parsed = double.TryParse(value, NumberStyles.Float, Localizer.Culture, out var offset) &&
                         double.IsFinite(offset) && Math.Abs(offset) <= 300;
            IsReturnLossDisplayOffsetValid = parsed;
            if (parsed && _returnLossDisplayOffsetDb != offset)
            {
                _returnLossDisplayOffsetDb = offset;
                OnPropertyChanged(nameof(ReturnLossDisplayOffsetDb));
                if (Points.Count > 0) RefreshMeasurementViews();
            }
        }
    }
    public double ReturnLossDisplayOffsetDb => _returnLossDisplayOffsetDb;
    public bool IsReturnLossDisplayOffsetValid
    {
        get => _isReturnLossDisplayOffsetValid;
        private set
        {
            if (!SetProperty(ref _isReturnLossDisplayOffsetValid, value)) return;
            OnPropertyChanged(nameof(IsReturnLossDisplayOffsetInvalid));
        }
    }
    public bool IsReturnLossDisplayOffsetInvalid => !IsReturnLossDisplayOffsetValid;
    public string CableVelocityFactorText
    {
        get => _cableVelocityFactorText;
        set
        {
            if (!SetProperty(ref _cableVelocityFactorText, value)) return;
            OnPropertyChanged(nameof(CableLengthResultText));
        }
    }
    public double? CableLengthMeters
    {
        get
        {
            if (Marker1Frequency is not { } first || Marker2Frequency is not { } second ||
                !CableLengthPairValidation.IsValid ||
                TryParseCableVelocityFactor(CableVelocityFactorText, Localizer.Culture) is not { } velocityFactor)
                return null;
            try { return CableLengthCalculator.CalculateMeters(first, second, velocityFactor); }
            catch (ArgumentException) { return null; }
        }
    }
    public CableResonancePairValidation CableLengthPairValidation =>
        CableResonancePairValidator.Validate(Points.ToArray(), Marker1Frequency, Marker2Frequency);
    public string CableLengthResultText => CableLengthMeters is { } meters
        ? $"{meters.ToString("0.###", Localizer.Culture)} m"
        : TryParseCableVelocityFactor(CableVelocityFactorText, Localizer.Culture) is null
            ? Text.Get("CableLengthInvalidVelocityFactor")
            : Text.Get(CableLengthPairValidation.Issue switch
            {
                CableResonancePairIssue.SameMarker => "CableLengthDistinctMarkers",
                CableResonancePairIssue.MarkerNotFound => "CableLengthMarkerOutsideSweep",
                CableResonancePairIssue.DataGapBetweenMarkers => "CableLengthGapBetweenMarkers",
                CableResonancePairIssue.NotAtResonance => "CableLengthMarkersAtResonances",
                CableResonancePairIssue.SameResonance => "CableLengthSeparateResonances",
                CableResonancePairIssue.DifferentKinds => "CableLengthSameResonanceType",
                CableResonancePairIssue.NotAdjacent => "CableLengthAdjacentResonances",
                _ => "CableLengthPickResonanceMarkers",
            });
    public string ResonanceFrequencyMHz
    {
        get => _resonanceFrequencyMHz;
        set { if (SetProperty(ref _resonanceFrequencyMHz, value)) OnPropertyChanged(nameof(ResonanceQResultText)); }
    }
    public string LowerEdgeMHz
    {
        get => _lowerEdgeMHz;
        set { if (SetProperty(ref _lowerEdgeMHz, value)) OnPropertyChanged(nameof(ResonanceQResultText)); }
    }
    public string UpperEdgeMHz
    {
        get => _upperEdgeMHz;
        set { if (SetProperty(ref _upperEdgeMHz, value)) OnPropertyChanged(nameof(ResonanceQResultText)); }
    }
    public IReadOnlyList<GeneratorMemoryPreset> GeneratorMemories { get; }
    public GeneratorMemoryPreset? SelectedGeneratorMemory
    {
        get => _selectedGeneratorMemory;
        set
        {
            if (value is null || !GeneratorMemories.Contains(value) || !SetProperty(ref _selectedGeneratorMemory, value)) return;
            GeneratorFrequencyMHz = (value.FrequencyHz / 1_000_000.0).ToString("0.###", Localizer.Culture);
        }
    }
    public string GeneratorFrequencyMHz
    {
        get => _generatorFrequencyMHz;
        set
        {
            if (!SetProperty(ref _generatorFrequencyMHz, value)) return;
            OnPropertyChanged(nameof(IsGeneratorFrequencyValid));
        }
    }
    public bool IsGeneratorFrequencyValid =>
        TryParseFrequencyMHz(GeneratorFrequencyMHz, out var frequency) &&
        frequency >= Profile.MinimumFrequencyHz && frequency <= Profile.MaximumFrequencyHz;
    public bool CanEnableGeneratorOutput => false;
    public double? ResonanceQ
    {
        get
        {
            if (!TryParseFrequencyMHz(ResonanceFrequencyMHz, out var resonanceHz) ||
                !TryParseFrequencyMHz(LowerEdgeMHz, out var lowerHz) ||
                !TryParseFrequencyMHz(UpperEdgeMHz, out var upperHz) ||
                resonanceHz < Profile.MinimumFrequencyHz || resonanceHz > Profile.MaximumFrequencyHz ||
                lowerHz < Profile.MinimumFrequencyHz || upperHz > Profile.MaximumFrequencyHz)
                return null;
            try { return ResonanceQCalculator.Calculate(resonanceHz, lowerHz, upperHz); }
            catch (ArgumentException) { return null; }
        }
    }
    public string ResonanceQResultText => ResonanceQ is { } q
        ? Text.Format("ResonanceQValue", q.ToString("0.0", Localizer.Culture))
        : Text.Get("ResonanceQEnterFrequencies");
    public AnalysisMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (!Enum.IsDefined(value) || !SetProperty(ref _selectedMode, value)) return;
            if (value == AnalysisMode.CableLoss) IsCableLossFixtureReady = false;
            foreach (var property in new[]
                     {
                         nameof(IsAntennaMode), nameof(IsCableLossMode), nameof(IsCableLengthMode), nameof(IsXtalMode),
                         nameof(IsFilterMode), nameof(IsGeneratorMode), nameof(IsCableLossAnalysisMode), nameof(IsCableLengthAnalysisMode), nameof(IsXtalAnalysisMode), nameof(IsFilterAnalysisMode), nameof(IsGeneratorAnalysisMode), nameof(IsUnavailableMode), nameof(ModeUnavailableMessage), nameof(CanSaveSession),
                     })
                OnPropertyChanged(property);
        }
    }
    public bool IsAntennaMode { get => SelectedMode == AnalysisMode.Antenna; set { if (value) SelectedMode = AnalysisMode.Antenna; } }
    public bool IsCableLossMode { get => SelectedMode == AnalysisMode.CableLoss; set { if (value) SelectedMode = AnalysisMode.CableLoss; } }
    public bool IsCableLengthMode { get => SelectedMode == AnalysisMode.CableLength; set { if (value) SelectedMode = AnalysisMode.CableLength; } }
    public bool IsXtalMode { get => SelectedMode == AnalysisMode.Xtal; set { if (value) SelectedMode = AnalysisMode.Xtal; } }
    public bool IsFilterMode { get => SelectedMode == AnalysisMode.Filter; set { if (value) SelectedMode = AnalysisMode.Filter; } }
    public bool IsGeneratorMode { get => SelectedMode == AnalysisMode.Generator; set { if (value) SelectedMode = AnalysisMode.Generator; } }
    public bool IsCableLossAnalysisMode => SelectedMode == AnalysisMode.CableLoss;
    public bool IsCableLengthAnalysisMode => SelectedMode == AnalysisMode.CableLength;
    public bool IsXtalAnalysisMode => SelectedMode == AnalysisMode.Xtal;
    public bool IsFilterAnalysisMode => SelectedMode == AnalysisMode.Filter;
    public bool IsGeneratorAnalysisMode => SelectedMode == AnalysisMode.Generator;
    public bool IsUnavailableMode => SelectedMode is not (AnalysisMode.Antenna or AnalysisMode.CableLoss or AnalysisMode.CableLength or AnalysisMode.Xtal or AnalysisMode.Filter or AnalysisMode.Generator);
    public string ModeUnavailableMessage => Text.Get(SelectedMode switch
    {
        AnalysisMode.CableLoss => "ModeCableLossUnavailable",
        AnalysisMode.CableLength => "ModeCableLengthUnavailable",
        AnalysisMode.Xtal => "ModeXtalUnavailable",
        AnalysisMode.Filter => "ModeFilterUnavailable",
        AnalysisMode.Generator => "ModeGeneratorUnavailable",
        _ => "ModeAntennaAvailable",
    });
    public AmateurBandGroup SelectedBandGroup
    {
        get => _selectedBandGroup;
        set
        {
            if (!Enum.IsDefined(value) || !SetProperty(ref _selectedBandGroup, value)) return;
            SelectedBandPreset = null;
            OnPropertyChanged(nameof(IsHfBandGroup));
            OnPropertyChanged(nameof(VisibleAmateurBands));
            OnPropertyChanged(nameof(BandGroupHint));
        }
    }
    public bool IsHfBandGroup => SelectedBandGroup == AmateurBandGroup.Hf;
    public IReadOnlyList<AmateurBandPreset> VisibleAmateurBands => BandPresets
        .Where(band => band.Group == SelectedBandGroup && band.Fits(Profile)).ToArray();
    private IReadOnlyList<AmateurBandPreset> BandPresets => Profile.LegacyBandPresets.Count > 0
        ? Profile.LegacyBandPresets
        : PolishAmateurBandPresets.All;
    public AmateurBandPreset? SelectedBandPreset
    {
        get => _selectedBandPreset;
        set
        {
            if (value is null) { SetProperty(ref _selectedBandPreset, null); return; }
            if (!VisibleAmateurBands.Contains(value) || !SetProperty(ref _selectedBandPreset, value)) return;
            StartMHz = (value.StartFrequencyHz / 1_000_000.0).ToString("0.###", Localizer.Culture);
            StopMHz = (value.StopFrequencyHz / 1_000_000.0).ToString("0.###", Localizer.Culture);
        }
    }
    public string BandGroupHint => SelectedBandGroup == AmateurBandGroup.Vhf && VisibleAmateurBands.Count == 0
        ? Text.Format("BandOutsideProfile", Profile.MaximumFrequencyHz / 1_000_000.0)
        : Text.Get("AmateurBandPresetHint");
    public string DataSource { get => _dataSource; private set { if (SetProperty(ref _dataSource, value)) OnPropertyChanged(nameof(DataSourceDisplay)); } }
    public string DataSourceDisplay
    {
        get
        {
            var source = DataSource switch
            {
                "demo" or "DANE DEMONSTRACYJNE" or "DEMONSTRATION DATA" => Text.Get("DemoData"),
                "hardware" => Text.Get("HardwareDevice"),
                "playback" => Text.Get("PlaybackDevice"),
                "unknown" or "nieznane źródło" or "unknown source" => Text.Get("UnknownSource"),
                _ => DataSource,
            };
            return _isSessionLoaded ? Text.Format("SessionSourceFormat", source) : source;
        }
    }
    public bool UseHardwareSource
    {
        get => _measurementSourceMode == DeviceSourceMode.Hardware;
        set => SelectMeasurementSource(value ? DeviceSourceMode.Hardware : DeviceSourceMode.Demo);
    }
    public IReadOnlyList<MeasurementSourceOption> MeasurementSources => new[]
    {
        new MeasurementSourceOption(DeviceSourceMode.Demo, Text.Get("SourceDemo")),
        new MeasurementSourceOption(DeviceSourceMode.Hardware, Text.Get("SourceHardware")),
        new MeasurementSourceOption(DeviceSourceMode.Playback, Text.Get("SourcePlayback")),
    };
    public MeasurementSourceOption SelectedMeasurementSource
    {
        get => MeasurementSources.First(option => option.Mode == _measurementSourceMode);
        set { if (value is not null) SelectMeasurementSource(value.Mode); }
    }
    public bool IsHardwareSource => _measurementSourceMode == DeviceSourceMode.Hardware;
    public bool IsPlaybackSource => _measurementSourceMode == DeviceSourceMode.Playback;
    public bool IsDeviceSourceConnectable => IsHardwareSource || IsPlaybackSource;
    public bool CanSelectMeasurementSource => !IsScanning && !IsDeviceConnected && !_isConnectingDevice && !_isDisposed;
    public bool IsDeviceConnected
    {
        get => _isDeviceConnected;
        private set
        {
            if (!SetProperty(ref _isDeviceConnected, value)) return;
            OnPropertyChanged(nameof(CanSelectMeasurementSource));
            OnPropertyChanged(nameof(CanConnectDevice));
            OnPropertyChanged(nameof(CanDisconnectDevice));
            OnPropertyChanged(nameof(CanChangePort));
            OnPropertyChanged(nameof(CanSetSweepRangeToMarkers));
            ConnectDeviceCommand.NotifyCanExecuteChanged();
            DisconnectDeviceCommand.NotifyCanExecuteChanged();
        }
    }
    public bool IsConnectingDevice
    {
        get => _isConnectingDevice;
        private set
        {
            if (!SetProperty(ref _isConnectingDevice, value)) return;
            OnPropertyChanged(nameof(CanSelectMeasurementSource));
            OnPropertyChanged(nameof(CanConnectDevice));
            OnPropertyChanged(nameof(CanDisconnectDevice));
            OnPropertyChanged(nameof(CanChangePort));
            ConnectDeviceCommand.NotifyCanExecuteChanged();
            DisconnectDeviceCommand.NotifyCanExecuteChanged();
            ((AsyncRelayCommand)StartCommand).NotifyCanExecuteChanged();
        }
    }
    public bool CanConnectDevice =>
        (IsHardwareSource || IsPlaybackSource && (IsSessionLoaded || _lastSweep is not null && Points.Count == _lastSweep.PointCount)) &&
        !IsDeviceConnected && !IsConnectingDevice && !IsScanning && !_isDisposed;
    public bool CanDisconnectDevice => (IsHardwareSource || IsPlaybackSource) && IsDeviceConnected && !IsConnectingDevice && !_isDisposed;
    public bool CanChangePort => !IsDeviceConnected && !IsConnectingDevice && !IsScanning && !_isDisposed;
    public string MeasurementSourceHint => Text.Get(_measurementSourceMode switch
    {
        DeviceSourceMode.Hardware => "HardwareSourceHint",
        DeviceSourceMode.Playback => IsSessionLoaded ? "PlaybackSourceHint" : "PlaybackOpenSessionHint",
        _ => "DemoSourceHint",
    });
    private string ConnectingStatusKey => _measurementSourceMode switch
    {
        DeviceSourceMode.Hardware => "StatusConnectingHardware",
        DeviceSourceMode.Playback => "StatusConnectingPlayback",
        _ => "StatusConnectingSource",
    };
    private string AcquisitionStatusKey => _measurementSourceMode switch
    {
        DeviceSourceMode.Hardware => "StatusAcquiringHardware",
        DeviceSourceMode.Playback => "StatusAcquiringPlayback",
        _ => "StatusAcquiringDemo",
    };
    private static DeviceSourceMode ParseMeasurementSource(string? source) => source?.Trim().ToLowerInvariant() switch
    {
        "hardware" => DeviceSourceMode.Hardware,
        "playback" => DeviceSourceMode.Playback,
        _ => DeviceSourceMode.Demo,
    };

    private void SelectMeasurementSource(DeviceSourceMode mode)
    {
        if (_deviceSourceSelector is null || !CanSelectMeasurementSource || !Enum.IsDefined(mode)) return;
        if (!_deviceSourceSelector.SelectSource(mode)) return;
        if (!SetProperty(ref _measurementSourceMode, mode, nameof(SelectedMeasurementSource))) return;
        OnPropertyChanged(nameof(UseHardwareSource));
        OnPropertyChanged(nameof(IsHardwareSource));
        OnPropertyChanged(nameof(IsPlaybackSource));
        OnPropertyChanged(nameof(IsDeviceSourceConnectable));
        OnPropertyChanged(nameof(MeasurementSourceHint));
        OnPropertyChanged(nameof(CanConnectDevice));
        OnPropertyChanged(nameof(CanDisconnectDevice));
        if (!IsSessionLoaded) DataSource = _device.DataSourceName;
        ConnectDeviceCommand.NotifyCanExecuteChanged();
        DisconnectDeviceCommand.NotifyCanExecuteChanged();
    }

    public string CurrentFileDisplay => _hasUnsavedResult ? Text.Get("UnsavedResult") : _currentSessionFileName ?? Text.Get("NoSessionFile");
    public bool HasUnsavedResult => _hasUnsavedResult;
    public void MarkSessionSaved(string fileName) { _currentSessionFileName = Path.GetFileName(fileName); _hasUnsavedResult = false; OnPropertyChanged(nameof(CurrentFileDisplay)); OnPropertyChanged(nameof(HasUnsavedResult)); }
    private void MarkResultUnsaved() { _currentSessionFileName = null; _hasUnsavedResult = true; OnPropertyChanged(nameof(CurrentFileDisplay)); OnPropertyChanged(nameof(HasUnsavedResult)); }
    public bool IsSessionLoaded
    {
        get => _isSessionLoaded;
        private set
        {
            if (!SetProperty(ref _isSessionLoaded, value)) return;
            OnPropertyChanged(nameof(DataSourceDisplay));
            OnPropertyChanged(nameof(CanConnectDevice));
            OnPropertyChanged(nameof(MeasurementSourceHint));
            ConnectDeviceCommand.NotifyCanExecuteChanged();
        }
    }
    public bool CanLoadSession => !IsScanning && !_isDisposed;
    public bool CanSaveSession => !IsScanning && (IsAntennaMode || IsCableLossMode || IsFilterMode) &&
                                  _lastSweep is not null && Points.Count == _lastSweep.PointCount;
    public bool IsRefreshingPorts
    {
        get => _isRefreshingPorts;
        private set
        {
            if (!SetProperty(ref _isRefreshingPorts, value)) return;
            OnPropertyChanged(nameof(CanRefreshPorts));
        }
    }
    public bool CanRefreshPorts => !IsRefreshingPorts && !_isDisposed;
    public string ProfileLabel => Profile.DisplayName;
    public string ConfigurationPath { get; }
    public string PortName
    {
        get => _portName;
        set
        {
            if (!SetProperty(ref _portName, value)) return;
            var matchingPort = AvailablePortNames.FirstOrDefault(port =>
                string.Equals(port, value, StringComparison.OrdinalIgnoreCase));
            SetProperty(ref _selectedDiscoveredPort, matchingPort, nameof(SelectedDiscoveredPort));
        }
    }
    public string? SelectedDiscoveredPort
    {
        get => _selectedDiscoveredPort;
        set
        {
            if (!SetProperty(ref _selectedDiscoveredPort, value)) return;
            if (value is not null) PortName = value;
        }
    }
    public string StartMHz { get => _startMHz; set => SetProperty(ref _startMHz, value); }
    public string StopMHz { get => _stopMHz; set => SetProperty(ref _stopMHz, value); }
    public string PointCount { get => _pointCount; set => SetProperty(ref _pointCount, value); }
    public string Status => _status.ToString();
    public string Error => _error.ToString();
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (!SetProperty(ref _isScanning, value)) return;
            OnPropertyChanged(nameof(CanLoadSession));
            OnPropertyChanged(nameof(CanSaveSession));
            OnPropertyChanged(nameof(CanSelectMeasurementSource));
            OnPropertyChanged(nameof(CanConnectDevice));
            OnPropertyChanged(nameof(CanDisconnectDevice));
            OnPropertyChanged(nameof(CanChangePort));
            OnPropertyChanged(nameof(ScanProgressValue));
            OnPropertyChanged(nameof(CanSetSweepRangeToMarkers));
            OnPropertyChanged(nameof(ScanProgressDisplay));
            ((AsyncRelayCommand)StartCommand).NotifyCanExecuteChanged();
            ConnectDeviceCommand.NotifyCanExecuteChanged();
            DisconnectDeviceCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
            SetMarker1ToMinimumSwrCommand.NotifyCanExecuteChanged();
            SetFullSweepRangeCommand.NotifyCanExecuteChanged();
            SetSweepRangeToMarkersCommand.NotifyCanExecuteChanged();
        }
    }
    public bool IsContinuousScan
    {
        get => !IsHold;
        set => IsHold = !value;
    }
    public bool IsHold
    {
        get => _isHold;
        set
        {
            if (!SetProperty(ref _isHold, value)) return;
            OnPropertyChanged(nameof(IsContinuousScan));
            OnPropertyChanged(nameof(ContinuousScanHint));
        }
    }
    public string ContinuousScanHint => Text.Get(IsHold ? "HoldAfterSweepHint" : "ContinuousScanHint");
    public double ScanProgressValue => _scanProgressCount;
    public double ScanProgressMaximum => _scanProgressMaximum > 0 ? _scanProgressMaximum : Profile.DefaultPointCount;
    public string ScanProgressDisplay => Text.Format("ScanProgressFormat", ScanProgressValue, ScanProgressMaximum,
        _lastScanDuration is { } duration ? $"{duration.TotalMilliseconds.ToString("0", Localizer.Culture)} ms" : "—");
    public long? Marker1Frequency { get => _marker1; private set { if (SetProperty(ref _marker1, value)) { OnPropertyChanged(nameof(Marker1Text)); OnPropertyChanged(nameof(Marker1Readout)); OnPropertyChanged(nameof(Marker1Values)); OnPropertyChanged(nameof(CableLossMarker1Text)); OnPropertyChanged(nameof(BandwidthHz)); OnPropertyChanged(nameof(BandwidthText)); OnPropertyChanged(nameof(MarkerQEstimate)); OnPropertyChanged(nameof(MarkerQEstimateText)); OnPropertyChanged(nameof(CableLengthPairValidation)); OnPropertyChanged(nameof(CableLengthMeters)); OnPropertyChanged(nameof(CableLengthResultText)); UpdateZoomAvailability(); } } }
    public long? Marker2Frequency { get => _marker2; private set { if (SetProperty(ref _marker2, value)) { OnPropertyChanged(nameof(Marker2Text)); OnPropertyChanged(nameof(Marker2Readout)); OnPropertyChanged(nameof(Marker2Values)); OnPropertyChanged(nameof(CableLossMarker2Text)); OnPropertyChanged(nameof(BandwidthHz)); OnPropertyChanged(nameof(BandwidthText)); OnPropertyChanged(nameof(MarkerQEstimate)); OnPropertyChanged(nameof(MarkerQEstimateText)); OnPropertyChanged(nameof(CableLengthPairValidation)); OnPropertyChanged(nameof(CableLengthMeters)); OnPropertyChanged(nameof(CableLengthResultText)); UpdateZoomAvailability(); } } }
    public long ViewStartHz { get => _viewStartHz; private set => SetProperty(ref _viewStartHz, value); }
    public long ViewStopHz { get => _viewStopHz; private set => SetProperty(ref _viewStopHz, value); }
    public string Marker1Text => FormatMarker(Marker1Frequency);
    public string Marker2Text => FormatMarker(Marker2Frequency);
    public string Marker1Readout => FormatPoint(FindNearest(Marker1Frequency));
    public string Marker2Readout => FormatPoint(FindNearest(Marker2Frequency));
    public MarkerValues Marker1Values => FormatMarkerValues(Marker1Frequency);
    public MarkerValues Marker2Values => FormatMarkerValues(Marker2Frequency);
    public long? BandwidthHz => Marker1Frequency is null || Marker2Frequency is null ? null : Math.Abs(Marker2Frequency.Value - Marker1Frequency.Value);
    public int? MarkerQEstimate => MarkerCalculations.EstimateMarkerQ(Marker1Frequency, Marker2Frequency);
    public string MarkerQEstimateText => MarkerQEstimate?.ToString(Localizer.Culture) ?? "—";
    public string BandwidthText => BandwidthHz is null
        ? "—"
        : $"{(BandwidthHz.Value / 1_000_000.0).ToString("0.###", Localizer.Culture)} MHz";
    public bool CanZoomToMarkers => Marker1Frequency is not null && Marker2Frequency is not null && Marker1Frequency != Marker2Frequency;
    public bool CanSetSweepRangeToMarkers => !IsScanning && !_isDisposed && CanZoomToMarkers &&
        Math.Min(Marker1Frequency!.Value, Marker2Frequency!.Value) >= Profile.MinimumFrequencyHz &&
        Math.Max(Marker1Frequency.Value, Marker2Frequency.Value) <= Profile.MaximumFrequencyHz;
    public bool CanRestoreZoom => _zoomHistory.Count > 0;
    public bool CanZoomIn
    {
        get
        {
            if (_isDisposed || Points.Count < 2) return false;
            var (start, stop) = GetCurrentPlotRange();
            return stop - start > 1;
        }
    }
    public bool CanZoomOut
    {
        get
        {
            if (_isDisposed || Points.Count < 2) return false;
            var (start, stop) = GetCurrentPlotRange();
            return stop > start && stop - start < Profile.MaximumFrequencyHz - Profile.MinimumFrequencyHz;
        }
    }
    public string MinimumSwrText
    {
        get
        {
            var minimum = MeasurementPointQueries.FindMinimumSwr(PlotPoints);
            return minimum is null ? "—" : Text.Format("MinimumSwrFormat", minimum.Swr, minimum.FrequencyHz / 1_000_000.0);
        }
    }
    public long? MinimumSwrFrequency => MeasurementPointQueries.FindMinimumSwr(PlotPoints)?.FrequencyHz;

    public void PickMarker(long frequencyHz, bool isMarker2)
    {
        var point = MeasurementPointQueries.FindNearest(Points, frequencyHz,
            ViewStopHz > ViewStartHz ? ViewStartHz : 0,
            ViewStopHz > ViewStartHz ? ViewStopHz : long.MaxValue);
        if (point is null) return;
        if (isMarker2) Marker2Frequency = point.FrequencyHz;
        else Marker1Frequency = point.FrequencyHz;
    }

    public void MoveMarkerSample(bool isMarker2, int direction)
    {
        if (direction == 0 || Points.Count == 0) return;
        var currentFrequency = isMarker2 ? Marker2Frequency : Marker1Frequency;
        if (currentFrequency is null) return;

        var start = ViewStopHz > ViewStartHz ? ViewStartHz : long.MinValue;
        var stop = ViewStopHz > ViewStartHz ? ViewStopHz : long.MaxValue;
        var visiblePoints = Points.Where(point => point.FrequencyHz >= start && point.FrequencyHz <= stop).ToArray();
        var currentIndex = Array.FindIndex(visiblePoints, point => point.FrequencyHz == currentFrequency.Value);
        var nextIndex = currentIndex + Math.Sign(direction);
        if (currentIndex < 0 || nextIndex < 0 || nextIndex >= visiblePoints.Length) return;

        if (isMarker2) Marker2Frequency = visiblePoints[nextIndex].FrequencyHz;
        else Marker1Frequency = visiblePoints[nextIndex].FrequencyHz;
    }

    private void SetStatus(string key, params object?[] arguments)
    {
        _status = LocalizedMessage.Resource(key, arguments);
        OnPropertyChanged(nameof(Status));
    }

    private void SetError(LocalizedMessage message)
    {
        _error = message;
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
    }

    private void ClearError() => SetError(LocalizedMessage.Literal(string.Empty));
    public void ReportStatus(string message)
    {
        ClearError();
        _status = LocalizedMessage.Literal(message);
        OnPropertyChanged(nameof(Status));
    }
    public void ReportError(string message) => SetError(LocalizedMessage.Literal(message));
    public void ReportException(Exception exception) => SetError(LocalizedErrors.Message(exception));
    public void ReportLocalizedStatus(string key, params object?[] arguments)
    {
        ClearError();
        SetStatus(key, arguments);
    }
    public void ReportLocalizedError(string key, params object?[] arguments) => SetError(LocalizedMessage.Resource(key, arguments));

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(LocalizationService.Culture)) return;
        if (double.TryParse(StartMHz, NumberStyles.Float, _inputCulture, out var start) && double.IsFinite(start))
            StartMHz = start.ToString("G", Localizer.Culture);
        if (double.TryParse(StopMHz, NumberStyles.Float, _inputCulture, out var stop) && double.IsFinite(stop))
            StopMHz = stop.ToString("G", Localizer.Culture);
        if (double.TryParse(CableVelocityFactorText, NumberStyles.Float, _inputCulture, out var cableVf) && double.IsFinite(cableVf))
            CableVelocityFactorText = cableVf.ToString("G", Localizer.Culture);
        if (double.TryParse(ReturnLossDisplayOffsetText, NumberStyles.Float, _inputCulture, out var rlOffset) && double.IsFinite(rlOffset))
            ReturnLossDisplayOffsetText = rlOffset.ToString("G", Localizer.Culture);
        ResonanceFrequencyMHz = ReformatFrequency(ResonanceFrequencyMHz, _inputCulture, Localizer.Culture);
        LowerEdgeMHz = ReformatFrequency(LowerEdgeMHz, _inputCulture, Localizer.Culture);
        UpperEdgeMHz = ReformatFrequency(UpperEdgeMHz, _inputCulture, Localizer.Culture);
        GeneratorFrequencyMHz = ReformatFrequency(GeneratorFrequencyMHz, _inputCulture, Localizer.Culture);
        OnPropertyChanged(nameof(MeasurementSources));
        OnPropertyChanged(nameof(SelectedMeasurementSource));
        _inputCulture = Localizer.Culture;
        OnPropertyChanged(nameof(CableLengthResultText));
        foreach (var trace in Traces) trace.RefreshLanguage();
        foreach (var theme in Themes) theme.RefreshLanguage();
        foreach (var property in new[] { nameof(SelectedLanguage), nameof(Status), nameof(Error), nameof(HasError),
                     nameof(DataSourceDisplay), nameof(MarkerHint), nameof(Marker1Text), nameof(Marker2Text),
                     nameof(Marker1Readout), nameof(Marker2Readout), nameof(Marker1Values), nameof(Marker2Values),
                     nameof(MinimumSwrText), nameof(ModeUnavailableMessage), nameof(BandGroupHint), nameof(ContinuousScanHint), nameof(MeasurementSourceHint), nameof(CurrentFileDisplay), nameof(MarkerQEstimateText), nameof(BandwidthText), nameof(ScanProgressDisplay), nameof(CableLengthResultText), nameof(ResonanceQResultText) })
            OnPropertyChanged(property);
    }

    public async Task RefreshPortNamesAsync(CancellationToken cancellationToken = default)
    {
        if (!CanRefreshPorts) return;
        var refreshStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _portDiscoveryStopped = refreshStopped;
        IsRefreshingPorts = true;
        try
        {
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _lifetimeCancellation.Token);
            var portNames = await _portCatalog.GetAvailablePortNamesAsync(linkedCancellation.Token);
            linkedCancellation.Token.ThrowIfCancellationRequested();

            ClearError();
            AvailablePortNames.Clear();
            foreach (var portName in portNames
                         .Where(name => !string.IsNullOrWhiteSpace(name))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                AvailablePortNames.Add(portName);

            var selected = AvailablePortNames.FirstOrDefault(port =>
                string.Equals(port, PortName, StringComparison.OrdinalIgnoreCase));
            SetProperty(ref _selectedDiscoveredPort, selected, nameof(SelectedDiscoveredPort));
            SetStatus(AvailablePortNames.Count == 0 ? "PortsEmpty" : "PortsFound", AvailablePortNames.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || _lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ReportLocalizedError("PortDiscoveryFailed", LocalizedErrors.Message(exception));
        }
        finally
        {
            IsRefreshingPorts = false;
            refreshStopped.TrySetResult();
            if (ReferenceEquals(_portDiscoveryStopped, refreshStopped)) _portDiscoveryStopped = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        OnPropertyChanged(nameof(CanLoadSession));
        OnPropertyChanged(nameof(CanRefreshPorts));
        _settingsSubscription?.Dispose();
        Localizer.PropertyChanged -= OnLanguageChanged;
        _lifetimeCancellation.Cancel();
        _scanCancellation?.Cancel();
        var pendingOperations = new List<Task>();
        if (_scanStopped is { } scanStopped) pendingOperations.Add(scanStopped.Task);
        if (_portDiscoveryStopped is { } portDiscoveryStopped) pendingOperations.Add(portDiscoveryStopped.Task);
        if (pendingOperations.Count > 0) await Task.WhenAll(pendingOperations).ConfigureAwait(false);
        _lifetimeCancellation.Dispose();
    }

    private (long Start, long Stop) GetCurrentPlotRange() => ViewStopHz > ViewStartHz
        ? (ViewStartHz, ViewStopHz)
        : Points.Count > 1 ? (Points[0].FrequencyHz, Points[^1].FrequencyHz) : (0, 0);

    private void ChangeZoom(bool zoomIn)
    {
        if (zoomIn ? !CanZoomIn : !CanZoomOut) return;
        var (start, stop) = GetCurrentPlotRange();
        var span = stop - start;
        var fullSpan = Profile.MaximumFrequencyHz - Profile.MinimumFrequencyHz;
        var targetSpan = zoomIn ? Math.Max(1, span / 2)
            : span > fullSpan / 2 ? fullSpan : span * 2;
        var targetStart = Math.Clamp(start + (span - targetSpan) / 2,
            Profile.MinimumFrequencyHz, Profile.MaximumFrequencyHz - targetSpan);
        var targetStop = targetStart + targetSpan;
        if (targetStart == start && targetStop == stop) return;
        _zoomHistory.Push((ViewStartHz, ViewStopHz));
        ViewStartHz = targetStart;
        ViewStopHz = targetStop;
        UpdateZoomHistoryAvailability();
    }

    public void PanFrequencyRange(long deltaHz)
    {
        if (deltaHz == 0 || Points.Count < 2) return;
        var (start, stop) = GetCurrentPlotRange();
        var span = stop - start;
        var profileSpan = Profile.MaximumFrequencyHz - Profile.MinimumFrequencyHz;
        if (span <= 0 || span >= profileSpan) return;

        var minimumStart = Profile.MinimumFrequencyHz;
        var maximumStart = Profile.MaximumFrequencyHz - span;
        var targetStart = deltaHz > 0 && deltaHz > maximumStart - start
            ? maximumStart
            : deltaHz < 0 && deltaHz < minimumStart - start
                ? minimumStart
                : start + deltaHz;
        if (targetStart == start) return;
        ViewStartHz = targetStart;
        ViewStopHz = targetStart + span;
        UpdateScaleZoomAvailability();
    }

    private void UpdateScaleZoomAvailability()
    {
        OnPropertyChanged(nameof(CanZoomIn));
        OnPropertyChanged(nameof(CanZoomOut));
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
    }

    private void ZoomToMarkers()
    {
        if (!CanZoomToMarkers) return;
        var start = Math.Min(Marker1Frequency!.Value, Marker2Frequency!.Value);
        var stop = Math.Max(Marker1Frequency.Value, Marker2Frequency.Value);
        if (start == ViewStartHz && stop == ViewStopHz) return;
        _zoomHistory.Push((ViewStartHz, ViewStopHz));
        ViewStartHz = start;
        ViewStopHz = stop;
        UpdateZoomHistoryAvailability();
    }

    private void RestoreZoom()
    {
        if (!_zoomHistory.TryPop(out var previous)) return;
        ViewStartHz = previous.Start;
        ViewStopHz = previous.Stop;
        UpdateZoomHistoryAvailability();
    }

    private void ResetZoom()
    {
        _zoomHistory.Clear();
        ViewStartHz = ViewStopHz = 0;
        UpdateZoomHistoryAvailability();
    }

    public void ChangeSmithZoom(double factor, SmithComplex anchor, double normalizedOffsetX, double normalizedOffsetY)
    {
        if (!double.IsFinite(factor) || factor <= 0 || !anchor.IsFinite ||
            !double.IsFinite(normalizedOffsetX) || !double.IsFinite(normalizedOffsetY)) return;
        var nextZoom = Math.Clamp(SmithZoom * factor, 1, 8);
        if (nextZoom == SmithZoom) return;
        SmithPanReal = Math.Clamp(anchor.Real - normalizedOffsetX / nextZoom, -8, 8);
        SmithPanImaginary = Math.Clamp(anchor.Imaginary - normalizedOffsetY / nextZoom, -8, 8);
        SmithZoom = nextZoom;
    }

    public void ChangeSmithZoom(double factor) =>
        ChangeSmithZoom(factor, new SmithComplex(SmithPanReal, SmithPanImaginary), 0, 0);

    public void PanSmith(double deltaReal, double deltaImaginary)
    {
        if (!double.IsFinite(deltaReal) || !double.IsFinite(deltaImaginary)) return;
        SmithPanReal = Math.Clamp(SmithPanReal + deltaReal, -8, 8);
        SmithPanImaginary = Math.Clamp(SmithPanImaginary + deltaImaginary, -8, 8);
    }

    public void ResetSmithView()
    {
        SmithZoom = 1;
        SmithPanReal = 0;
        SmithPanImaginary = 0;
    }

    private void ShowProfileRange()
    {
        if (ViewStartHz == Profile.MinimumFrequencyHz && ViewStopHz == Profile.MaximumFrequencyHz) return;
        _zoomHistory.Push((ViewStartHz, ViewStopHz));
        ViewStartHz = Profile.MinimumFrequencyHz;
        ViewStopHz = Profile.MaximumFrequencyHz;
        UpdateZoomHistoryAvailability();
    }

    private void SetFullSweepRange()
    {
        if (IsScanning || _isDisposed) return;
        StartMHz = (Profile.MinimumFrequencyHz / 1_000_000d).ToString("0.######", Localizer.Culture);
        StopMHz = (Profile.MaximumFrequencyHz / 1_000_000d).ToString("0.######", Localizer.Culture);
    }

    private void SetSweepRangeToMarkers()
    {
        if (!CanSetSweepRangeToMarkers) return;
        StartMHz = (Math.Min(Marker1Frequency!.Value, Marker2Frequency!.Value) / 1_000_000d)
            .ToString("0.######", Localizer.Culture);
        StopMHz = (Math.Max(Marker1Frequency.Value, Marker2Frequency.Value) / 1_000_000d)
            .ToString("0.######", Localizer.Culture);
    }

    private void SaveViewRange()
    {
        _savedViewRange = (ViewStartHz, ViewStopHz);
        RecallViewRangeCommand.NotifyCanExecuteChanged();
    }

    private void RecallViewRange()
    {
        if (_savedViewRange is not { } range) return;
        if (range.Start == ViewStartHz && range.Stop == ViewStopHz) return;
        _zoomHistory.Push((ViewStartHz, ViewStopHz));
        ViewStartHz = range.Start;
        ViewStopHz = range.Stop;
        UpdateZoomHistoryAvailability();
    }

    private void UpdateZoomHistoryAvailability()
    {
        OnPropertyChanged(nameof(CanRestoreZoom));
        RestoreZoomCommand.NotifyCanExecuteChanged();
        UpdateScaleZoomAvailability();
    }

    private void ApplyProfilePalette(DeviceProfile profile)
    {
        PlotPalette = Vna.App.Plotting.PlotPalette.FromProfile(profile, SelectedTheme.Kind, _traceColorOverrides);
        foreach (var trace in Traces) trace.ApplyPalette(PlotPalette);
    }

    private void SetTraceColorOverride(PlotTrace trace, string color)
    {
        var normalized = NormalizeColor(color);
        if (normalized is null) return;
        var option = Traces.First(candidate => candidate.Trace == trace);
        if (string.Equals(normalized, option.DefaultColor, StringComparison.OrdinalIgnoreCase))
            _traceColorOverrides.Remove(trace);
        else
            _traceColorOverrides[trace] = normalized;
        ApplyProfilePalette(_profileSnapshot ?? Profile);
    }

    private void ApplyTraceColorOverrides(IReadOnlyDictionary<string, string>? colors)
    {
        var parsed = ParseTraceColorOverrides(colors);
        _traceColorOverrides.Clear();
        foreach (var pair in parsed) _traceColorOverrides.Add(pair.Key, pair.Value);

        _updatingTraceColors = true;
        try
        {
            foreach (var trace in Traces)
                trace.SelectedColor = _traceColorOverrides.GetValueOrDefault(trace.Trace, trace.DefaultColor);
        }
        finally { _updatingTraceColors = false; }
        ApplyProfilePalette(_profileSnapshot ?? Profile);
    }

    private static Dictionary<PlotTrace, string> ParseTraceColorOverrides(IReadOnlyDictionary<string, string>? colors)
    {
        var result = new Dictionary<PlotTrace, string>();
        if (colors is null) return result;
        foreach (var (key, value) in colors)
        {
            if (!Enum.TryParse<PlotTrace>(key, true, out var trace) || trace is PlotTrace.None or PlotTrace.All ||
                !Enum.IsDefined(trace)) continue;
            if (NormalizeColor(value) is { } normalized) result[trace] = normalized;
        }
        return result;
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            var color = Color.Parse(value);
            return color.A == byte.MaxValue ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : null;
        }
        catch (ArgumentException) { return null; }
    }

    private void SetTraceVisibility(bool isVisible)
    {
        foreach (var trace in Traces) trace.IsVisible = isVisible;
    }

    private void SetMarker1ToMinimumSwr()
    {
        if (MinimumSwrFrequency is not { } frequency || IsScanning) return;
        if (ViewStopHz > ViewStartHz && (frequency < ViewStartHz || frequency > ViewStopHz))
        {
            _zoomHistory.Push((ViewStartHz, ViewStopHz));
            ViewStartHz = ViewStopHz = 0;
            UpdateZoomHistoryAvailability();
        }
        Marker1Frequency = frequency;
    }

    private void UpdateMinimumSwrAvailability()
    {
        OnPropertyChanged(nameof(MinimumSwrText));
        OnPropertyChanged(nameof(MinimumSwrFrequency));
        SetMarker1ToMinimumSwrCommand.NotifyCanExecuteChanged();
    }

    private void UpdateZoomAvailability()
    {
        OnPropertyChanged(nameof(CanZoomToMarkers));
        OnPropertyChanged(nameof(CanSetSweepRangeToMarkers));
        ZoomToMarkersCommand.NotifyCanExecuteChanged();
        SetSweepRangeToMarkersCommand.NotifyCanExecuteChanged();
    }

    public string MarkerHint => Text.Get("MarkerHint");

    public MeasurementSession? CreateSession()
    {
        if (!(IsAntennaMode || IsCableLossMode || IsFilterMode) || _lastSweep is null || Points.Count != _lastSweep.PointCount) return null;
        return new MeasurementSession(MeasurementSessionFile.CurrentFormatVersion, Profile.Id, DataSource, _capturedAtUtc, _lastSweep, Points.ToArray())
        {
            ProfileSnapshot = _profileSnapshot,
            AnalysisMode = Enum.Parse<SessionAnalysisMode>(SelectedMode.ToString()),
        };
    }

    public void LoadSession(MeasurementSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (IsScanning)
            throw LocalizedErrors.InvalidOperation("SessionStopFirst");
        if (!string.Equals(session.ProfileId, Profile.Id, StringComparison.Ordinal))
            throw LocalizedErrors.InvalidData("SessionProfileMismatch", session.ProfileId, Profile.Id);

        session.Sweep.ValidateAgainst(Profile);
        if (session.Points.Count != session.Sweep.PointCount)
            throw LocalizedErrors.InvalidData("SessionSampleCountMismatch");

        var previousFrequencyHz = -1L;
        for (var index = 0; index < session.Points.Count; index++)
        {
            var frequencyHz = session.Points[index].FrequencyHz;
            if (frequencyHz <= previousFrequencyHz || frequencyHz < session.Sweep.StartFrequencyHz ||
                frequencyHz > session.Sweep.StopFrequencyHz)
                throw LocalizedErrors.InvalidData("SessionPointFrequencyMismatch", index);
            previousFrequencyHz = frequencyHz;
        }

        (_device as ISessionPlaybackSource)?.SetSession(session);
        IsCableLossFixtureReady = false;
        _cableLossHardwareDataAvailable = session.AnalysisMode == SessionAnalysisMode.CableLoss &&
                                          string.Equals(session.DataSource, "hardware", StringComparison.OrdinalIgnoreCase);

        _profileSnapshot = session.ProfileSnapshot;
        OnPropertyChanged(nameof(SmithReferenceImpedanceOhms));
        ApplyProfilePalette(_profileSnapshot ?? Profile);
        Points.Clear();
        foreach (var point in session.Points) Points.Add(point);
        RefreshMeasurementViews();
        StartMHz = (session.Sweep.StartFrequencyHz / 1_000_000.0).ToString("0.###", Localizer.Culture);
        StopMHz = (session.Sweep.StopFrequencyHz / 1_000_000.0).ToString("0.###", Localizer.Culture);
        PointCount = session.Sweep.PointCount.ToString(Localizer.Culture);
        _lastSweep = session.Sweep;
        OnPropertyChanged(nameof(ScanProgressMaximum));
        SelectedMode = Enum.Parse<AnalysisMode>(session.AnalysisMode.ToString());
        DataSource = string.IsNullOrWhiteSpace(session.DataSource) ? "unknown" : session.DataSource;
        IsSessionLoaded = true;
        _currentSessionFileName = null;
        _hasUnsavedResult = false;
        OnPropertyChanged(nameof(CurrentFileDisplay));
        OnPropertyChanged(nameof(HasUnsavedResult));
        _capturedAtUtc = session.CapturedAtUtc;
        _scanProgressCount = Points.Count;
        _scanProgressMaximum = session.Sweep.PointCount;
        OnPropertyChanged(nameof(ScanProgressValue));
        OnPropertyChanged(nameof(ScanProgressMaximum));
        OnPropertyChanged(nameof(ScanProgressDisplay));
        Marker1Frequency = Marker2Frequency = null;
        ResetZoom();
        UpdateMinimumSwrAvailability();
        OnPropertyChanged(nameof(Marker1Readout));
        OnPropertyChanged(nameof(Marker2Readout));
        OnPropertyChanged(nameof(Marker1Values));
        OnPropertyChanged(nameof(Marker2Values));
        SetStatus("SessionLoaded", Points.Count);
        ClearError();
    }

    private MeasurementPoint? FindNearest(long? frequency)
    {
        if (frequency is null || Points.Count == 0) return null;
        var point = MeasurementPointQueries.FindNearest(Points, frequency.Value);
        return point is null ? null : ApplyReturnLossDisplayOffset(point);
    }

    private void RefreshMeasurementViews()
    {
        PlotPoints = Array.AsReadOnly(Points.Select(ApplyReturnLossDisplayOffset).ToArray());
        AnalysisPlotPoints = Array.AsReadOnly(Points.ToArray());
        OnPropertyChanged(nameof(Marker1Readout));
        OnPropertyChanged(nameof(Marker2Readout));
        OnPropertyChanged(nameof(Marker1Values));
        OnPropertyChanged(nameof(Marker2Values));
        OnPropertyChanged(nameof(CableLossMarker1Text));
        OnPropertyChanged(nameof(CableLossMarker2Text));
        UpdateMinimumSwrAvailability();
    }

    private string FormatCableLossMarker(long? frequencyHz)
    {
        if (frequencyHz is null) return "—";
        var point = Points.FirstOrDefault(sample => sample.FrequencyHz == frequencyHz.Value);
        return point?.CableLossDb is { } loss
            ? $"{loss.ToString("0.00", Localizer.Culture)} dB"
            : "—";
    }

    private MeasurementPoint ApplyReturnLossDisplayOffset(MeasurementPoint point)
    {
        if (_returnLossDisplayOffsetDb == 0 || point.Quality != SampleQuality.Valid ||
            point.ReturnLossDb is not { } returnLossDb || point.PhaseDegrees is not { } phaseDegrees)
            return point;
        return RfSampleCalculator.FromReturnLossAndPhase(
            point.FrequencyHz, returnLossDb + _returnLossDisplayOffsetDb, phaseDegrees, _profileSnapshot ?? Profile) with
        {
            PhaseSignConfidence = point.PhaseSignConfidence,
            PhaseSignProvenance = point.PhaseSignProvenance,
        };
    }

    private static string FormatPoint(MeasurementPoint? point)
    {
        if (point is null) return Text.Get("MarkerEmptyReadout");
        if (point.Quality != SampleQuality.Valid) return Text.Get("InvalidSampleReadout");
        static string Value(double? value, string format) => value is { } number && double.IsFinite(number)
            ? number.ToString(format, LocalizationService.Instance.Culture) : "—";
        return Text.Format("MarkerReadoutFormat", Value(point.Swr, "0.00"), Value(point.ReturnLossDb, "0.0"),
            Value(point.PhaseDegrees, "0.0"), Value(point.ImpedanceOhms, "0.0"), Value(point.ResistanceOhms, "0.0"),
            Value(point.ReactanceOhms is { } x ? Math.Abs(x) : null, "0.0"));
    }

    private MarkerValues FormatMarkerValues(long? frequencyHz)
    {
        if (frequencyHz is null) return MarkerValues.Empty;
        var point = FindNearest(frequencyHz);
        if (point is null) return MarkerValues.Empty;

        var culture = Localizer.Culture;
        static string Value(double? value, string format, CultureInfo culture) =>
            value is { } number && double.IsFinite(number) ? number.ToString(format, culture) : "—";

        return new MarkerValues(
            point.FrequencyHz.ToString("N0", culture),
            point.Quality == SampleQuality.Valid ? Value(point.Swr, "0.00", culture) : "—",
            point.Quality == SampleQuality.Valid ? Value(point.ImpedanceOhms, "0.0", culture) : "—",
            point.Quality == SampleQuality.Valid ? Value(point.ReturnLossDb, "0.0", culture) : "—",
            point.Quality == SampleQuality.Valid ? Value(point.PhaseDegrees, "0.0", culture) : "—",
            point.Quality == SampleQuality.Valid ? Value(point.ResistanceOhms, "0.0", culture) : "—",
            point.Quality == SampleQuality.Valid
                ? Value(point.ReactanceOhms is { } reactance ? Math.Abs(reactance) : null, "0.0", culture)
                : "—")
        {
            ReactanceComponent = FormatReactanceComponent(point, culture),
        };
    }

    private static string FormatReactanceComponent(MeasurementPoint point, CultureInfo culture)
    {
        if (point.Quality != SampleQuality.Valid || point.ReactanceOhms is not { } reactance ||
            !double.IsFinite(reactance) || reactance == 0 || point.FrequencyHz <= 0)
            return "—";

        var frequencyHz = (double)point.FrequencyHz;
        var value = reactance < 0
            ? 1d / (2d * Math.PI * frequencyHz * Math.Abs(reactance)) * 1e12
            : reactance / (2d * Math.PI * frequencyHz) * 1e6;
        return double.IsFinite(value)
            ? $"{value.ToString("0.###", culture)} {(reactance < 0 ? "pF" : "µH")}" 
            : "—";
    }

    private static double GetCableVelocityFactor(double? configuredValue, DeviceProfile profile) =>
        configuredValue is { } value && double.IsFinite(value) && value is > 0 and <= 1
            ? value
            : profile.LegacyCalibration.InputVelocityFactor;

    private static double? TryParseCableVelocityFactor(string? text, CultureInfo culture) =>
        double.TryParse(text, NumberStyles.Float, culture, out var value) && double.IsFinite(value) && value is > 0 and <= 1
            ? value
            : null;

    private bool TryParseFrequencyMHz(string? text, out long frequencyHz)
    {
        frequencyHz = 0;
        if (!double.TryParse(text, NumberStyles.Float, Localizer.Culture, out var frequencyMHz) ||
            !double.IsFinite(frequencyMHz) || frequencyMHz <= 0 || frequencyMHz > 100_000)
            return false;
        frequencyHz = (long)Math.Round(frequencyMHz * 1_000_000, MidpointRounding.AwayFromZero);
        return true;
    }

    private static string ReformatFrequency(string value, CultureInfo source, CultureInfo destination) =>
        double.TryParse(value, NumberStyles.Float, source, out var frequency) && double.IsFinite(frequency)
            ? frequency.ToString("G", destination)
            : value;

    private async Task ConnectDeviceAsync()
    {
        if (!CanConnectDevice) return;
        IsConnectingDevice = true;
        ClearError();
        SetStatus(_measurementSourceMode == DeviceSourceMode.Playback ? "StatusConnectingPlayback" : "StatusConnectingHardware");
        try
        {
            using var connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
            await _device.ConnectAsync(new DeviceConnectionOptions(PortName), connectionCancellation.Token);
            IsDeviceConnected = true;
            if (!IsSessionLoaded) DataSource = _device.DataSourceName;
            SetStatus(_measurementSourceMode == DeviceSourceMode.Playback ? "StatusConnectedPlayback" : "StatusConnectedHardware");
        }
        catch (OperationCanceledException)
        {
            SetStatus("StatusScanStopped", 0);
        }
        catch (Exception exception)
        {
            ReportException(exception);
            SetStatus("StatusConnectFailed");
        }
        finally
        {
            IsConnectingDevice = false;
        }
    }

    private async Task DisconnectDeviceAsync()
    {
        if (!CanDisconnectDevice) return;
        if (IsScanning)
        {
            _scanCancellation?.Cancel();
            if (_scanStopped is { } scanStopped)
                await scanStopped.Task;
        }
        if (!IsDeviceConnected) return;

        try
        {
            using var disconnectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _device.DisconnectAsync(disconnectTimeout.Token);
            IsDeviceConnected = false;
            SetStatus("StatusDisconnectedHardware");
        }
        catch (Exception exception)
        {
            ReportException(exception);
            SetStatus("StatusDisconnectFailed");
        }
    }

    private async Task HandleHoldChangedAsync()
    {
        if (_isDisposed || IsHold || IsScanning || IsConnectingDevice) return;
        await ((AsyncRelayCommand)StartCommand).ExecuteAsync();
    }

    private void StopScanning()
    {
        IsHold = true;
        _scanCancellation?.Cancel();
    }

    private async Task StartScanAsync()
    {
        if (_isDisposed || IsConnectingDevice) return;
        var scanStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _scanStopped = scanStopped;
        OnPropertyChanged(nameof(ScanProgressDisplay));
        ClearError();
        List<MeasurementPoint>? pendingPoints = null;
        var connectionAttempted = false;
        var scanFailed = false;
        var scanCancelled = false;
        try
        {
            var settings = CreateSweepSettings();
            settings.ValidateAgainst(Profile);
            IsScanning = true;
            SetStatus(IsDeviceConnected ? AcquisitionStatusKey : ConnectingStatusKey);
            _scanCancellation = new CancellationTokenSource();
            if (!IsDeviceConnected)
            {
                connectionAttempted = true;
                await _device.ConnectAsync(new DeviceConnectionOptions(PortName), _scanCancellation.Token);
                IsDeviceConnected = true;
            }
            do
            {
                var scanStartedAt = Stopwatch.GetTimestamp();
                pendingPoints = new List<MeasurementPoint>(settings.PointCount);
                _scanProgressCount = 0;
                _scanProgressMaximum = settings.PointCount;
                OnPropertyChanged(nameof(ScanProgressValue));
                OnPropertyChanged(nameof(ScanProgressMaximum));
                OnPropertyChanged(nameof(ScanProgressDisplay));
                SetStatus(AcquisitionStatusKey);
                var deviceSweepMode = SelectedMode == AnalysisMode.Filter
                    ? DeviceSweepMode.ScalarTransmission
                    : DeviceSweepMode.Reflection;
                await foreach (var point in _device.SweepAsync(settings, deviceSweepMode, _scanCancellation.Token))
                {
                    pendingPoints.Add(point);
                    _scanProgressCount = pendingPoints.Count;
                    OnPropertyChanged(nameof(ScanProgressValue));
                    OnPropertyChanged(nameof(ScanProgressDisplay));
                }

                ValidateCompletedSweep(settings, pendingPoints);

                // Each repetition commits atomically. Cancellation or failure in the next scan
                // leaves the previous complete plot, session and duration intact.
                var sweepChanged = _lastSweep != settings;
                _lastSweep = settings;
                _profileSnapshot = Profile;
                OnPropertyChanged(nameof(SmithReferenceImpedanceOhms));
                ApplyProfilePalette(Profile);
                DataSource = _device.DataSourceName;
                IsSessionLoaded = false;
                _capturedAtUtc = DateTimeOffset.UtcNow;
                _cableLossHardwareDataAvailable = _measurementSourceMode == DeviceSourceMode.Hardware &&
                                                  SelectedMode == AnalysisMode.CableLoss;
                Points.Clear();
                foreach (var point in pendingPoints) Points.Add(point);
                RefreshMeasurementViews();
                (_device as ISessionPlaybackSource)?.SetSession(new MeasurementSession(
                    MeasurementSessionFile.CurrentFormatVersion, Profile.Id, _device.DataSourceName,
                    _capturedAtUtc, settings, pendingPoints.ToArray())
                {
                    ProfileSnapshot = _profileSnapshot,
                    AnalysisMode = Enum.Parse<SessionAnalysisMode>(SelectedMode.ToString()),
                });
                OnPropertyChanged(nameof(CanConnectDevice));
                ConnectDeviceCommand.NotifyCanExecuteChanged();
                Marker1Frequency = Marker2Frequency = null;
                if (sweepChanged) ResetZoom();
                _lastScanDuration = Stopwatch.GetElapsedTime(scanStartedAt);
                OnPropertyChanged(nameof(ScanProgressDisplay));
                OnPropertyChanged(nameof(ScanProgressMaximum));
                MarkResultUnsaved();
                SetStatus("StatusScanComplete", pendingPoints.Count);

                if (!IsContinuousScan || IsHold) break;
                await Task.Delay(TimeSpan.FromMilliseconds(100), _scanCancellation.Token);
                if (!IsContinuousScan || IsHold) break;
            } while (true);
        }
        catch (OperationCanceledException)
        {
            scanCancelled = true;
            IsHold = true;
            SetStatus("StatusScanStopped", pendingPoints?.Count ?? 0);
        }
        catch (Exception exception)
        {
            scanFailed = true;
            IsHold = true;
            ReportException(exception);
            SetStatus("StatusScanFailed");
        }
        finally
        {
            try
            {
                if (connectionAttempted || scanFailed || scanCancelled)
                {
                    using var disconnectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await _device.DisconnectAsync(disconnectTimeout.Token);
                    IsDeviceConnected = false;
                }
            }
            catch (Exception exception)
            {
                if (!scanFailed)
                {
                    ReportException(exception);
                    SetStatus("StatusDisconnectFailed");
                }
            }
            try
            {
                _scanCancellation?.Dispose();
                _scanCancellation = null;
                IsScanning = false;
            }
            finally
            {
                scanStopped.TrySetResult();
                if (ReferenceEquals(_scanStopped, scanStopped)) _scanStopped = null;
            }
        }
    }

    private void ValidateCompletedSweep(SweepSettings settings, IReadOnlyList<MeasurementPoint> points)
    {
        if (points.Count != settings.PointCount)
            throw LocalizedErrors.InvalidData("SessionSampleCountMismatch");

        for (var index = 0; index < points.Count; index++)
        {
            if (points[index].FrequencyHz != _device.GetSampleFrequencyHz(settings, index))
                throw LocalizedErrors.InvalidData("SessionPointFrequencyMismatch", index);
            if (!Enum.IsDefined(points[index].Quality))
                throw LocalizedErrors.InvalidData("SessionPointQualityUnknown", index);
        }
    }

    private SweepSettings CreateSweepSettings()
    {
        double Parse(string value, string name)
        {
            if ((!double.TryParse(value, NumberStyles.Float, Localizer.Culture, out var result) &&
                 !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) || !double.IsFinite(result))
                throw LocalizedErrors.Argument(null, "FieldInvalid", name);
            return result;
        }

        var start = Parse(StartMHz, Text.Get("StartFieldName"));
        var stop = Parse(StopMHz, Text.Get("StopFieldName"));
        if (!int.TryParse(PointCount, NumberStyles.Integer, Localizer.Culture, out var count))
            throw LocalizedErrors.Argument(null, "PointCountInteger");
        return new SweepSettings((long)Math.Round(start * 1_000_000), (long)Math.Round(stop * 1_000_000), count);
    }

    private static string FormatMarker(long? frequency) => frequency is null
        ? "—"
        : Text.Format("MarkerFrequencyFormat", frequency.Value / 1_000_000.0);
}












