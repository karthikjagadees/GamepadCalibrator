namespace GamepadCalibrator.Core.Models;

/// <summary>Logical names for physical axes as reported by Windows/HID.</summary>
public enum AxisKind
{
    X = 0,
    Y = 1,
    Z = 2,
    RotationZ = 3, // Z Rotation / Rz
    U = 4,
    V = 5,
    Slider0 = 6,
    Slider1 = 7
}

public static class AxisKindExtensions
{
    public static string ToDisplayName(this AxisKind kind) => kind switch
    {
        AxisKind.X => "X Axis",
        AxisKind.Y => "Y Axis",
        AxisKind.Z => "Z Axis",
        AxisKind.RotationZ => "Z Rotation",
        AxisKind.U => "U Axis",
        AxisKind.V => "V Axis",
        AxisKind.Slider0 => "Slider 0",
        AxisKind.Slider1 => "Slider 1",
        _ => kind.ToString()
    };
}
