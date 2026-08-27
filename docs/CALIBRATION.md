# Calibration instructions (Quantum / Generic USB)

1. Plug the controller into a USB port (prefer USB-A ports on desktops; avoid failing hubs).
2. Launch **Gamepad Calibrator**.
3. Click **Refresh** and select the device by **VID:PID**, not only the name `Generic USB Joystick`.
4. Open **Raw Axes**. Leave sticks alone — note resting Raw/Center candidates.
5. If your pad has an **ANALOG** button/LED, enable Analog mode so both sticks expose axes.
6. Open **Calibration Wizard** → **Axis Discovery**:
   - Hands off (resting centers)
   - Move **left** stick through extremes + outer circle
   - Move **right** stick through extremes + outer circle
7. Check discovered mapping. Typical *example* (must be verified):
   - Left H → X, Left V → Y
   - Right H → Z, Right V → Z Rotation
8. If vertical left stick does not change any axis, read the warning — try Analog mode again. Repeated recalibration will not revive a dead axis.
9. Run **Center Calibration** with hands off.
10. Run **Range Calibration** while sweeping both sticks.
11. On **Live Test**, set dead zone (default 5%, range 0–30%).
12. Complete **Final Validation**, then **Save Profile**.

Uninstalling this app does not change Windows HID drivers or registry gameport settings.
