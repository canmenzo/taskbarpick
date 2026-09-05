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

## The leftover strip

A hidden taskbar keeps reserving its 48px strip, and Explorer will not give it back. Measured
on build 26200, none of these work on a secondary display:

- `SPI_SETWORKAREA` reports success and changes nothing (on the primary it does apply, but
  Explorer then re-docks its bar inside the smaller area, and repeating that walks the bar and
  your windows up the screen, so this app never writes work areas).
- `ABM_SETPOS` against Explorer's own bar is ignored.
- Resizing the bar window does not change the reservation.
- The autohide flag and the taskbar size fields in that display's `MMStuckRects3` blob are
  ignored; Windows 11 treats autohide as global (`ABM_SETSTATE` autohides every display at once).

So taskbarpick resizes the windows instead: **Stretch maximized windows over the leftover
strip** watches for a window maximized on a display whose taskbar is hidden, un-maximizes it and
places it over the whole monitor. The shell re-applies the maximized rect over any `SetWindowPos`,
so dropping the maximized state is the only way past it, and the cost is that such a window no
longer counts as maximized: its restore button and Win+Down behave like a normal window. Displays
that still have a taskbar are never touched. Turning the option off, or re-enabling that display's
taskbar, puts the windows back the way they were.

## How it works

- Enumerates displays with `EnumDisplayMonitors` and matches Explorer's `Shell_TrayWnd` /
  `Shell_SecondaryTrayWnd` windows to them with `MonitorFromWindow`.
- Hides the unwanted ones with `ShowWindow(SW_HIDE)`.
- Reapplies on the `TaskbarCreated` broadcast, on display changes, and on a twice-a-second
  watchdog, so an Explorer restart or a resolution change does not bring the bar back.
- Stretches maximized windows via `SetWindowPlacement`, reusing the invisible resize border the
  shell itself used for the maximized rect so the visible edges land exactly on the monitor.

No hooks, no injection, no DLLs loaded into other processes, no keyboard automation, so there
is nothing for anticheat to react to. Config lives in `%LOCALAPPDATA%\taskbarpick\hidden.txt`
and stores only the displays that should stay bare, so a newly attached monitor keeps its
taskbar. Displays are keyed by device interface path, so they survive renumbering.

## Build

Needs no SDK; it uses the C# compiler that ships with Windows.

    powershell -File build.ps1

Output: `bin\taskbarpick.exe` (.NET Framework 4.x, x64).
