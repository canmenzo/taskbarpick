# taskbarpick

Windows 11 only lets you put the taskbar on the primary display or on all of them.
Taskbar Picker lets you pick exactly which displays get one.

![dialog](docs/dialog.png)

## Use

Run `bin\taskbarpick.exe`. Tick the displays that should have a taskbar, hit Apply.
It then sits in the tray; double-click the tray icon to change the selection.

- **Identify Displays** flashes the display number on each screen, matching the numbering in
  Settings > System > Display.
- **Start with Windows** adds an HKCU `Run` entry that launches it minimized (`--silent`).
- **Exit (Restore All Taskbars)** puts every taskbar back.

Windows itself has to create the taskbars on non-primary displays, so the first Apply that
wants one turns on "show taskbar on all displays" and offers to restart Explorer once. If you
end up wanting the taskbar on the primary display only, Apply turns that setting back off,
which is the native way to do it and leaves nothing behind.

## Known limitation

A hidden taskbar still reserves its strip on that display, so maximized windows there stop
about 48px short of the edge. Explorer owns the work area of a secondary display and there is
no way to hand the strip back from another process: `SPI_SETWORKAREA` reports success and does
nothing on a secondary display, `ABM_SETPOS` on Explorer's own bar is ignored, and resizing the
bar window does not change the reservation. All three were measured on build 26200, not assumed.
On the primary display `SPI_SETWORKAREA` does apply, but Explorer then re-docks its taskbar
inside the smaller area, and repeating that walks the bar and your windows up the screen, so
this app never writes work areas at all.

## How it works

- Enumerates displays with `EnumDisplayMonitors` and matches Explorer's `Shell_TrayWnd` /
  `Shell_SecondaryTrayWnd` windows to them with `MonitorFromWindow`.
- Hides the unwanted ones with `ShowWindow(SW_HIDE)`.
- Reapplies on the `TaskbarCreated` broadcast, on display changes, and on a 2s watchdog, so an
  Explorer restart or a resolution change does not bring the bar back.

No hooks, no injection, no DLLs loaded into other processes, no keyboard automation, so there
is nothing for anticheat to react to. Config lives in `%LOCALAPPDATA%\taskbarpick\hidden.txt`
and stores only the displays that should stay bare, so a newly attached monitor keeps its
taskbar. Displays are keyed by device interface path, so they survive renumbering.

## Build

Needs no SDK; it uses the C# compiler that ships with Windows.

    powershell -File build.ps1

Output: `bin\taskbarpick.exe` (.NET Framework 4.x, x64).
