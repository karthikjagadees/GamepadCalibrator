namespace GamepadCalibrator.Infrastructure.Services;

using System.Runtime.InteropServices;
using GamepadCalibrator.Core.Models;
using GamepadCalibrator.Core.Services;
using GamepadCalibrator.Infrastructure.Winmm;
using HidSharp;
using Microsoft.Extensions.Logging;

public sealed class DeviceDiscoveryService : IDeviceDiscoveryService, IDisposable
{
    private readonly ILogger<DeviceDiscoveryService>? _log;
    private System.Threading.Timer? _timer;

    public event EventHandler? DevicesChanged;

    public DeviceDiscoveryService(ILogger<DeviceDiscoveryService>? log = null) => _log = log;

    public void StartWatching()
    {
        _timer ??= new System.Threading.Timer(_ => DevicesChanged?.Invoke(this, EventArgs.Empty), null, 2000, 2000);
    }

    public void StopWatching()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public IReadOnlyList<DeviceIdentity> EnumerateControllers()
    {
        var results = new List<DeviceIdentity>();
        var hidByVidPid = BuildHidLookup();

        var max = JoyNative.joyGetNumDevs();
        for (var i = 0; i < max; i++)
        {
            var caps = new JoyNative.JOYCAPS();
            if (JoyNative.joyGetDevCaps(i, ref caps, Marshal.SizeOf(caps)) != JoyNative.JOYERR_NOERROR)
                continue;

            var info = new JoyNative.JOYINFOEX
            {
                dwSize = Marshal.SizeOf<JoyNative.JOYINFOEX>(),
                dwFlags = JoyNative.JOY_RETURNALL
            };
            if (JoyNative.joyGetPosEx(i, ref info) != JoyNative.JOYERR_NOERROR)
                continue;

            hidByVidPid.TryGetValue((caps.wMid, caps.wPid), out var hid);

            string? manufacturer = null;
            try { manufacturer = hid?.GetManufacturer(); } catch { /* ignore */ }

            var identity = new DeviceIdentity
            {
                VendorId = caps.wMid,
                ProductId = caps.wPid,
                ProductName = string.IsNullOrWhiteSpace(caps.szPname) ? "USB Game Controller" : caps.szPname.Trim(),
                Manufacturer = manufacturer ?? "Unknown",
                DevicePath = hid?.DevicePath,
                UsagePage = 1,
                Usage = 4,
                WinmmDeviceIndex = i,
                InputType = "DirectInput / HID (winmm)",
                IsNativeXInput = false
            };
            results.Add(identity);
            _log?.LogDebug("Found controller {Label} idx={Index}", identity.DisplayLabel, i);
        }

        return results;
    }

    private static Dictionary<(int vid, int pid), HidDevice> BuildHidLookup()
    {
        var map = new Dictionary<(int, int), HidDevice>();
        foreach (var d in DeviceList.Local.GetHidDevices())
        {
            try { map[(d.VendorID, d.ProductID)] = d; }
            catch { /* ignore */ }
        }
        return map;
    }

    public void Dispose() => StopWatching();
}
