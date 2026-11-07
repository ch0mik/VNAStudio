# Device plugin contract

VNA Studio uses statically registered device plugins. The host talks to each plugin through the model-neutral contracts in `Vna.Core`; a plugin owns its device commands, serial framing, transport configuration, sample decoding and device-specific conversion.

## Contract API compatibility

Plugin API version is a `(Major, Minor)` pair independent of the plugin's own release version. The current host contract is **1.0**.

- The major version must match exactly. A breaking contract change requires a major increment.
- A plugin minor version must be non-negative and no greater than the host's supported minor version. Compatible additive contract changes increment the minor version.
- Negative versions, a different major, or a plugin minor newer than the host are rejected during catalog registration. The app stops with a diagnostic rather than trying to run an incompatible plugin.

The policy is implemented by [`DevicePluginContract`](../src/Vna.Core/DevicePluginContracts.cs) and enforced by [`DevicePluginCatalog`](../src/Vna.Core/DevicePluginCatalog.cs).

## Plugin and profile responsibilities

An `IDevicePlugin` declares a stable plugin ID, its display name, API version and supported `DeviceProfileDescriptor` entries. Plugin IDs must be non-empty and unique across the catalog without regard to letter case. Profile IDs must be non-empty and unique within their plugin. A profile descriptor declares its stable ID and schema version, frequency limits, default point count, optional reference impedance, connectors, capabilities and presentation hints. Protocol values and serial framing do not belong in the descriptor.

`CreateSource(profile)` must accept only a profile declared by that plugin and return an `IMeasurementSource`. The source owns protocol behavior. Connection, disconnection and sweep operations are asynchronous and accept a `CancellationToken`; the host passes an OS-specific port name unchanged in `DeviceConnectionRequest.PortName`. A missing reference impedance in the neutral descriptor stays unknown in display and saved profile metadata; the transitional UI compatibility profile is not authoritative. A source that cannot report the actual reflection processing it applied leaves `AppliedReflectionCalibration` null so saved provenance remains unknown.

Capabilities describe analysis modes, device commands or host-side tools. Keep `Availability` separate from `Evidence`: for example, `Partial` is not proof of physical accuracy, and `Blocked` must not send a hardware command. Connector labels and electrical roles are separate so an observed label does not imply a verified signal path.

## Registration and selection

Plugins are registered explicitly in the app's composition root. The host does not scan writable folders, load arbitrary assemblies, or install plugins from the network. Adding a plugin therefore requires a reviewed project reference and explicit DI registration, and its assets must be added to release packages.

The active plugin/profile IDs are stored in the `DeviceSelection` section of `settings.json`; see [device selection](DEVICE-SELECTION.md). The Help catalog displays registered descriptors but does not hot-switch a running source. COM/tty paths and the selected measurement source are separate settings.

The VNA 1–65 MHz implementation is in [`Vna.Device.Vna65MHz`](../src/Vna.Device.Vna65MHz/). Shared serial byte I/O is in `Vna.Devices.Transport`; device commands and response framing remain in the plugin. See [the project dependency map](DEVICE-PROFILE-DEPENDENCIES.md) before migrating additional legacy `DeviceProfile` fields.
