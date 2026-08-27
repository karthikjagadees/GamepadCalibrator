namespace GamepadCalibrator.Core.Models;

public sealed class StickMapping
{
    public AxisKind? Horizontal { get; set; }
    public AxisKind? Vertical { get; set; }
    public bool InvertHorizontal { get; set; }
    public bool InvertVertical { get; set; }
    public double DeadZone { get; set; } = 0.05;
    public bool HorizontalDetected => Horizontal.HasValue;
    public bool VerticalDetected => Vertical.HasValue;

    public StickMapping Clone() => new()
    {
        Horizontal = Horizontal,
        Vertical = Vertical,
        InvertHorizontal = InvertHorizontal,
        InvertVertical = InvertVertical,
        DeadZone = DeadZone
    };
}
