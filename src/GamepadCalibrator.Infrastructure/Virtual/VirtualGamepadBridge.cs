namespace GamepadCalibrator.Infrastructure.Virtual;

/// <summary>
/// Optional virtual gamepad module (ViGEmBus, etc.).
/// Calibration works WITHOUT this. Virtual output is opt-in and never auto-installs drivers.
/// </summary>
public interface IVirtualGamepadBridge
{
    bool IsDriverAvailable { get; }
    string DriverName { get; }
    string InstallInstructions { get; }
}

public sealed class NullVirtualGamepadBridge : IVirtualGamepadBridge
{
    public bool IsDriverAvailable => false;
    public string DriverName => "None (optional)";
    public string InstallInstructions =>
        "Virtual Controller Driver Required\n\n" +
        "This optional feature needs a supported virtual gamepad driver " +
        "(for example ViGEmBus) installed by you after explicit confirmation.\n\n" +
        "Calibration, mapping, profiles, and testing work without it.\n" +
        "This application will never silently install kernel drivers.";
}
