# Continuous scan and Hold review

- Added optional repeated scans over the existing `IVnaDevice.SweepAsync` contract; the default remains one scan. The source is connected once for the loop and disconnected on exit with a two-second cancellation token.
- Each sweep is validated and committed atomically. Hold stops after the current complete sweep; Stop cancels an in-flight sweep, which leaves the prior complete result intact.
- Added localized Repeat/Hold controls and state-specific guidance. A short cancellable pause separates repeated acquisitions.
- `ContinuousSweepCommitsEachCompleteResultAndHoldStopsAfterCurrentSweep` checks that two complete sweeps commit and Hold stops before a third.
- Hardware timeout, partial-frame recovery and reconnect remain unimplemented until a verified device driver exists.
