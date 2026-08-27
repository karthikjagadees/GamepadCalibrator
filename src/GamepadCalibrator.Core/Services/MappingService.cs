namespace GamepadCalibrator.Core.Services;

using GamepadCalibrator.Core.Calibration;
using GamepadCalibrator.Core.Models;

public sealed class MappingService : IMappingService
{
    private readonly ICalibrationService _calibration;

    public MappingService(ICalibrationService calibration) => _calibration = calibration;

    public void ApplyDiscovery(CalibrationProfile profile, AxisDiscoveryResult discovery)
    {
        profile.LeftStick = discovery.LeftStick.Clone();
        profile.RightStick = discovery.RightStick.Clone();

        foreach (var (axis, center) in discovery.RestCenters)
        {
            var cal = _calibration.EnsureAxis(profile, axis);
            cal.Center = center;
        }

        MergeSpansIntoRange(profile, discovery.LeftSpans, discovery.RestCenters);
        MergeSpansIntoRange(profile, discovery.RightSpans, discovery.RestCenters);
        profile.UpdatedUtc = DateTimeOffset.UtcNow;
        profile.Notes = string.Join(" | ", discovery.Warnings);
    }

    private void MergeSpansIntoRange(
        CalibrationProfile profile,
        IReadOnlyDictionary<AxisKind, double> spans,
        IReadOnlyDictionary<AxisKind, double> centers)
    {
        foreach (var (axis, span) in spans)
        {
            if (span < 1) continue;
            centers.TryGetValue(axis, out var c);
            var cal = _calibration.EnsureAxis(profile, axis);
            // provisional min/max from center ± half span until full range cal
            cal.Minimum = Math.Min(cal.Minimum, c - span);
            cal.Maximum = Math.Max(cal.Maximum, c + span);
        }
    }

    public void SwapSticks(CalibrationProfile profile)
    {
        (profile.LeftStick, profile.RightStick) = (profile.RightStick, profile.LeftStick);
        profile.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void ResetMapping(CalibrationProfile profile)
    {
        profile.LeftStick = new StickMapping();
        profile.RightStick = new StickMapping();
        profile.UpdatedUtc = DateTimeOffset.UtcNow;
    }
}
