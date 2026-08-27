# gamepad Calibrator

Professional Windows 10/11 utility for detecting, diagnosing, calibrating, normalizing, remapping, and testing **generic USB / DirectInput** game controllers (including Quantum QHM7468-family pads that appear as `Generic USB Joystick`).

## What this solves

Cheap DirectInput pads often expose sticks as **X / Y / Z / Z Rotation** with badly offset centers (for example X≈40, Y stuck near max). Windows `joy.cpl` is not enough.

This app builds its **own calibration layer**:

- Measures real centers (never assumes 128)
- Discovers which physical stick moves which axis
- Supports manual remapping, invert, swap, dead zones, sensitivity
- Saves profiles keyed by **VID+PID+usage**, not display name alone
- Distinguishes software mapping issues from likely hardware failures

## What it cannot do

- Repair physically broken potentiometers / dead axes
- Press the hardware **ANALOG** button for you
- Silently convert DirectInput into XInput
- Auto-install virtual gamepad kernel drivers

## Requirements

- Windows 10 or 11 (laptop or desktop)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build
- A USB DirectInput/HID gamepad

## Build

```powershell
cd $env:USERPROFILE\GamepadCalibrator
dotnet restore
dotnet build GamepadCalibrator.sln -c Release
dotnet test GamepadCalibrator.sln -c Release
```

Run:

```powershell
dotnet run --project src\GamepadCalibrator.App -c Release
```

Or launch:

`src\GamepadCalibrator.App\bin\Release\net8.0-windows\GamepadCalibrator.App.exe`

## Libraries

| Package | Why | License | Status |
|---------|-----|---------|--------|
| HidSharp 2.1.0 | Enumerate HID VID/PID/path/manufacturer | Apache-2.0 | Stable, widely used |
| CommunityToolkit.Mvvm | MVVM source generators | MIT | Actively maintained |
| Microsoft.Extensions.* | DI + logging | MIT | Supported by Microsoft |
| winmm `joyGetPosEx` | Raw axes/buttons/HAT for DirectInput-class pads | OS API | Built into Windows |

No abandoned DirectInput wrappers are required for core calibration.

## Game Remap (key / mouse bindings)

Open the **Game Remap** tab to assign any control on the connected joystick to keyboard keys or mouse actions:

- Buttons 1–N → any key / mouse button  
- D-pad directions → jump, sprint, interact, crouch, etc.  
- Stick axes → camera mouse look (`MouseMoveX` / `MouseMoveY`)

**FPS Preset (CrazyGames)** loads the layout used during setup (buttons 1–4 = WASD, D-pad = Space/Shift/E/Ctrl, 5=R, 6=Q, 7=scope, 8=shoot, left stick = look).

Edit any row, **Save Profile**, then **Start Game Remap**. Bindings are stored per device (VID/PID) and reload on reconnect. Recalibrate sticks anytime on the Calibration Wizard tab without losing bindings.

1. **Refresh** → select controller  
2. **Raw Axes** tab → verify live values  
3. Press hardware **ANALOG** if the pad has it / LED  
4. **Calibration Wizard → Axis Discovery** (rest → left stick → right stick)  
5. Correct mapping manually if needed  
6. **Center Calibration** (hands off)  
7. **Range Calibration** (full circles)  
8. Adjust dead zones on **Live Test**  
9. **Final Validation** → mark PASS/FAIL → **Save Profile**

On reconnect, a matching profile is loaded automatically.

## Profiles

Stored under:

`%LocalAppData%\GamepadCalibrator\Profiles\`

Logs:

`%LocalAppData%\GamepadCalibrator\Logs\app.log`

Sample: `profiles/Quantum-USB-Gamepad-Sample.json`

## DirectInput vs XInput

| | DirectInput / HID | XInput |
|--|-------------------|--------|
| Typical devices | Generic USB / Quantum / many older pads | Xbox controllers |
| Axes | Named X/Y/Z/Rz/… | Stick LX/LY/RX/RY |
| This app | **Primary target** | Not native; optional virtual bridge only |


## Project layout

```
GamepadCalibrator/
├── src/GamepadCalibrator.Core            # models + calibration math
├── src/GamepadCalibrator.Infrastructure  # winmm + HID + profiles
├── src/GamepadCalibrator.App             # WPF UI (MVVM)
├── tests/GamepadCalibrator.Tests
├── profiles/
└── docs/
```

## Docs

- [Calibration instructions](docs/CALIBRATION.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Manual hardware test](docs/MANUAL_TEST.md)
- [Release build](docs/RELEASE.md)
