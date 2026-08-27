namespace GamepadCalibrator.Core.Services;

using GamepadCalibrator.Core.Models;

public interface IDeviceDiscoveryService
{
    IReadOnlyList<DeviceIdentity> EnumerateControllers();
    event EventHandler? DevicesChanged;
    void StartWatching();
    void StopWatching();
}

public interface IInputService : IDisposable
{
    DeviceIdentity? CurrentDevice { get; }
    bool IsConnected { get; }
    bool Open(DeviceIdentity device);
    void Close();
    ControllerSnapshot? Poll();
    event EventHandler? Disconnected;
    event EventHandler? Reconnected;
}

public interface ICalibrationService
{
    AxisCalibration EnsureAxis(CalibrationProfile profile, AxisKind axis);
    void ApplyCenterSamples(CalibrationProfile profile, AxisKind axis, IReadOnlyList<double> samples);
    void ApplyRange(CalibrationProfile profile, AxisKind axis, double min, double max);
    void Reset(CalibrationProfile profile);
    StickState EvaluateStick(CalibrationProfile profile, StickMapping mapping, ControllerSnapshot snap);
    double EvaluateAxis(CalibrationProfile profile, AxisKind axis, double raw);
}

public interface IMappingService
{
    void ApplyDiscovery(CalibrationProfile profile, Calibration.AxisDiscoveryResult discovery);
    void SwapSticks(CalibrationProfile profile);
    void ResetMapping(CalibrationProfile profile);
}

public interface IProfileService
{
    string ProfilesDirectory { get; }
    IReadOnlyList<string> ListProfiles();
    void Save(CalibrationProfile profile, string? fileName = null);
    CalibrationProfile? Load(string fileName);
    CalibrationProfile? FindForDevice(DeviceIdentity device);
    void Delete(string fileName);
    void Export(CalibrationProfile profile, string path);
    CalibrationProfile Import(string path);
}
