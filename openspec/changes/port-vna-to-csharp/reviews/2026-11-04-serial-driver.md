# Serial driver stage review

Date: 2026-11-04.

## Reviewed

- Added `MiniVnaSerialDevice` behind the existing `IVnaDevice` and `ISerialTransport` contracts. It uses typed serial options, asynchronous calls, cancellation tokens, one-operation-at-a-time gating, exact-length frame reads and a configured read timeout.
- Bound `PORTSETTING.VNA_type` from `analyz.ini` into the device profile. Type 0 selects four bytes per sample; type 1 selects six. The application asset now explicitly matches the user's supplied type 1 profile.
- Added fake-transport coverage for command formatting, 4/6-byte framing, frequency generation, ADC conversion, cancellation cleanup and preservation of the user-supplied port path.
- Added `docs/DEVICE-PROTOCOL.md` to separate upstream/source evidence, local binary evidence and unverified hardware behavior.

## Findings and disposition

1. **The PA7N client and the local type-1 binary use different response lengths.** Preserved as profile-selected 4/6-byte frame lengths; the extra two bytes are consumed but not assigned a guessed meaning.
2. **DDS sweep end semantics differ from an inclusive UI range.** The driver follows the recovered client divisor of point count and reports actual frequencies from the transmitted DDS words. The last point therefore falls below Stop, as in the reference client.
3. **Partial frame recovery cannot be trusted without a synchronization marker.** On timeout, I/O failure or cancellation during acquisition, the transport closes; the next sweep requires a fresh connection.
4. **No user-interface source switching or physical hardware verification is included in this stage.** The host still selects the demo device. Those remain explicit follow-up work, so this driver is not presented as a completed hardware port.
5. **The third type-1 channel is not understood.** The driver only consumes its bytes to maintain framing and does not expose or apply them.

## Verification

- `dotnet test --no-restore -p:UsedAvaloniaProducts=`: 114 passed.
- NuGet vulnerability metadata could not be fetched because this environment has no access to `api.nuget.org`; restore completed from cached packages. The Avalonia property disables its telemetry build target because the sandbox cannot write to the user's AppData location.

No remaining code defect was identified in this isolated driver stage. The limitations above are compatibility evidence gaps, not claims of verified hardware support.
