namespace GamepadCalibrator.Core.Models;

/// <summary>Per-axis calibration parameters. Centers are measured, never assumed.</summary>
public sealed class AxisCalibration
{
    public AxisKind Axis { get; set; }
    public double Center { get; set; } = 32767;
    public double Minimum { get; set; } = 0;
    public double Maximum { get; set; } = 65535;
    public double DeadZone { get; set; } = 0.05; // 5%
    public double AntiDeadZone { get; set; } = 0.0;
    public bool Invert { get; set; }
    public double Sensitivity { get; set; } = 1.0; // curve exponent (>= 0.2)
    public bool IsCalibrated { get; set; }
    public bool HasValidRange => Maximum - Minimum >= 1.0;

    public AxisCalibration Clone() => new()
    {
        Axis = Axis,
        Center = Center,
        Minimum = Minimum,
        Maximum = Maximum,
        DeadZone = DeadZone,
        AntiDeadZone = AntiDeadZone,
        Invert = Invert,
        Sensitivity = Sensitivity,
        IsCalibrated = IsCalibrated
    };
}
