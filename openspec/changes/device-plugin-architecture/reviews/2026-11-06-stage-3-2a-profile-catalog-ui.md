# Stage 3.2a review — descriptor-driven profile catalog UI

**Result:** PASS

## Implemented

- The Help menu opens a resizable EN/PL device-profile catalog sourced from the validated `DevicePluginCatalog` snapshot.
- Each registered plugin/profile lists its declared frequency range, default point count, reference impedance, capabilities, availability and evidence level.
- Known capability IDs use localized user-facing labels; unknown IDs remain visible as declared by the plugin.
- The dialog states that it is informational and does not select or connect a device.
- Host-side scan cancellation is classified as an application tool, not a device command; its evidence note explicitly states that no cancel command is sent to the hardware.
- README and profile dependency documentation describe the informational catalog.

## Review

Subagent review passed. It checked catalog wiring, resizable dialog behavior, EN/PL summary formatting, capability status/evidence presentation, and the scan-cancellation classification. No findings.

## Verification

- `csharp-ls --diagnose --solution Vna.slnx` loaded the solution.
- XAML and both RESX files parsed as XML; EN/PL resource keys matched.
- No tests, builds, or device connections were used.
