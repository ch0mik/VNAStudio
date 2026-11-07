# Stage 3.4 review — source errors and lifecycle

## Review result

**PASS.** Existing headless coverage exercises connection and sweep failures without falling back to demo, source selection and the connected-source boundary. New cases block a fake sweep, stop it through the ViewModel command, and verify cancellation plus one disconnect. A separate case disposes the ViewModel during a blocked scan, waits for the scan to end, then disposes the router and verifies that all registered sources are disposed once.

The playback display assertion now checks the loaded session's source label, matching the ViewModel's `SESSION · <source>` display for a loaded session. The subagent review found no remaining concerns.

## Verification

- `dotnet test tests/Vna.Tests/Vna.Tests.csproj --no-restore --filter "FullyQualifiedName~DeviceSourceSelectionTests"` passed: 11 passed, 0 failed.
- The headless tests use fake sources and do not open a physical COM port or connect to WOB.
- `csharp-ls --diagnose --solution Vna.slnx` had loaded the solution successfully earlier in this turn.

## Scope boundary

This verifies host cancellation and resource lifecycle using test doubles. It does not verify a device firmware cancel command or physical transport recovery on the user's hardware.
