# DDS sample grid review

Date: 2026-11-04.

## Finding

The serial protocol uses `step = (stop - start) / pointCount`, so the final DDS tuning word is below the requested stop. The view model had validated every source against `SweepSettings.FrequencyAt`, an inclusive-endpoint grid. A complete and well-formed hardware sweep would therefore have been rejected at commit time.

## Change

- Added `IVnaDevice.GetSampleFrequencyHz` with an inclusive-range default for demonstration and session playback.
- Routed the query to the selected source and implemented the serial device grid from the same DDS words used by the protocol.
- The view model validates each sample against the connected source's expected frequency before atomic commit.
- Added a headless integration test using the real serial driver and a fake transport. It confirms a full three-point scan from 1 to 4 MHz commits samples at 1, 2 and 3 MHz.

## Verification

- `dotnet test --no-restore -p:UsedAvaloniaProducts=`: 120 passed.
- No physical hardware was used; this test verifies software consistency between DDS command generation, sample frequency mapping and view-model validation.
- NuGet vulnerability metadata could not be reached (NU1900); the cached build and tests succeeded.
