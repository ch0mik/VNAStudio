# Stage 4.4b review — legacy migration and playback without the source plugin

## Review result

**PASS.** The v4 fixture is serialized directly with its legacy format version and no plugin ID; `MeasurementSessionFile.LoadAsync` performs the migration. The test verifies the `legacy-vna65` compatibility ID, creation of a neutral profile descriptor snapshot, and preservation of the saved points. A separate current-format session with an uninstalled plugin ID survives save/load and plays through `SessionPlaybackDevice`, without connecting the demo or hardware source.

The independent subagent review confirmed the fixture exercises loading-time migration and the current-format case preserves unknown plugin identity during playback.

## Verification

- `dotnet test tests/Vna.Tests/Vna.Tests.csproj --no-restore --filter "FullyQualifiedName~DeviceSourceSelectionTests"` passed: 13 passed, 0 failed.
- Tests use serialized in-memory sessions and fake sources; no COM port or WOB connection is used.
- No source plugin for `vendor.removed-vna` is registered or resolved.

## Scope boundary

The tests establish session migration and host-side playback behavior. They do not claim that the absent plugin can communicate with hardware.
