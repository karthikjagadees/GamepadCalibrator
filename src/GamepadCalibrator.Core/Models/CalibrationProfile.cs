namespace GamepadCalibrator.Core.Models;

public sealed class CalibrationProfile
{
    public string ProfileName { get; set; } = "Default";
    public string FriendlyName { get; set; } = "USB Gamepad";
    public DeviceIdentity Device { get; set; } = new()
    {
        VendorId = 0,
        ProductId = 0,
        ProductName = "Unknown"
    };

    public StickMapping LeftStick { get; set; } = new();
    public StickMapping RightStick { get; set; } = new();
    public Dictionary<AxisKind, AxisCalibration> Axes { get; set; } = new();
    public RemapSettings Remap { get; set; } = RemapSettings.CreateFpsCrazyGamesPreset();
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Notes { get; set; } = string.Empty;

    public CalibrationProfile Clone()
    {
        var clone = new CalibrationProfile
        {
            ProfileName = ProfileName,
            FriendlyName = FriendlyName,
            Device = Device,
            LeftStick = LeftStick.Clone(),
            RightStick = RightStick.Clone(),
            Remap = Remap.Clone(),
            UpdatedUtc = UpdatedUtc,
            Notes = Notes
        };
        foreach (var (k, v) in Axes)
            clone.Axes[k] = v.Clone();
        return clone;
    }
}
