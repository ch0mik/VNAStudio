# Generator frequency memories

The profile loads the ten generator frequency entries from the supplied `analyz.ini` MEMORY section. For the 65 MHz profile they are:

| Memory | Frequency |
| --- | ---: |
| M0 | 3.600 MHz |
| M1 | 7.050 MHz |
| M2 | 10.500 MHz |
| M3 | 14.100 MHz |
| M4 | 18.050 MHz |
| M5 | 21.050 MHz |
| M6 | 24.550 MHz |
| M7 | 27.195 MHz |
| M8 | 28.050 MHz |
| M9 | 29.050 MHz |

In Generator mode, choose a memory or edit its frequency inside the active profile range. The supplied hardware manual says the button labeled `OFF` shows the current generator state and clicking it turns the generator on; it also says the second BNC is used as an RF probe in this mode. VNA Studio has not recovered the device command sequence that starts, stops, or tunes this output. Its output control is disabled, it sends no generator command, and the physical output state is unknown. Do not assume this app has turned the output off.

Analysis of the VB6 event `VB6_0042399d` recovered a control-state toggle branch and the `&OFF` button caption; that caption reference alone does not prove a command is sent over COM. The available procedure does not recover a complete, verified sequence for enabling, disabling, and setting output frequency. This is insufficient evidence to implement generator control.
