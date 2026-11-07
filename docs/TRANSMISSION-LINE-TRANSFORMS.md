# Transmission-line transforms: design and gates

**Status: design only.** VNA Studio does not currently embed or de-embed transmission lines.

## Eligible input

The transform operates on complex `NetworkDataSet` samples from Touchstone S1P/S2P or a future plugin that explicitly supplies complex S-parameters with known interpretation. This is necessary, but not sufficient, for a physical correction. Scalar FILTER magnitude is not complex S21. Reflection-derived VNA samples with an unconfirmed phase/reactance sign are not sufficient to construct complex S11.

`NetworkDataSet` currently does not store calibration status. A future UI/transform input must therefore carry explicit calibration/correction provenance for both the DUT/source dataset and any measured fixture dataset; the application must not infer calibration from a file extension or the presence of complex values. The transform does not substitute for measurement calibration. A source and measured fixture explicitly marked calibrated/corrected may produce a corrected physical result only when both calibration records and the line model are valid for the same ports, reference planes, and frequency range. An uncalibrated operand may be used only for an explicitly experimental mathematical result, and an unknown calibration state must remain visibly unknown; neither may be presented as calibrated hardware correction. Keysight likewise describes reference-plane adjustment as a post-processing step on corrected measurement data in its [reference-plane adjustment overview](https://helpfiles.keysight.com/csg/N1930xB/ToolsAndUtilities/Removing_Unwanted_Effects_from_the_Measurement.html).

Every network dataset carries its own finite, positive port reference impedance `Zref`. A line model separately carries its characteristic impedance `Zc` and propagation data. `Zc` must never be silently replaced with `Zref`; any impedance renormalization must be explicit.

## Line model

The first implementation should accept either:

- a measured two-port line/fixture Touchstone dataset, with a declared port direction and compatible frequency samples; or
- an explicitly selected uniform-line model with physical length and a documented propagation/attenuation model.

An ideal port extension can represent only a matched uniform line with the assumptions of constant impedance, linear phase, and flat magnitude. A mismatch or loss correction needs an accurate fixture model or measured fixture S-parameters. The UI must identify which model is being used and must not label a simple phase extension as general de-embedding. See Keysight's [reference-plane adjustment overview](https://helpfiles.keysight.com/csg/N1930xB/ToolsAndUtilities/Removing_Unwanted_Effects_from_the_Measurement.html) and its [uniform-line S-matrix relation](https://edadownload.software.keysight.com/eedl/ads/2012_08/pdf/em.pdf).

## Transform contract

- Provide explicit **add line** and **remove line** operations, with per-port direction and length.
- For measured fixtures, use network cascade/de-cascade with the complete complex fixture model; do not reduce the operation to a phase-only rotation when fixture mismatch or loss is modeled.
- Preserve the source dataset and its sample grid. A derived dataset is a new immutable value. Do not silently interpolate between mismatched fixture and measurement grids.
- Keep output `Zref` explicit and separate from line `Zc`.
- Reject non-finite model values, incompatible port counts, incompatible frequency grids, and singular/ill-conditioned points with a localized actionable error.

## Provenance

Each derived result must identify the source dataset and its content hash when available, source calibration/correction state and provenance, and—when used—the measured fixture dataset, its content hash, and its calibration/correction state and provenance. It must also retain transform direction, port mapping, line model/version, `Zc`, `Zref`, length, propagation/attenuation inputs, and capture/creation time. The derived record must distinguish a calibrated physical correction from an experimental mathematical transform. Exporting to Touchstone must retain the derived dataset's `Zref`; source and fixture datasets remain available unchanged.

## Validation gate

Before exposing a transform in the UI, define immutable input/output vectors from an independent network-analysis reference and test both add/remove directions on lossless matched lines, mismatched `Zc`/`Zref`, lossy lines, and singular cases. Then compare measured-fixture de-embedding against an independent fixture or calibrated reference, and verify that the DUT and fixture calibration provenance is retained end-to-end. Until these checks exist, the design remains documented but no transform is claimed as implemented or validated for the VNA 1–65 MHz hardware.

Related implementation: [Touchstone import/export](TOUCHSTONE.md), [complex network data](../src/Vna.Core/NetworkDataSet.cs).
