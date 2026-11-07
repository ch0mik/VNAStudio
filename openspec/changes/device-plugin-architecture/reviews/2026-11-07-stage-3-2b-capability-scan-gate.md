# Stage 3.2b review — capability-gated hardware scans

## Review result

**PASS.** Hardware Start scan is controlled by the active profile descriptor:

- `Available` and `Partial` capabilities allow their declared reflection scan paths.
- `Blocked`, `Unknown`, and missing capabilities disable Start scan for hardware before that command can connect or send a scan request.
- The current VNA65 Filter mode is blocked; Cable Loss remains `Partial` and retains its separate fixture confirmation gate.
- Generator mode does not start a measurement scan. Demo and compatible session playback remain available for supported analysis data.
- Mode changes are rejected and the mode tabs disabled while scanning, preventing a continuous scan from switching into a blocked mode.
- Playback Start/Connect requires a compatible saved session or a complete in-memory sweep.

The explicit **Connect** action can still open the serial connection while a blocked mode is selected. It does not start a scan or send a device measurement command; Start scan remains disabled. This permits an operator to connect and select another supported mode without changing the source.

EN/PL source hints, Filter documentation and README describe the behavior. OpenSpec strict validation and C# LSP solution loading passed. No tests, build, application run, or hardware connection was performed.
