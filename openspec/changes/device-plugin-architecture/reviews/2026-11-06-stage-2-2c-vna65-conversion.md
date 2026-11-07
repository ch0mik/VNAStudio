# Review: implementation stage 2.2c — VNA65 ADC/RF conversion

Reviewer: `/root/review_vna65_plugin_stage` (subagent)  
Date: 2026-11-06  
Result: **PASS**

The VNA65-specific legacy ADC conversion and `SPECIAL_CAL` affine conversion moved from Core to `Vna65MHzRfConverter`. Their polarity, offsets, coefficients, scalar-transmission representation, validation, and invalid-sample handling were preserved by inspection. The serial source and existing conversion call sites now use the plugin converter. Core keeps the device-neutral reflection-coefficient-to-impedance calculation and calibration snapshot metadata.

The first review noted that the dependency-map project summary omitted `Vna65MHzRfConverter`; the inventory was corrected. No tests or build were run.
