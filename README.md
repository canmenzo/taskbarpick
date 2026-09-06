# 🖥️ Taskbar Picker

Windows 11 only lets you put the taskbar on **one** display or on **all** of them.

Got three monitors and want the taskbar on just two? 🤷 That's what this fixes.

![Taskbar Picker hiding a taskbar](docs/demo.gif)

## 🚀 Get started

1. ⬇️ Green **Code** button → **Download ZIP**, then unzip it
2. 🔨 Double-click **`build.cmd`** (takes a second)
3. ▶️ Run **`bin\taskbarpick.exe`**

Nothing to install. No SDK, no runtime, no downloads. Step 2 uses the compiler already built
into Windows and gives you one 24KB app. 🆓

## ✅ Use it

Tick the displays that should have a taskbar → **Apply**. That's it.

- 🔢 **Identify Displays** flashes a big number on each screen so you know which is which
- 🔁 **Start with Windows** brings it back after a reboot
- 📌 It lives in the system tray. Double-click the icon to change your picks
- 🚪 Tray menu → **Exit** puts every taskbar back

The first time you change a non-primary display, Windows needs Explorer restarted. The app asks
first, it takes a second, and open File Explorer windows will close.

## 🖼️ Full-screen windows

**Stretch maximized windows over the leftover strip** fills the 48px of wallpaper Windows leaves
behind when a taskbar is hidden. Downside: those windows stop counting as "maximized", so the
restore button won't snap them back. Untick it to go back to normal.

## 🆘 Taskbar missing and the app is gone?

If the app gets killed instead of exited, the taskbar stays hidden and there's no tray icon left.
Either way back works:

- Run `taskbarpick.exe` again → tray menu → **Exit**
- Or Ctrl+Shift+Esc → **Windows Explorer** → **Restart**

## 🔒 Safe by design

No hooks, no injection, no background services, nothing loaded into other programs.

MIT licensed, see [LICENSE](LICENSE). 💙 Built for Windows 11.
