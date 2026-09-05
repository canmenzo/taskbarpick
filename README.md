# 🖥️ Taskbar Picker

Windows 11 gives you two choices: taskbar on **one** display, or on **all** of them.

Got three monitors and want the taskbar on just two? Tough luck. That's what this fixes ✨

![Taskbar Picker](docs/dialog.png)

## 📦 Get it running

1. Download or clone this repo
2. Run `bin\taskbarpick.exe`

That's it. No installer, no runtime to download, nothing to configure. It's a single 20KB app 🪶

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

## 🔧 Build it yourself

```
powershell -File build.ps1
```

Uses the C# compiler already in Windows, so there's no SDK to install. Output lands in `bin\`.

---

No hooks, no injection, no background services, nothing loaded into other programs 🛡️
