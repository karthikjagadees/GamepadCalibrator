namespace GamepadCalibrator.Core.Services;

using GamepadCalibrator.Core.Models;
using GamepadCalibrator.Core.Remapping;

public interface IInputEmulator : IDisposable
{
    void Apply(RemapFrame frame);
    void ReleaseAll();
}

public interface IRemapRuntime
{
    bool IsRunning { get; }
    void Start();
    void Stop();
}
