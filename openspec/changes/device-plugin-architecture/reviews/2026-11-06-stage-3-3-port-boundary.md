# Review: system port and plugin protocol boundary

## Scope

Confirmed the OS-specific COM/tty path is kept as application configuration and passed through the adapter to the VNA65 plugin and serial transport. Confirmed protocol settings are supplied separately by plugin-owned options.

## Review outcome

PASS. `AppSettings.PortName` is provided to `DeviceConnectionOptions`; the compatibility adapter forwards it as `DeviceConnectionRequest.PortName`; `Vna65MHzMeasurementSource` puts that same value into `SerialConnectionSettings`; and `SerialPortTransport` passes it directly to `SerialPort`. No path normalization or profile-derived port default is applied. VNA65 protocol and serial options are registered independently of the system port setting.

## Verification

- Reviewer subagent: PASS.
- No build, tests, or physical WOB connection were used.
