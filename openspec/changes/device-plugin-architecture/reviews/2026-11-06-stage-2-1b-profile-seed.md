# Review: implementation stage 2.1b — plugin-owned built-in profile

Reviewer: `/root/frequency_grid_review` (subagent)
Date: 2026-11-06
Result: **PASS after documentation correction**

The static `DeviceProfile.Vna65MHz` seed was removed from Core and moved to `Vna.Device.Vna65MHz.Vna65MHzDeviceProfile.Default`. Its values and the INI factory's exact-profile criteria are preserved. Source consumers now use the plugin-owned profile, including legacy-session migration through a stable profile ID constant. Remaining protocol-specific fields are still on the Core compatibility type and remain tracked under tasks 2.2/2.3.

The reviewer requested one correction because the design context still placed the profile in Core. That text now distinguishes the Core compatibility type from the plugin-owned VNA65 data. No tests or build were run.
