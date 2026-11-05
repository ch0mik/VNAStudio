# Review F03 — File and About menus

Date: 2026-11-04.

- Added localized File/Plik menu actions for Open Session, Save Session and Export CSV, plus Help/Pomoc → About.
- The About dialog shows VNA Studio · SQ7MRU, the assembly version and the verified-protocol status. Its theme variant follows the main window.
- The existing sidebar file buttons remain available as direct actions.
- Verification: full Release suite, 65 passed, 0 failed; XAML compiles and the app headless window tests pass.
- Added a sidebar indicator for the current session name or an unsaved-result label. Successful save/open updates the name; starting a new scan clears the old filename and a completed scan marks the new result unsaved.

