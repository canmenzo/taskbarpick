# taskbarpick

Windows 11 only lets you put the taskbar on the primary display or on all of them.
taskbarpick lets you pick exactly which displays get one.

![dialog](docs/dialog.png)

## Use

Run `bin\taskbarpick.exe`. Tick the displays that should have a taskbar, hit Apply.
It then sits in the tray; double-click the tray icon to change the selection.

- **Identify** flashes the display number on each screen, matching Settings > Display numbering.
- **Start with Windows** adds an HKCU `Run` entry that launches it minimized (`--silent`).
- **Exit (restore all taskbars)** puts every taskbar back.

The first time you enable a taskbar on a non-primary display, Windows itself has to
create it, so taskbarpick turns on "show taskbar on all displays" and offers to restart
Explorer once. After that it just hides the ones you unticked, including after future
Explorer restarts.

## How it works

- Enumerates displays with `EnumDisplayMonitors` and matches Explorer's `Shell_TrayWnd` /
  `Shell_SecondaryTrayWnd` windows to them with `MonitorFromWindow`.
- Hides unwanted bars with `ShowWindow(SW_HIDE)` and hands the strip back to the desktop
  with `SystemParametersInfo(SPI_SETWORKAREA)`, so maximized windows fill the screen
  instead of leaving a gap.
- Reapplies on the `TaskbarCreated` broadcast, on display changes, and on a 2s watchdog.

No hooks, no injection, no DLLs loaded into other processes, no keyboard automation, so
it has nothing anticheat reacts to. Config lives in `%LOCALAPPDATA%\taskbarpick\hidden.txt`
and stores only the displays that should stay bare, so a newly attached monitor keeps its
taskbar.

## Build

Needs no SDK; it uses the C# compiler that ships with Windows.

    powershell -File build.ps1

Output: `bin\taskbarpick.exe` (.NET Framework 4.x, x64).
