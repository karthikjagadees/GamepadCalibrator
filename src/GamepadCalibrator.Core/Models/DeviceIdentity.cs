namespace GamepadCalibrator.Core.Models;

/// <summary>
/// Stable identity for profile matching. Display name alone is NOT unique
/// (many pads report "Generic USB Joystick").
/// </summary>
public sealed record DeviceIdentity
{
    public required int VendorId { get; init; }
    public required int ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? Manufacturer { get; init; }
    public string? DevicePath { get; init; }
    public int? UsagePage { get; init; }
    public int? Usage { get; init; }
    public int? WinmmDeviceIndex { get; init; }
    public string InputType { get; init; } = "DirectInput / HID";
    public bool IsNativeXInput { get; init; }

    /// <summary>Stable key used for profile association (VID+PID+usage).</summary>
    public string StableKey =>
        $"{VendorId:X4}:{ProductId:X4}:UP{(UsagePage ?? 0):X}:U{(Usage ?? 0):X}";

    public string DisplayLabel =>
        $"{ProductName} ({VendorId:X4}:{ProductId:X4})";

    public bool SameHardware(DeviceIdentity other) =>
        VendorId == other.VendorId
        && ProductId == other.ProductId
        && (UsagePage ?? 0) == (other.UsagePage ?? 0)
        && (Usage ?? 0) == (other.Usage ?? 0);
}
