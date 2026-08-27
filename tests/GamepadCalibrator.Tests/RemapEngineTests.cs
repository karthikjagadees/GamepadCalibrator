using GamepadCalibrator.Core.Models;
using GamepadCalibrator.Core.Remapping;
using GamepadCalibrator.Core.Services;
using GamepadCalibrator.Infrastructure.Input;

namespace GamepadCalibrator.Tests;

public class RemapEngineTests
{
    [Fact]
    public void ButtonBinding_MapsToKey()
    {
        var cal = new CalibrationService();
        var engine = new RemapEngine(cal);
        var profile = new CalibrationProfile
        {
            LeftStick = new StickMapping { Horizontal = AxisKind.X, Vertical = AxisKind.Y },
            Remap = new RemapSettings
            {
                Bindings =
                {
                    new ControlBinding
                    {
                        SourceType = BindingSourceType.Button,
                        ButtonNumber = 8,
                        Output = OutputActionType.MouseLeft
                    },
                    new ControlBinding
                    {
                        SourceType = BindingSourceType.Button,
                        ButtonNumber = 1,
                        Output = OutputActionType.Key,
                        KeyName = "W"
                    }
                }
            }
        };

        var buttons = Enumerable.Repeat(false, 12).ToList();
        buttons[0] = true;
        buttons[7] = true;
        var snap = new ControllerSnapshot
        {
            IsConnected = true,
            Buttons = buttons,
            Axes =
            [
                new AxisReading { Axis = AxisKind.X, Raw = 32767 },
                new AxisReading { Axis = AxisKind.Y, Raw = 32767 }
            ],
            Pov = 65535
        };

        var frame = engine.Evaluate(profile, snap);
        Assert.True(frame.Keys["W"]);
        Assert.True(frame.MouseLeft);
    }

    [Fact]
    public void HatUp_MapsToSpace()
    {
        var engine = new RemapEngine(new CalibrationService());
        var profile = new CalibrationProfile
        {
            Remap = new RemapSettings
            {
                Bindings =
                {
                    new ControlBinding
                    {
                        SourceType = BindingSourceType.Hat,
                        Hat = HatDirection.Up,
                        Output = OutputActionType.Key,
                        KeyName = "Space"
                    }
                }
            }
        };
        var snap = new ControllerSnapshot
        {
            IsConnected = true,
            Buttons = Array.Empty<bool>(),
            Axes = Array.Empty<AxisReading>(),
            Pov = 0
        };
        var frame = engine.Evaluate(profile, snap);
        Assert.True(frame.Keys["Space"]);
    }

    [Fact]
    public void FpsPreset_ContainsExpectedActions()
    {
        var preset = RemapSettings.CreateFpsCrazyGamesPreset();
        Assert.Contains(preset.Bindings, b => b.ButtonNumber == 8 && b.Output == OutputActionType.MouseLeft);
        Assert.Contains(preset.Bindings, b => b.ButtonNumber == 7 && b.Output == OutputActionType.MouseRight);
        Assert.Contains(preset.Bindings, b => b.ButtonNumber == 1 && b.KeyName == "W");
        Assert.Contains(preset.Bindings, b => b.SourceType == BindingSourceType.StickAxis && b.Output == OutputActionType.MouseMoveX);
    }

    [Theory]
    [InlineData("W", true)]
    [InlineData("Space", true)]
    [InlineData("Shift", true)]
    [InlineData("Ctrl", true)]
    [InlineData("NoSuchKey", false)]
    public void TryMapKey_KnownNames(string name, bool ok)
    {
        Assert.Equal(ok, SendInputEmulator.TryMapKey(name, out _));
    }
}
