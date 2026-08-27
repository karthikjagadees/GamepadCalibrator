namespace GamepadCalibrator.Core.Calibration;

using GamepadCalibrator.Core.Models;

public sealed class AxisDiscoveryResult
{
    public IReadOnlyDictionary<AxisKind, double> RestCenters { get; init; }
        = new Dictionary<AxisKind, double>();
    public StickMapping LeftStick { get; init; } = new();
    public StickMapping RightStick { get; init; } = new();
    public IReadOnlyDictionary<AxisKind, double> LeftSpans { get; init; }
        = new Dictionary<AxisKind, double>();
    public IReadOnlyDictionary<AxisKind, double> RightSpans { get; init; }
        = new Dictionary<AxisKind, double>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public bool LeftVerticalMissing { get; init; }
    public bool RightStickMissing { get; init; }
}

/// <summary>
/// Discovers which physical axes belong to left/right sticks from measured spans.
/// Never hard-codes X/Y/Z/Rz — ranks by observed movement.
/// </summary>
public static class AxisDiscoveryEngine
{
    public const double MinUsefulSpan = 1500.0; // winmm 0..65535 scale
    public const double MinUsefulSpanByte = 40.0; // HID 0..255 scale

    public static AxisDiscoveryResult Resolve(
        IReadOnlyDictionary<AxisKind, double> restCenters,
        IReadOnlyDictionary<AxisKind, double> leftSpans,
        IReadOnlyDictionary<AxisKind, double> rightSpans,
        double minSpan)
    {
        var warnings = new List<string>();

        var leftRanked = RankMovingAxes(leftSpans, minSpan);
        var rightRanked = RankMovingAxes(rightSpans, minSpan);

        var left = new StickMapping();
        if (leftRanked.Count >= 1)
            left.Horizontal = leftRanked[0];
        if (leftRanked.Count >= 2)
            left.Vertical = leftRanked[1];
        else if (leftRanked.Count == 1)
        {
            warnings.Add(
                "VERTICAL AXIS NOT DETECTED for LEFT stick. " +
                "No second axis showed measurable movement. " +
                "Try ANALOG mode, re-run discovery, or test the other stick. " +
                "If nothing responds vertically, this may be a hardware/firmware issue — " +
                "software calibration cannot repair a dead axis.");
        }

        var used = new HashSet<AxisKind>();
        if (left.Horizontal.HasValue) used.Add(left.Horizontal.Value);
        if (left.Vertical.HasValue) used.Add(left.Vertical.Value);

        var right = new StickMapping();
        var rightCandidates = rightRanked.Where(a => !used.Contains(a)).ToList();
        // Prefer axes that moved more during right-stick phase than left-stick phase
        rightCandidates = rightCandidates
            .OrderByDescending(a =>
            {
                rightSpans.TryGetValue(a, out var rs);
                leftSpans.TryGetValue(a, out var ls);
                return rs - ls * 0.25;
            })
            .ToList();

        if (rightCandidates.Count >= 1)
            right.Horizontal = rightCandidates[0];
        if (rightCandidates.Count >= 2)
            right.Vertical = rightCandidates[1];

        var rightMissing = !right.Horizontal.HasValue && !right.Vertical.HasValue;
        if (rightMissing)
        {
            warnings.Add(
                "RIGHT STICK NOT DETECTED. No unique axes moved during the right-stick " +
                "discovery step. Press the physical ANALOG button (if present), then run " +
                "discovery again. Some Generic USB pads only expose one working stick.");
        }

        // Heuristic: if classic Quantum layout spans look correct, prefer it when ambiguous
        PreferClassicIfClear(left, right, leftSpans, rightSpans, minSpan);

        return new AxisDiscoveryResult
        {
            RestCenters = restCenters,
            LeftStick = left,
            RightStick = right,
            LeftSpans = leftSpans,
            RightSpans = rightSpans,
            Warnings = warnings,
            LeftVerticalMissing = !left.Vertical.HasValue,
            RightStickMissing = rightMissing
        };
    }

    private static void PreferClassicIfClear(
        StickMapping left,
        StickMapping right,
        IReadOnlyDictionary<AxisKind, double> leftSpans,
        IReadOnlyDictionary<AxisKind, double> rightSpans,
        double minSpan)
    {
        bool Strong(AxisKind a, IReadOnlyDictionary<AxisKind, double> spans) =>
            spans.TryGetValue(a, out var s) && s >= minSpan;

        // Only nudge if classic axes clearly moved in expected phases
        if (Strong(AxisKind.X, leftSpans) && left.Horizontal is null)
            left.Horizontal = AxisKind.X;
        if (Strong(AxisKind.Y, leftSpans) && left.Vertical is null)
            left.Vertical = AxisKind.Y;
        if (Strong(AxisKind.Z, rightSpans) && right.Horizontal is null)
            right.Horizontal = AxisKind.Z;
        if (Strong(AxisKind.RotationZ, rightSpans) && right.Vertical is null)
            right.Vertical = AxisKind.RotationZ;
    }

    public static List<AxisKind> RankMovingAxes(
        IReadOnlyDictionary<AxisKind, double> spans,
        double minSpan)
    {
        return spans
            .Where(kv => kv.Value >= minSpan)
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();
    }

    public static Dictionary<AxisKind, double> ComputeSpans(
        IReadOnlyDictionary<AxisKind, double> mins,
        IReadOnlyDictionary<AxisKind, double> maxs)
    {
        var result = new Dictionary<AxisKind, double>();
        foreach (var axis in Enum.GetValues<AxisKind>())
        {
            mins.TryGetValue(axis, out var min);
            maxs.TryGetValue(axis, out var max);
            result[axis] = Math.Max(0, max - min);
        }
        return result;
    }
}
