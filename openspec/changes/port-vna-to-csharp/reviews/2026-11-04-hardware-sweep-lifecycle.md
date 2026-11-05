# Hardware sweep lifecycle review

Date: 2026-11-04.

## Reviewed

- A DDS sweep now publishes the frequency grid derived from the exact start/step tuning words sent to the device. The view model validates against that grid, including the legacy last point below Stop.
- The serial port closes when a sweep fails, is cancelled after a complete frame read, or its async iterator is disposed before completion. The operation gate is released even if closing the transport fails.
- The view-model integration test proves that a full hardware frame from the fake transport commits successfully as a completed scan.

## Verification

- `dotnet test --no-restore -p:UsedAvaloniaProducts=`: 121 passed.
- Coverage includes the non-inclusive DDS final point, view-model atomic commit, cancellation while reading, and cancellation after the first yielded point.
- No physical VNA was connected. These checks prove lifecycle and software consistency, not electrical or calibration accuracy.
