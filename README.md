# 🎮 Gamepad Calibrator

Professional Windows 10/11 utility for detecting, diagnosing, calibrating, normalizing, remapping, and testing **generic USB / DirectInput** game controllers — including Quantum QHM7468-family pads that appear as `Generic USB Joystick`.

---

## 📋 Table of Contents

- [What This Solves](#what-this-solves)
- [What It Cannot Do](#what-it-cannot-do)
- [Requirements](#requirements)
- [Build & Run](#build--run)
- [Libraries](#libraries)
- [Quick Start](#quick-start)
- [Game Remap](#game-remap)
- [Profiles](#profiles)
- [DirectInput vs XInput](#directinput-vs-xinput)
- [Project Layout](#project-layout)
- [Documentation](#documentation)

---

## ✅ What This Solves

Cheap DirectInput pads often expose sticks as **X / Y / Z / Z Rotation** with badly offset centers (for example X≈40, Y stuck near max). Windows `joy.cpl` is not enough.

This app builds its **own calibration layer**:

- 🔧 Measures real centers (never assumes 128)
- 🔍 Discovers which physical stick moves which axis
- 🎛️ Supports manual remapping, invert, swap, dead zones, sensitivity
- 💾 Saves profiles keyed by **VID+PID+usage**, not display name alone
- 🩺 Distinguishes software mapping issues from likely hardware failures

---

## ❌ What It Cannot Do

- Repair physically broken potentiometers / dead axes
- Press the hardware **ANALOG** button for you
- Silently convert DirectInput into XInput
- Auto-install virtual gamepad kernel drivers

---

## 🖥️ Requirements

| Item | Details |
|------|---------|
| OS | Windows 10 or 11 (laptop or desktop) |
| SDK | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Device | A USB DirectInput/HID gamepad |

---

## 🚀 Build & Run

```powershell
# Clone or navigate to project directory
cd $env:USERPROFILE\GamepadCalibrator

# Restore dependencies
dotnet restore

# Build solution
dotnet build GamepadCalibrator.sln -c Release

# Run tests
dotnet test GamepadCalibrator.sln -c Release
```

### Run the Application

```powershell
# Via CLI
dotnet run --project src\GamepadCalibrator.App -c Release
```

Or launch directly:

```
src\GamepadCalibrator.Appin\Release
et8.0-windows\GamepadCalibrator.App.exe
```

---

## 📦 Libraries

| Package | Purpose | License | Status |
|---------|---------|---------|--------|
| **HidSharp** 2.1.0 | Enumerate HID VID/PID/path/manufacturer | Apache-2.0 | Stable, widely used |
| **CommunityToolkit.Mvvm** | MVVM source generators | MIT | Actively maintained |
| **Microsoft.Extensions.*** | DI + logging | MIT | Supported by Microsoft |
| **winmm** `joyGetPosEx` | Raw axes/buttons/HAT for DirectInput-class pads | OS API | Built into Windows |

> No abandoned DirectInput wrappers are required for core calibration.

---

## ⚡ Quick Start

Follow these steps to calibrate your gamepad:

1. **Refresh** → select your controller from the list
2. **Raw Axes** tab → verify live values are changing
3. Press hardware **ANALOG** button if your pad has one (check LED)
4. **Calibration Wizard → Axis Discovery**
   - Rest all sticks
   - Move left stick
   - Move right stick
5. Correct mapping manually if the discovery got it wrong
6. **Center Calibration** — leave sticks untouched, hands off
7. **Range Calibration** — move sticks in full circles
8. Adjust dead zones on **Live Test** tab
9. **Final Validation** → mark **PASS/FAIL** → **Save Profile**

On reconnect, a matching profile is loaded automatically.

---

## 🎮 Game Remap (Key / Mouse Bindings)

Open the **Game Remap** tab to assign any control on the connected joystick to keyboard keys or mouse actions:

| Control | Action |
|---------|--------|
| Buttons 1–N | Any key / mouse button |
| D-pad directions | Jump, sprint, interact, crouch, etc. |
| Stick axes | Camera mouse look (`MouseMoveX` / `MouseMoveY`) |

### FPS Preset (CrazyGames)

Loads the layout used during setup:

| Button | Action |
|--------|--------|
| 1–4 | WASD movement |
| D-pad | Space / Shift / E / Ctrl |
| 5 | R (reload) |
| 6 | Q (ability) |
| 7 | Scope |
| 8 | Shoot |
| Left stick | Look (mouse) |

**How to use:**
1. Edit any row to your preference
2. **Save Profile**
3. **Start Game Remap**

Bindings are stored per device (VID/PID) and reload on reconnect. Recalibrate sticks anytime on the **Calibration Wizard** tab without losing bindings.

---

## 💾 Profiles

Profile and log storage locations:

| Type | Path |
|------|------|
| Profiles | `%LocalAppData%\GamepadCalibrator\Profiles\` |
| Logs | `%LocalAppData%\GamepadCalibrator\Logspp.log` |

Sample profile included: `profiles/Quantum-USB-Gamepad-Sample.json`

---

## 🆚 DirectInput vs XInput

| Feature | DirectInput / HID | XInput |
|---------|-------------------|--------|
| Typical devices | Generic USB / Quantum / many older pads | Xbox controllers |
| Axes | Named X/Y/Z/Rz/… | Stick LX/LY/RX/RY |
| **This app** | **Primary target** ✅ | Not native; optional virtual bridge only |

---

## 📁 Project Layout

```
GamepadCalibrator/
├── src/
│   ├── GamepadCalibrator.Core            # Models + calibration math
│   ├── GamepadCalibrator.Infrastructure  # winmm + HID + profiles
│   └── GamepadCalibrator.App             # WPF UI (MVVM)
├── tests/
│   └── GamepadCalibrator.Tests
├── profiles/
└── docs/
```

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [`docs/CALIBRATION.md`](docs/CALIBRATION.md) | Step-by-step calibration instructions |
| [`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md) | Common issues and fixes |
| [`docs/MANUAL_TEST.md`](docs/MANUAL_TEST.md) | Manual hardware test procedures |
| [`docs/RELEASE.md`](docs/RELEASE.md) | Release build instructions |

---

## 📄 License

See project repository for license details.

---

> **Tip:** If your pad is not responding correctly, make sure the hardware **ANALOG** mode is enabled before starting calibration!
