# Device profile dependencies

This inventory reflects the current source tree and identifies the dependencies to remove or preserve while migrating VNA 1–65 MHz into a device plugin.

## Project dependency map

| Project | References | Current responsibilities |
| --- | --- | --- |
| `Vna.Localization` | None | EN/PL resources and localized error text. |
| `Vna.Core` | `Vna.Localization` | Sweep/session/sample domain types, RF calculations, transitional `DeviceProfile`, and transport-neutral plugin contracts. `DeviceProfileDescriptor` also carries optional neutral frequency presets. |
| `Vna.Devices.Transport` | `Vna.Localization`, `System.IO.Ports` | Neutral serial byte-stream transport and OS-native port catalog. It does not define device commands or frames. |
| `Vna.Devices` | `Vna.Core`, `Vna.Localization`, `Vna.Devices.Transport` | Shared demo, playback, source router, neutral-descriptor-to-legacy-UI compatibility bridge, and the temporary adapter from `IMeasurementSource` to the existing UI-facing `IVnaDevice` contract. Device protocol implementation no longer lives here. |
| `Vna.Device.Vna65MHz` | `Vna.Core`, `Vna.Localization`, `Vna.Devices.Transport`, configuration abstractions, options | VNA 1–65 MHz plugin descriptor/source, `Vna65MHzProtocolSettings`, `Vna65MHzRfConverter`, DDS command generation, serial frame parsing, typed serial options, built-in compatibility profile, and legacy INI profile factory. |
| `Vna.IO` | `Vna.Core`, `Vna.Localization` | App settings, CSV, and session JSON. Session format v6 writes the neutral profile descriptor for new sessions and retains support for legacy `DeviceProfile` snapshots. |
| `Vna.App` | `Vna.Core`, `Vna.Localization`, `Vna.Devices`, `Vna.Devices.Transport`, `Vna.IO`, `Vna.Device.Vna65MHz`, Avalonia and hosting packages | UI, plotting, startup DI composition, validated plugin/profile selection, user settings, and analysis flows. `App.axaml.cs` explicitly registers approved plugins, resolves `DeviceSelection` against the catalog, and adapts the selected source for the current router/UI flow. |

The runtime now explicitly registers `Vna.Device.Vna65MHz` and creates its source through `IDevicePlugin`; a small compatibility adapter keeps the existing ViewModel/router API in place while those consumers migrate. The neutral serial transport is a separate `Vna.Devices.Transport` dependency and preserves the system port path and async/cancellable byte-stream API.

The Help → Registered device profiles window reads `DevicePluginCatalog.Descriptors` and lists every registered plugin/profile plus the neutral range, reference impedance, capability availability and evidence declarations. The active plugin/profile is selected at application startup from the separate `DeviceSelection` settings section; the catalog window remains informational and does not hot-switch an open source. Invalid and ambiguous selections are rejected instead of falling back to Demo.

## `DeviceProfile` field usage

| Field | Current consumers | Migration destination |
| --- | --- | --- |
| `Id` | App session creation/load checks; router source agreement; playback; active CSV import uses the selected neutral descriptor; legacy CSV/session compatibility still preserves the legacy snapshot | Neutral profile descriptor and session profile ID. |
| `DisplayName` | UI profile label; session snapshot validation | Neutral profile descriptor. |
| `MinimumFrequencyHz`, `MaximumFrequencyHz` | Core sweep and band validation; App scan fields, zoom, marker bounds; session validation | Neutral profile descriptor. |
| `DefaultPointCount` | App initial point count and progress display; session validation | Neutral profile descriptor. |
| `ReferenceImpedanceOhms` | RF conversion; Smith chart; session validation; App compatibility fallback only when no descriptor exists; CSV import stores the active descriptor directly | Neutral profile descriptor / session snapshot; a present descriptor is authoritative, including a null value. |
| `PlotColors` | App palette fallback only when no presentation descriptor exists; session validation | Neutral presentation metadata, separate from protocol settings. |
| `Capabilities`, `Connectors` | Profile construction and session snapshot validation; Help catalog and hardware-scan preflight consume neutral capability descriptors | Neutral profile descriptor. |
| `DdsClockHz` | Serial DDS words/frequency calculation; session validation; exact-profile match in INI factory | VNA 1–65 MHz plugin option. |
| `DdsPllPrescaler` | `Vna.Device.Vna65MHz` INI factory assignment, session validation, and exact-profile match; current serial implementation does not read this property | Keep in plugin options only if exact-variant source/measurement evidence shows it affects operation; otherwise retain as legacy metadata during migration. |
| `StartPauseMilliseconds`, `StepPauseMilliseconds` | Serial sweep timing; session validation | VNA 1–65 MHz plugin options. |
| `LegacyVnaType` | Plugin serial range check and `Vna65MHzRfConverter` magnitude polarity/offset; INI factory validation and exact-profile match; carried in session profile snapshots (not currently validated by the session reader) | VNA 1–65 MHz plugin options and calculation implementation. |
| `LegacySvna` | Serial bytes-per-sample choice; `Vna.Device.Vna65MHz` INI factory validation and exact-profile match; carried in session profile snapshots (not currently validated by the session reader) | VNA 1–65 MHz plugin options. |
| `MagnitudeAdcDbPerCount`, `PhaseAdcDegreesPerCount` | `Vna65MHzRfConverter`; legacy session validation; plugin-provided processing snapshot | VNA 1–65 MHz plugin profile/calibration input. |
| `LegacyCalibration` | `Vna65MHzRfConverter` affine conversion/mode offset; legacy session validation; plugin-provided processing snapshot | VNA 1–65 MHz plugin profile/calibration input, with evidence state preserved separately. |
| `GeneratorMemoryFrequenciesHz` | `Vna.Device.Vna65MHz` compatibility profile/INI mapping; App fallback only when descriptor presets are absent; legacy session serialization/validation | Neutral `DeviceProfileDescriptor.FrequencyPresetsHz` consumed by the app. These values do not enable Generator output. |
| `LegacyBandPresets` | App band-selector fallback only when no presentation descriptor exists; `Vna.Device.Vna65MHz` INI parsing; legacy session validation | Neutral preset/presentation data in `DeviceProfileDescriptor.Presentation`. |

## Migration notes

- Core still contains the legacy profile shape to read, validate, and preserve older session snapshots. Runtime protocol settings, profile presentation, and new-session metadata are plugin-owned or neutral descriptors. The built-in VNA 1–65 MHz profile seed, INI option model, profile construction/validation, DDS commands, framing, timing, ADC byte decoding, ADC-to-RF conversion, and SPECIAL_CAL affine conversion now live in `Vna.Device.Vna65MHz`. Generic reflection-coefficient-to-impedance math remains in Core. Retaining the legacy session DTO does not reinterpret saved samples.
- `LegacyAnalyzIniProfileFactory` now lives in `Vna.Device.Vna65MHz`; it attaches the known capability and connector descriptors only when the key profile and protocol configuration match the built-in profile. Other INI profiles receive no inferred capabilities or connector roles.
- `MeasurementSession.ProfileDescriptorSnapshot` is the neutral v6 metadata snapshot; migration synthesizes it from an available legacy `ProfileSnapshot`. New ViewModel sessions write the neutral descriptor and no legacy profile. The old full `DeviceProfile` snapshot remains readable and is preserved when an older session is re-saved. Playback uses the saved sweep and metadata without resolving the source plugin; hardware acquisition still requires the active profile to match.
- `MeasurementCalibrationSnapshot` is now assembled from host display settings plus an optional neutral adjustment snapshot reported by the active source. It no longer reads protocol coefficients from `DeviceProfile`; an unreported hardware conversion stays unknown.
- DDS words, serial commands/frame layout, pauses, ADC byte decoding, and VNA65-specific ADC-to-RF conversion now live behind the plugin. `RfSampleCalculator` retains only device-neutral reflection-coefficient-to-impedance conversion. `DeviceProfile` still carries legacy fields for settings/session compatibility, but new runtime data and processing provenance use plugin-owned options and neutral descriptors.
- `Vna65MHzMeasurementSource` and `Vna65MHzRfConverter` now consume plugin-owned `Vna65MHzProtocolSettings` and a neutral `DeviceProfileDescriptor`, not Core's `DeviceProfile`. Generator preset frequencies are exposed to the app through `DeviceProfileDescriptor.FrequencyPresetsHz`; the explicit profile bridge remains for the transitional UI API and legacy INI/session compatibility. Active CSV import uses the neutral descriptor and never promotes its compatibility fallback impedance into session metadata.
- `DeviceProfileCompatibilityFactory` builds a temporary UI-facing `DeviceProfile` from the selected neutral descriptor. Its legacy protocol/calibration fields are not acquisition settings; selected plugin source construction and operation remain authoritative. Cable velocity factor is left unset when neither the user nor the selected descriptor supplies it.
- Reflection calibration provenance is now reported by the measurement source through a neutral adjustment snapshot. VNA65 reports the configured values it actually applies as `ConfiguredUnverified`; other plugins that cannot report processing leave provenance `Unknown`. When a profile descriptor exists, its nullable `Zref` remains authoritative in live charts and display correction.
