# Troubleshooting

## Cursor / stick drifts while untouched

Run **Center Calibration** with hands completely off. Increase dead zone to 8–12%. Cheap pads often rest far from mid-scale (e.g. X≈40).

## Left/right works, up/down does not

1. Press hardware **ANALOG**.
2. Re-run **Axis Discovery**.
3. Watch **Raw Axes** while pushing the stick up — which column changes?
4. Manually assign that axis as Left Vertical.
5. If **no** axis changes, treat as hardware/firmware failure — software cannot invent vertical data.

## Right stick does nothing

Many Generic USB / DragonRise clones only report one healthy stick. Discovery will warn. Confirm Analog mode and USB connection.

## Profile does not reload

Profiles key on VID+PID+usage, not the string `Generic USB Joystick`. Confirm Diagnostics shows the same VID:PID.

## App finds no controllers

- Re-seat USB cable  
- Try another port  
- Confirm Device Manager shows a game controller  
- Close other apps exclusively locking the device  

## Administrator rights

Not required for calibration. Do not replace Microsoft HID drivers.

## joy.cpl vs this app

`joy.cpl` is optional. This app’s profiles are independent. Do not expect joy.cpl to show your app’s dead zones.
