namespace GamepadCalibrator.Core.Models;

public sealed class AxisReading
{
    public AxisKind Axis { get; init; }
    public double Raw { get; init; }
    public double? ObservedMin { get; set; }
    public double? ObservedMax { get; set; }
    public double? Center { get; set; }
    public double Normalized { get; set; }
}

public sealed class ControllerSnapshot
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<AxisReading> Axes { get; init; } = Array.Empty<AxisReading>();
    public IReadOnlyList<bool> Buttons { get; init; } = Array.Empty<bool>();
    /// <summary>POV in hundredths of a degree, or -1 / 65535 when centered.</summary>
    public int Pov { get; init; } = -1;
    public bool IsConnected { get; init; }

    public double GetRaw(AxisKind axis)
    {
        foreach (var a in Axes)
            if (a.Axis == axis) return a.Raw;
        return 0;
    }
}

public sealed class StickState
{
    public double RawX { get; init; }
    public double RawY { get; init; }
    public double NormalizedX { get; init; }
    public double NormalizedY { get; init; }
    public bool HorizontalMissing { get; init; }
    public bool VerticalMissing { get; init; }
}

public enum TestResult
{
    Pass,
    Warning,
    Fail,
    NotTested
}
