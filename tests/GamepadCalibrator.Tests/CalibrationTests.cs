using GamepadCalibrator.Core.Calibration;
using GamepadCalibrator.Core.Models;
using GamepadCalibrator.Core.Services;
using GamepadCalibrator.Infrastructure.Services;

namespace GamepadCalibrator.Tests;

public class AxisNormalizerTests
{
    [Fact]
    public void Normalize_AtCenter_ReturnsZero()
    {
        var cal = new AxisCalibration { Center = 128, Minimum = 0, Maximum = 255, DeadZone = 0 };
        Assert.Equal(0, AxisNormalizer.NormalizeRaw(128, cal), 3);
    }

    [Fact]
    public void Normalize_AsymmetricRanges()
    {
        var cal = new AxisCalibration { Center = 40, Minimum = 0, Maximum = 255, DeadZone = 0, Sensitivity = 1 };
        var left = AxisNormalizer.NormalizeRaw(0, cal);
        var right = AxisNormalizer.NormalizeRaw(255, cal);
        Assert.True(left < -0.9);
        Assert.True(right > 0.9);
    }

    [Fact]
    public void Normalize_ClampsOutsideRange()
    {
        var cal = new AxisCalibration { Center = 128, Minimum = 10, Maximum = 200, DeadZone = 0, Sensitivity = 1 };
        Assert.InRange(AxisNormalizer.NormalizeRaw(-50, cal), -1, 1);
        Assert.InRange(AxisNormalizer.NormalizeRaw(999, cal), -1, 1);
    }

    [Fact]
    public void DeadZone_SuppressesSmallValues()
    {
        Assert.Equal(0, AxisNormalizer.ApplyDeadZone(0.04, 0.05));
        Assert.True(Math.Abs(AxisNormalizer.ApplyDeadZone(0.5, 0.05)) > 0.4);
    }

    [Fact]
    public void RadialDeadZone_ZerosInsideCircle()
    {
        var (x, y) = AxisNormalizer.ApplyRadialDeadZone(0.02, 0.02, 0.05);
        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void Invert_FlipsSign()
    {
        var cal = new AxisCalibration { Center = 128, Minimum = 0, Maximum = 255, DeadZone = 0, Sensitivity = 1, Invert = true };
        var v = AxisNormalizer.NormalizeRaw(255, cal);
        Assert.True(v < 0);
    }

    [Fact]
    public void RobustCenter_IgnoresOutliers()
    {
        var samples = Enumerable.Repeat(128.0, 20).Concat(new[] { 0.0, 255.0 }).ToList();
        var c = AxisNormalizer.RobustCenter(samples);
        Assert.InRange(c, 120, 136);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.15)]
    [InlineData(0.30)]
    public void DeadZone_AcceptsConfiguredRange(double dz)
    {
        var v = AxisNormalizer.ApplyDeadZone(1.0, dz);
        Assert.InRange(v, 0, 1);
    }
}

public class AxisDiscoveryTests
{
    [Fact]
    public void Resolve_MapsLeftXy_RightZRz_WhenSpansMatch()
    {
        var rest = Enum.GetValues<AxisKind>().ToDictionary(a => a, _ => 32767.0);
        var leftSpans = new Dictionary<AxisKind, double>
        {
            [AxisKind.X] = 40000, [AxisKind.Y] = 38000, [AxisKind.Z] = 100, [AxisKind.RotationZ] = 50
        };
        var rightSpans = new Dictionary<AxisKind, double>
        {
            [AxisKind.X] = 200, [AxisKind.Y] = 100, [AxisKind.Z] = 42000, [AxisKind.RotationZ] = 41000
        };
        foreach (AxisKind a in Enum.GetValues<AxisKind>())
        {
            leftSpans.TryAdd(a, 0);
            rightSpans.TryAdd(a, 0);
        }

        var result = AxisDiscoveryEngine.Resolve(rest, leftSpans, rightSpans, 1500);
        Assert.Equal(AxisKind.X, result.LeftStick.Horizontal);
        Assert.Equal(AxisKind.Y, result.LeftStick.Vertical);
        Assert.Equal(AxisKind.Z, result.RightStick.Horizontal);
        Assert.Equal(AxisKind.RotationZ, result.RightStick.Vertical);
        Assert.False(result.LeftVerticalMissing);
    }

    [Fact]
    public void Resolve_WarnsWhenVerticalMissing()
    {
        var rest = Enum.GetValues<AxisKind>().ToDictionary(a => a, _ => 32767.0);
        var leftSpans = Enum.GetValues<AxisKind>().ToDictionary(a => a, a => a == AxisKind.X ? 30000.0 : 0.0);
        var rightSpans = Enum.GetValues<AxisKind>().ToDictionary(a => a, _ => 0.0);
        var result = AxisDiscoveryEngine.Resolve(rest, leftSpans, rightSpans, 1500);
        Assert.True(result.LeftVerticalMissing);
        Assert.Contains(result.Warnings, w => w.Contains("VERTICAL AXIS NOT DETECTED"));
    }

    [Fact]
    public void ComputeSpans_UsesMeasuredMinMax()
    {
        var mins = new Dictionary<AxisKind, double> { [AxisKind.X] = 10 };
        var maxs = new Dictionary<AxisKind, double> { [AxisKind.X] = 200 };
        var spans = AxisDiscoveryEngine.ComputeSpans(mins, maxs);
        Assert.Equal(190, spans[AxisKind.X]);
    }
}

public class CalibrationServiceTests
{
    [Fact]
    public void ApplyCenter_UsesRobustAverage()
    {
        var svc = new CalibrationService();
        var profile = new CalibrationProfile();
        svc.ApplyCenterSamples(profile, AxisKind.X, new[] { 40.0, 41, 39, 40, 40 });
        Assert.InRange(profile.Axes[AxisKind.X].Center, 39, 41);
    }

    [Fact]
    public void ApplyRange_SwapsIfInverted()
    {
        var svc = new CalibrationService();
        var profile = new CalibrationProfile();
        svc.ApplyRange(profile, AxisKind.Y, 200, 10);
        Assert.Equal(10, profile.Axes[AxisKind.Y].Minimum);
        Assert.Equal(200, profile.Axes[AxisKind.Y].Maximum);
    }

    [Fact]
    public void Reset_ClearsMappingAndAxes()
    {
        var svc = new CalibrationService();
        var profile = new CalibrationProfile
        {
            LeftStick = new StickMapping { Horizontal = AxisKind.X },
            Axes = { [AxisKind.X] = new AxisCalibration { Axis = AxisKind.X, Center = 1 } }
        };
        svc.Reset(profile);
        Assert.Empty(profile.Axes);
        Assert.Null(profile.LeftStick.Horizontal);
    }

    [Fact]
    public void EvaluateStick_AppliesInversion()
    {
        var svc = new CalibrationService();
        var profile = new CalibrationProfile();
        svc.ApplyCenterSamples(profile, AxisKind.X, new[] { 128.0 });
        svc.ApplyRange(profile, AxisKind.X, 0, 255);
        profile.Axes[AxisKind.X].DeadZone = 0;
        profile.Axes[AxisKind.X].Sensitivity = 1;
        var mapping = new StickMapping { Horizontal = AxisKind.X, InvertHorizontal = true, DeadZone = 0 };
        var snap = new ControllerSnapshot
        {
            IsConnected = true,
            Axes = new[] { new AxisReading { Axis = AxisKind.X, Raw = 255 } }
        };
        var state = svc.EvaluateStick(profile, mapping, snap);
        Assert.True(state.NormalizedX < 0);
    }
}

public class MappingServiceTests
{
    [Fact]
    public void SwapSticks_ExchangesMappings()
    {
        var cal = new CalibrationService();
        var map = new MappingService(cal);
        var profile = new CalibrationProfile
        {
            LeftStick = new StickMapping { Horizontal = AxisKind.X },
            RightStick = new StickMapping { Horizontal = AxisKind.Z }
        };
        map.SwapSticks(profile);
        Assert.Equal(AxisKind.Z, profile.LeftStick.Horizontal);
        Assert.Equal(AxisKind.X, profile.RightStick.Horizontal);
    }

    [Fact]
    public void ResetMapping_ClearsAssignments()
    {
        var map = new MappingService(new CalibrationService());
        var profile = new CalibrationProfile { LeftStick = new StickMapping { Horizontal = AxisKind.X } };
        map.ResetMapping(profile);
        Assert.Null(profile.LeftStick.Horizontal);
    }
}

public class ProfileServiceTests
{
    [Fact]
    public void SaveLoad_RoundTrips()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gc-profiles-" + Guid.NewGuid().ToString("N"));
        var svc = new ProfileService(profilesDirectory: dir);
        var profile = new CalibrationProfile
        {
            ProfileName = "Default",
            FriendlyName = "Quantum USB Gamepad",
            Device = new DeviceIdentity { VendorId = 0x0079, ProductId = 0x0006, ProductName = "Generic USB Joystick" },
            LeftStick = new StickMapping { Horizontal = AxisKind.X, Vertical = AxisKind.Y, DeadZone = 0.05 },
            RightStick = new StickMapping { Horizontal = AxisKind.Z, Vertical = AxisKind.RotationZ, DeadZone = 0.05 }
        };
        profile.Axes[AxisKind.X] = new AxisCalibration { Axis = AxisKind.X, Center = 40, Minimum = 0, Maximum = 255, IsCalibrated = true };

        svc.Save(profile, "test.json");
        var loaded = svc.Load("test.json");
        Assert.NotNull(loaded);
        Assert.Equal(0x0079, loaded!.Device.VendorId);
        Assert.Equal(AxisKind.Z, loaded.RightStick.Horizontal);
        Assert.Equal(40, loaded.Axes[AxisKind.X].Center);

        var found = svc.FindForDevice(profile.Device);
        Assert.NotNull(found);
        svc.Delete("test.json");
        Directory.Delete(dir, true);
    }

    [Fact]
    public void DeviceIdentity_NotByNameAlone()
    {
        var a = new DeviceIdentity { VendorId = 1, ProductId = 2, ProductName = "Generic USB Joystick" };
        var b = new DeviceIdentity { VendorId = 9, ProductId = 9, ProductName = "Generic USB Joystick" };
        Assert.False(a.SameHardware(b));
        Assert.NotEqual(a.StableKey, b.StableKey);
    }
}

public class DeviceIdentityTests
{
    [Fact]
    public void StableKey_IncludesVidPidUsage()
    {
        var d = new DeviceIdentity { VendorId = 0x79, ProductId = 0x6, ProductName = "x", UsagePage = 1, Usage = 4 };
        Assert.Contains("0079:0006", d.StableKey);
    }
}
