# English/Dark defaults and remembered preferences

New/missing preference fields default to en-US and dark. The initial localization service, language normalization, theme normalization and application theme agree with those defaults. Explicit saved PL/light choices continue to load.

The application host enables settings persistence on its MainWindow. A normal close first waits for an asynchronous atomic AppSettingsFile write and then posts the final Close to the dispatcher. A shared semaphore serializes manual Save and close-time Save, preserving request order. The closing snapshot includes the supported preferences already represented by AppSettings, including language, theme, port and plot preferences; measurement sessions remain separately saved files.

While saving, repeated close requests are cancelled and the window is disabled. The lifetime token remains active until the final close. If saving fails, the error remains visible and the window is enabled again; the next close can exit. Headless windows opt out by default, while dedicated integration tests opt in using temporary paths.

Preferences are read at startup (the user JSON provider has reloadOnChange=false). This avoids delayed notifications from the application's own Save overwriting newer unsaved UI edits. Typed options and their monitor remain in use; manual external edits take effect on the next application start.

Validation: 136/136 Debug tests passed. Integration coverage exercises EN/dark startup, selecting PL/light, closing to a real JSON file, rebinding saved settings on the next startup and preserving COM9/other preferences. An unwritable destination test verifies visible errors and a subsequent successful close. Existing Polish measurement fixtures now specify their culture explicitly. The application was rebuilt in Debug.

An independent subagent found no blocking error in the async closing/defaults flow and identified the existing file-reload race; startup-only loading resolves that race in the application host. README translations and theme documentation describe the save timing and defaults.
