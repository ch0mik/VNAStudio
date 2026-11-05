# Serial port access diagnostics

The user can open COM9 in PuTTY and observes the VNA TX LED when typing at 9600/8N1. This establishes access from PuTTY and outgoing USB traffic, but does not establish the firmware baud rate or a measurement response. A failed `mode COM9` query must not be interpreted alone as proof of a missing driver or disconnected device; a port can be occupied by another application.

Inspection of the local original `analyser306b.exe` found the little-endian integer 115200 at file offset `0xFC32`, within the persisted `MSComm1` control data. The existing 115200 default is retained. The reference executable remains outside VnaStudio.

Changes:

- Report serial access denial with the port name, advice to close PuTTY/other analysers, and the original system details in both languages.
- Preserve the original exception as the inner exception and retain the UnauthorizedAccessException type.
- Describe a successful connection as an opened serial port; only a subsequent valid scan establishes measurement communication.

Review: cancellation handling still takes precedence over access-denial translation. The failure paths dispose the attempted port. EN/PL resources have matching keys and format arguments. No terminal was closed or device commands sent during this change.

Validation: Release build through the test project succeeded. The relevant localization and serial acquisition tests passed: 20/20. These tests use a fake transport and do not establish physical device compatibility.
