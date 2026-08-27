namespace GamepadCalibrator.Infrastructure.Input;

using System.Runtime.InteropServices;
using GamepadCalibrator.Core.Remapping;
using GamepadCalibrator.Core.Services;

/// <summary>SendInput-based keyboard/mouse emulator for remapping (no third-party runtime deps).</summary>
public sealed class SendInputEmulator : IInputEmulator
{
    private readonly HashSet<ushort> _keysDown = new();
    private bool _left, _right, _middle;

    public void Apply(RemapFrame frame)
    {
        // mouse move
        var dx = (int)Math.Round(frame.MouseDeltaX);
        var dy = (int)Math.Round(frame.MouseDeltaY);
        if (dx != 0 || dy != 0)
            SendMouseMove(dx, dy);

        SetMouse(ref _left, frame.MouseLeft, MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP);
        SetMouse(ref _right, frame.MouseRight, MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP);
        SetMouse(ref _middle, frame.MouseMiddle, MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP);

        var desired = new HashSet<ushort>();
        foreach (var (name, on) in frame.Keys)
        {
            if (!on) continue;
            if (TryMapKey(name, out var vk))
                desired.Add(vk);
        }

        foreach (var vk in _keysDown.ToList())
        {
            if (!desired.Contains(vk))
            {
                SendKey(vk, keyUp: true);
                _keysDown.Remove(vk);
            }
        }
        foreach (var vk in desired)
        {
            if (_keysDown.Add(vk))
                SendKey(vk, keyUp: false);
        }
    }

    public void ReleaseAll()
    {
        foreach (var vk in _keysDown.ToList())
            SendKey(vk, keyUp: true);
        _keysDown.Clear();
        if (_left) { SendMouseButton(MOUSEEVENTF_LEFTUP); _left = false; }
        if (_right) { SendMouseButton(MOUSEEVENTF_RIGHTUP); _right = false; }
        if (_middle) { SendMouseButton(MOUSEEVENTF_MIDDLEUP); _middle = false; }
    }

    public void Dispose() => ReleaseAll();

    private void SetMouse(ref bool state, bool want, uint down, uint up)
    {
        if (want && !state) { SendMouseButton(down); state = true; }
        else if (!want && state) { SendMouseButton(up); state = false; }
    }

    public static bool TryMapKey(string name, out ushort vk)
    {
        vk = 0;
        if (string.IsNullOrWhiteSpace(name)) return false;
        name = name.Trim();
        if (name.Length == 1)
        {
            var ch = char.ToUpperInvariant(name[0]);
            if (ch is >= 'A' and <= 'Z') { vk = ch; return true; }
            if (ch is >= '0' and <= '9') { vk = ch; return true; }
        }

        vk = name.ToLowerInvariant() switch
        {
            "space" => 0x20,
            "enter" or "return" => 0x0D,
            "shift" or "lshift" => 0x10,
            "ctrl" or "control" or "lctrl" => 0x11,
            "alt" or "lalt" => 0x12,
            "esc" or "escape" => 0x1B,
            "tab" => 0x09,
            "up" => 0x26,
            "down" => 0x28,
            "left" => 0x25,
            "right" => 0x27,
            "f1" => 0x70,
            "f2" => 0x71,
            "f3" => 0x72,
            "f4" => 0x73,
            "f5" => 0x74,
            _ => (ushort)0
        };
        return vk != 0;
    }

    private static void SendKey(ushort vk, bool keyUp)
    {
        var inp = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                }
            }
        };
        SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }

    private static void SendMouseMove(int dx, int dy)
    {
        var inp = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT { dx = dx, dy = dy, dwFlags = MOUSEEVENTF_MOVE }
            }
        };
        SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }

    private static void SendMouseButton(uint flags)
    {
        var inp = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } }
        };
        SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }

    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
