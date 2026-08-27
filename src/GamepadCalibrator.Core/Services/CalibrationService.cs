namespace GamepadCalibrator.Core.Services;

using GamepadCalibrator.Core.Calibration;
using GamepadCalibrator.Core.Models;
using Microsoft.Extensions.Logging;

public sealed class CalibrationService : ICalibrationService
{
    private readonly ILogger<CalibrationService>? _log;

    public CalibrationService(ILogger<CalibrationService>? log = null) => _log = log;

    public AxisCalibration EnsureAxis(CalibrationProfile profile, AxisKind axis)
    {
        if (!profile.Axes.TryGetValue(axis, out var cal))
        {
            cal = new AxisCalibration { Axis = axis };
            profile.Axes[axis] = cal;
        }
        return cal;
    }

    public void ApplyCenterSamples(CalibrationProfile profile, AxisKind axis, IReadOnlyList<double> samples)
    {
        var cal = EnsureAxis(profile, axis);
        cal.Center = AxisNormalizer.RobustCenter(samples);
        cal.IsCalibrated = true;
        _log?.LogInformation("Center {Axis} = {Center:F2}", axis, cal.Center);
    }

    public void ApplyRange(CalibrationProfile profile, AxisKind axis, double min, double max)
    {
        var cal = EnsureAxis(profile, axis);
        if (max < min) (min, max) = (max, min);
        cal.Minimum = min;
        cal.Maximum = max;
        // Keep center inside [min,max]
        cal.Center = Math.Clamp(cal.Center, min, max);
        cal.IsCalibrated = true;
    }

    public void Reset(CalibrationProfile profile)
    {
        profile.Axes.Clear();
        profile.LeftStick = new StickMapping();
        profile.RightStick = new StickMapping();
        profile.UpdatedUtc = DateTimeOffset.UtcNow;
        profile.Notes = "Reset to raw defaults";
    }

    public double EvaluateAxis(CalibrationProfile profile, AxisKind axis, double raw)
    {
        var cal = EnsureAxis(profile, axis);
        // If never ranged, use provisional full-scale based on observed center scale
        if (!cal.HasValidRange || cal.Maximum <= cal.Minimum)
        {
            var provisional = cal.Clone();
            if (raw <= 255 && cal.Center <= 255)
            {
                provisional.Minimum = 0;
                provisional.Maximum = 255;
            }
            else
            {
                provisional.Minimum = 0;
                provisional.Maximum = 65535;
            }
            return AxisNormalizer.NormalizeRaw(raw, provisional);
        }
        return AxisNormalizer.NormalizeRaw(raw, cal);
    }

    public StickState EvaluateStick(CalibrationProfile profile, StickMapping mapping, ControllerSnapshot snap)
    {
        double rawX = 0, rawY = 0, nx = 0, ny = 0;
        var hMissing = !mapping.Horizontal.HasValue;
        var vMissing = !mapping.Vertical.HasValue;

        if (mapping.Horizontal is { } hx)
        {
            rawX = snap.GetRaw(hx);
            nx = EvaluateAxis(profile, hx, rawX);
            if (mapping.InvertHorizontal) nx = -nx;
            var cal = EnsureAxis(profile, hx);
            cal.DeadZone = mapping.DeadZone;
        }

        if (mapping.Vertical is { } vy)
        {
            rawY = snap.GetRaw(vy);
            ny = EvaluateAxis(profile, vy, rawY);
            if (mapping.InvertVertical) ny = -ny;
            var cal = EnsureAxis(profile, vy);
            cal.DeadZone = mapping.DeadZone;
        }

        (nx, ny) = AxisNormalizer.ApplyRadialDeadZone(nx, ny, mapping.DeadZone);

        return new StickState
        {
            RawX = rawX,
            RawY = rawY,
            NormalizedX = nx,
            NormalizedY = ny,
            HorizontalMissing = hMissing,
            VerticalMissing = vMissing
        };
    }
}
