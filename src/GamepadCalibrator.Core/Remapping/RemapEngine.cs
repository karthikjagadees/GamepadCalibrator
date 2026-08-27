namespace GamepadCalibrator.Core.Remapping;

using GamepadCalibrator.Core.Models;
using GamepadCalibrator.Core.Services;

/// <summary>Evaluates calibrated controller state into discrete/analog remap outputs.</summary>
public sealed class RemapEngine
{
    private readonly ICalibrationService _calibration;

    public RemapEngine(ICalibrationService calibration) => _calibration = calibration;

    public RemapFrame Evaluate(CalibrationProfile profile, ControllerSnapshot snap)
    {
        var settings = profile.Remap;
        var left = _calibration.EvaluateStick(profile, profile.LeftStick, snap);
        var right = _calibration.EvaluateStick(profile, profile.RightStick, snap);
        var hat = DecodeHat(snap.Pov);

        var keys = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var mouseLeft = false;
        var mouseRight = false;
        var mouseMiddle = false;
        double moveX = 0, moveY = 0;
        var dead = Math.Clamp(settings.StickDeadZone, 0, 0.3);
        var speed = Math.Clamp(settings.CameraSpeed, 1, 80);

        foreach (var b in settings.Bindings.Where(x => x.Enabled && x.Output != OutputActionType.None))
        {
            switch (b.SourceType)
            {
                case BindingSourceType.Button:
                {
                    var idx = b.ButtonNumber - 1;
                    var pressed = idx >= 0 && idx < snap.Buttons.Count && snap.Buttons[idx];
                    ApplyDigital(b, pressed, keys, ref mouseLeft, ref mouseRight, ref mouseMiddle);
                    break;
                }
                case BindingSourceType.Hat:
                {
                    var pressed = b.Hat switch
                    {
                        HatDirection.Up => hat.Up,
                        HatDirection.Down => hat.Down,
                        HatDirection.Left => hat.Left,
                        HatDirection.Right => hat.Right,
                        _ => false
                    };
                    ApplyDigital(b, pressed, keys, ref mouseLeft, ref mouseRight, ref mouseMiddle);
                    break;
                }
                case BindingSourceType.StickAxis:
                {
                    var axis = ReadStickAxis(b.StickAxis, left, right);
                    if (b.Invert) axis = -axis;
                    if (Math.Abs(axis) < dead) axis = 0;
                    if (b.Output == OutputActionType.MouseMoveX)
                        moveX += axis * speed;
                    else if (b.Output == OutputActionType.MouseMoveY)
                        moveY += axis * speed;
                    else if (b.Output == OutputActionType.Key && !string.IsNullOrWhiteSpace(b.KeyName))
                    {
                        // digital threshold on stick for optional key binding
                        keys[b.KeyName] = keys.GetValueOrDefault(b.KeyName) || Math.Abs(axis) >= Math.Max(dead, 0.35);
                    }
                    break;
                }
            }
        }

        return new RemapFrame(keys, mouseLeft, mouseRight, mouseMiddle, moveX, moveY);
    }

    private static double ReadStickAxis(StickAxisRole role, StickState left, StickState right) => role switch
    {
        StickAxisRole.LeftHorizontal => left.NormalizedX,
        StickAxisRole.LeftVertical => left.NormalizedY,
        StickAxisRole.RightHorizontal => right.NormalizedX,
        StickAxisRole.RightVertical => right.NormalizedY,
        _ => 0
    };

    private static void ApplyDigital(
        ControlBinding b,
        bool pressed,
        Dictionary<string, bool> keys,
        ref bool mouseLeft,
        ref bool mouseRight,
        ref bool mouseMiddle)
    {
        switch (b.Output)
        {
            case OutputActionType.Key when !string.IsNullOrWhiteSpace(b.KeyName):
                keys[b.KeyName] = keys.GetValueOrDefault(b.KeyName) || pressed;
                break;
            case OutputActionType.MouseLeft:
                mouseLeft |= pressed;
                break;
            case OutputActionType.MouseRight:
                mouseRight |= pressed;
                break;
            case OutputActionType.MouseMiddle:
                mouseMiddle |= pressed;
                break;
        }
    }

    public static (bool Up, bool Down, bool Left, bool Right) DecodeHat(int pov)
    {
        if (pov < 0 || pov == 65535) return (false, false, false, false);
        var deg = (pov / 100.0) % 360;
        bool up = false, down = false, left = false, right = false;
        if (deg >= 337.5 || deg < 22.5) up = true;
        else if (deg < 67.5) { up = right = true; }
        else if (deg < 112.5) right = true;
        else if (deg < 157.5) { right = down = true; }
        else if (deg < 202.5) down = true;
        else if (deg < 247.5) { down = left = true; }
        else if (deg < 292.5) left = true;
        else { up = left = true; }
        return (up, down, left, right);
    }
}

public readonly record struct RemapFrame(
    IReadOnlyDictionary<string, bool> Keys,
    bool MouseLeft,
    bool MouseRight,
    bool MouseMiddle,
    double MouseDeltaX,
    double MouseDeltaY);
