# Review: implementation stage 1.2 — plugin contract

Reviewer: `/root/frequency_grid_review` (subagent)
Date: 2026-11-06
Result: **PASS**

The Core contract has no dependency on `Vna.Devices`, serial APIs, protocol framing, baud rate, or legacy profile configuration. `IDevicePlugin.CreateSource(profile)` is transport-independent; the source receives a neutral connection request later, and connect/disconnect/sweep operations accept cancellation tokens. Contract compatibility enforces matching positive major versions and rejects plugin minors newer than the host.

The reviewer identified and confirmed a documentation correction: the OpenSpec scenario now describes source creation from a profile followed by passing the connection request to `ConnectAsync`.

No tests or build were run during this stage.
