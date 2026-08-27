namespace GamepadCalibrator.Infrastructure.Services;

using GamepadCalibrator.Core.Models;
using GamepadCalibrator.Core.Services;
using GamepadCalibrator.Infrastructure.Winmm;
using Microsoft.Extensions.Logging;

/// <summary>
/// Reads raw axes/buttons/HAT via winmm joyGetPosEx (DirectInput-compatible path
/// for Generic USB Joystick / Quantum-class pads). Does not require a virtual driver.
/// </summary>
public sealed class WinmmInputService : IInputService
{
    private readonly ILogger<WinmmInputService>? _log;
    private DeviceIdentity? _device;
    private int _index = -1;
    private bool _wasConnected;
    private readonly Dictionary<AxisKind, double> _obsMin = new();
    private readonly Dictionary<AxisKind, double> _obsMax = new();

    public DeviceIdentity? CurrentDevice => _device;
    public bool IsConnected => _index >= 0 && PollRaw(out _);

    public event EventHandler? Disconnected;
    public event EventHandler? Reconnected;

    public WinmmInputService(ILogger<WinmmInputService>? log = null) => _log = log;

    public bool Open(DeviceIdentity device)
    {
        Close();
        var idx = device.WinmmDeviceIndex ?? FindIndex(device);
        if (idx < 0)
        {
            _log?.LogWarning("Could not open {Device}", device.DisplayLabel);
            return false;
        }

        var info = new JoyNative.JOYINFOEX
        {
            dwSize = System.Runtime.InteropServices.Marshal.SizeOf<JoyNative.JOYINFOEX>(),
            dwFlags = JoyNative.JOY_RETURNALL
        };
        if (JoyNative.joyGetPosEx(idx, ref info) != JoyNative.JOYERR_NOERROR)
            return false;

        _index = idx;
        _device = device with { WinmmDeviceIndex = idx };
        _wasConnected = true;
        _obsMin.Clear();
        _obsMax.Clear();
        _log?.LogInformation("Opened {Device} at winmm index {Index}", device.DisplayLabel, idx);
        return true;
    }

    public void Close()
    {
        _index = -1;
        _device = null;
        _wasConnected = false;
    }

    public ControllerSnapshot? Poll()
    {
        if (_index < 0)
            return null;

        if (!PollRaw(out var info))
        {
            if (_wasConnected)
            {
                _wasConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
            return new ControllerSnapshot { IsConnected = false };
        }

        if (!_wasConnected)
        {
            _wasConnected = true;
            Reconnected?.Invoke(this, EventArgs.Empty);
        }

        var axes = new List<AxisReading>();
        void Add(AxisKind kind, int raw)
        {
            var r = (double)raw;
            if (!_obsMin.ContainsKey(kind) || r < _obsMin[kind]) _obsMin[kind] = r;
            if (!_obsMax.ContainsKey(kind) || r > _obsMax[kind]) _obsMax[kind] = r;
            axes.Add(new AxisReading
            {
                Axis = kind,
                Raw = r,
                ObservedMin = _obsMin[kind],
                ObservedMax = _obsMax[kind]
            });
        }

        Add(AxisKind.X, info.dwXpos);
        Add(AxisKind.Y, info.dwYpos);
        Add(AxisKind.Z, info.dwZpos);
        Add(AxisKind.RotationZ, info.dwRpos);
        Add(AxisKind.U, info.dwUpos);
        Add(AxisKind.V, info.dwVpos);

        var buttons = new List<bool>();
        for (var b = 0; b < 32; b++)
            buttons.Add((info.dwButtons & (1 << b)) != 0);
        // Trim trailing unused after last pressed historically — keep first NumButtons if known
        var caps = new JoyNative.JOYCAPS();
        var buttonCount = 12;
        if (JoyNative.joyGetDevCaps(_index, ref caps, System.Runtime.InteropServices.Marshal.SizeOf(caps)) == JoyNative.JOYERR_NOERROR)
            buttonCount = (int)Math.Clamp(caps.wNumButtons, 1, 32);
        buttons = buttons.Take(buttonCount).ToList();

        return new ControllerSnapshot
        {
            IsConnected = true,
            Axes = axes,
            Buttons = buttons,
            Pov = info.dwPOV
        };
    }

    private bool PollRaw(out JoyNative.JOYINFOEX info)
    {
        info = new JoyNative.JOYINFOEX
        {
            dwSize = System.Runtime.InteropServices.Marshal.SizeOf<JoyNative.JOYINFOEX>(),
            dwFlags = JoyNative.JOY_RETURNALL
        };
        if (_index < 0) return false;
        return JoyNative.joyGetPosEx(_index, ref info) == JoyNative.JOYERR_NOERROR;
    }

    private static int FindIndex(DeviceIdentity device)
    {
        var max = JoyNative.joyGetNumDevs();
        for (var i = 0; i < max; i++)
        {
            var caps = new JoyNative.JOYCAPS();
            if (JoyNative.joyGetDevCaps(i, ref caps, System.Runtime.InteropServices.Marshal.SizeOf(caps)) != JoyNative.JOYERR_NOERROR)
                continue;
            if (caps.wMid == device.VendorId && caps.wPid == device.ProductId)
                return i;
        }
        return -1;
    }

    public void Dispose() => Close();
}
