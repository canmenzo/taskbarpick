# 🖥️ Taskbar Picker

Windows 11 gives you two choices: taskbar on **one** display, or on **all** of them.

Got three monitors and want the taskbar on just two? Tough luck. That's what this fixes ✨

![Taskbar Picker](docs/dialog.png)

## 📦 Get it running

1. Download this repo (green **Code** button → **Download ZIP**) and unzip it
2. Double-click **`build.cmd`** — takes about a second
3. Run **`bin\taskbarpick.exe`**

No installer and nothing to download beforehand: step 2 uses the C# compiler that is already
part of Windows, so there is no SDK, no runtime, no dependencies. You end up with one 20KB app 🪶

## 🎛️ Using it

Tick the displays that should have a taskbar, click **Apply**. Done.

- 🔍 **Identify Displays** flashes a big number on each screen, so you know which is which
- 🚀 **Start with Windows** brings it back automatically after a reboot
- 📌 It lives in the system tray. Double-click the icon to change your picks
- 👋 Tray menu → **Exit** puts every taskbar back exactly as it was

The very first time you turn a taskbar on or off for a non-primary display, Windows needs
Explorer restarted to do it. The app asks first, and it takes about a second 🔄

## 🤔 That last checkbox

**Stretch maximized windows over the leftover strip**

When a taskbar is hidden, Windows still holds onto its 48px slice of that screen, so maximized
windows stop just short of the bottom and you see a wallpaper stripe 😒

Leave this ticked and windows maximized on those displays get stretched over the stripe, using
the whole screen. The catch: such a window stops counting as "maximized", so its restore button
won't snap it back to its old size. Untick it and everything goes back to normal 👍

---

No hooks, no injection, no background services, nothing loaded into other programs 🛡️
