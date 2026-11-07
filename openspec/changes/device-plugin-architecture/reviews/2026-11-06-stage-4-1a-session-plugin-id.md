# Review: implementation stage 4.1a — session plugin identity

Reviewer: `/root/frequency_grid_review` (subagent)
Date: 2026-11-06
Result: **PASS after migration correction**

Session format v5 adds an optional `PluginId`. New hardware scans carry the source ID, and session playback forwards/preserves it. The v5 loader accepts versions 1–4 and migrates only older sessions whose profile is `iw3hev-65mhz`, source is `hardware`, and ID is absent to `legacy-vna65`. Unknown/demo/imported v5 sessions keep `PluginId = null`; the importer does not infer an ID from `hardware` alone for v5.

The reviewer initially found that the old current format v4 could be misidentified. The format was bumped to v5, the migration was limited to v1–v4, and the current-version validation list was updated. The existing format-version assertion was updated to reference `CurrentFormatVersion`; no tests/build were run.
