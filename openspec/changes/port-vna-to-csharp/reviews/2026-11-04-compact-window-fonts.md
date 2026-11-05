# Compact window and typography review

## Changes reviewed

- Set the resizable main window minimum to 1024×768 DIP and asserted both limits in an Avalonia test.
- Reduced interface font sizes by roughly 10%, including the dynamic plot labels and About heading.
- Added an English minimum-size preview and updated the bilingual README and OpenSpec design notes.
- Updated the resize/DPI regression matrix to start at 1024×768.

## Verification

- Full Release test suite: 126 passed, 0 failed.
- `openspec validate port-vna-to-csharp --strict`: valid.
- Rendered and visually inspected `docs/images/themes/minimum-light.png`; the plot, marker tables, and scan controls fit at 1024×768, with the settings panel scrolling vertically.

## Review note

The first full-suite run exposed an existing Avalonia test-thread affinity issue in `LegacyBandRangesOverrideShortcutsAndOutOfProfileBandsAreHidden`. Marking this UI-bound test as an `AvaloniaFact` kept its view-model localization updates on the UI dispatcher; the complete suite then passed.
