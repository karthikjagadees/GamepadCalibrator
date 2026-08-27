namespace GamepadCalibrator.Core.Calibration;

using GamepadCalibrator.Core.Models;

/// <summary>Pure calibration mathematics — no hardware dependencies.</summary>
public static class AxisNormalizer
{
    public static double Clamp01Signed(double value) =>
        value < -1.0 ? -1.0 : value > 1.0 ? 1.0 : value;

    /// <summary>
    /// Asymmetric normalization around a measured center.
    /// </summary>
    public static double NormalizeRaw(double raw, AxisCalibration cal)
    {
        if (!cal.HasValidRange)
            return 0.0;

        var center = cal.Center;
        var min = Math.Min(cal.Minimum, center);
        var max = Math.Max(cal.Maximum, center);

        double normalized;
        if (raw < center)
        {
            var span = center - min;
            normalized = span <= 1e-9 ? 0.0 : (raw - center) / span;
        }
        else
        {
            var span = max - center;
            normalized = span <= 1e-9 ? 0.0 : (raw - center) / span;
        }

        normalized = Clamp01Signed(normalized);

        if (cal.Invert)
            normalized = -normalized;

        normalized = ApplyDeadZone(normalized, cal.DeadZone, cal.AntiDeadZone);
        normalized = ApplySensitivity(normalized, cal.Sensitivity);
        return Clamp01Signed(normalized);
    }

    public static double ApplyDeadZone(double value, double deadZone, double antiDeadZone = 0.0)
    {
        deadZone = Math.Clamp(deadZone, 0.0, 0.30);
        antiDeadZone = Math.Clamp(antiDeadZone, 0.0, 0.30);

        var abs = Math.Abs(value);
        if (abs < deadZone)
            return 0.0;

        var sign = value < 0 ? -1.0 : 1.0;
        var scaled = (abs - deadZone) / (1.0 - deadZone);
        if (antiDeadZone > 0)
            scaled = antiDeadZone + scaled * (1.0 - antiDeadZone);
        return sign * Clamp01Signed(scaled);
    }

    /// <summary>Radial dead zone for a 2D stick.</summary>
    public static (double X, double Y) ApplyRadialDeadZone(double x, double y, double deadZone)
    {
        deadZone = Math.Clamp(deadZone, 0.0, 0.30);
        var mag = Math.Sqrt(x * x + y * y);
        if (mag < deadZone || mag < 1e-9)
            return (0, 0);

        var scaled = (mag - deadZone) / (1.0 - deadZone);
        scaled = Math.Min(1.0, scaled);
        var factor = scaled / mag;
        return (Clamp01Signed(x * factor), Clamp01Signed(y * factor));
    }

    public static double ApplySensitivity(double value, double sensitivity)
    {
        if (value == 0.0) return 0.0;
        sensitivity = Math.Clamp(sensitivity, 0.2, 3.0);
        var sign = value < 0 ? -1.0 : 1.0;
        return sign * Math.Pow(Math.Abs(value), sensitivity);
    }

    public static double Average(IReadOnlyList<double> samples)
    {
        if (samples.Count == 0) return 0;
        double sum = 0;
        foreach (var s in samples) sum += s;
        return sum / samples.Count;
    }

    /// <summary>Trimmed mean: drop outer 10% to reduce noise spikes.</summary>
    public static double RobustCenter(IReadOnlyList<double> samples)
    {
        if (samples.Count == 0) return 0;
        if (samples.Count < 5) return Average(samples);
        var sorted = samples.OrderBy(x => x).ToArray();
        var drop = Math.Max(1, sorted.Length / 10);
        var slice = sorted.Skip(drop).Take(sorted.Length - 2 * drop).ToArray();
        return Average(slice);
    }
}
