using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GamepadCalibrator.Core.Calibration;
using GamepadCalibrator.Core.Models;
using GamepadCalibrator.Core.Remapping;
using GamepadCalibrator.Core.Services;
using GamepadCalibrator.Infrastructure.Virtual;
using Microsoft.Extensions.Logging;

namespace GamepadCalibrator.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDeviceDiscoveryService _discovery;
    private readonly IInputService _input;
    private readonly ICalibrationService _calibration;
    private readonly IMappingService _mapping;
    private readonly IProfileService _profiles;
    private readonly IVirtualGamepadBridge _virtual;
    private readonly IInputEmulator _emulator;
    private readonly RemapEngine _remapEngine;
    private readonly ILogger<MainViewModel> _log;
    private readonly DispatcherTimer _pollTimer;
    private readonly Dictionary<AxisKind, List<double>> _centerBuffers = new();
    private readonly Dictionary<AxisKind, double> _rangeMin = new();
    private readonly Dictionary<AxisKind, double> _rangeMax = new();

    public MainViewModel(
        IDeviceDiscoveryService discovery,
        IInputService input,
        ICalibrationService calibration,
        IMappingService mapping,
        IProfileService profiles,
        IVirtualGamepadBridge virtualBridge,
        IInputEmulator emulator,
        ILogger<MainViewModel> log)
    {
        _discovery = discovery;
        _input = input;
        _calibration = calibration;
        _mapping = mapping;
        _profiles = profiles;
        _virtual = virtualBridge;
        _emulator = emulator;
        _remapEngine = new RemapEngine(calibration);
        _log = log;

        Profile = new CalibrationProfile();
        AxisKinds = Enum.GetValues<AxisKind>().ToList();
        WorkflowStatus = "Plug in your gamepad, click Refresh, then open Set Controls.";
        BuildControlSlots();
        ApplyFpsPresetToSlots();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _pollTimer.Tick += (_, _) => Poll();
        _pollTimer.Start();

        _input.Disconnected += (_, _) =>
        {
            ConnectionStatus = "Controller disconnected.";
            IsConnected = false;
            if (IsRemapActive)
            {
                IsRemapActive = false;
                _emulator.ReleaseAll();
            }
        };
        _input.Reconnected += (_, _) =>
        {
            ConnectionStatus = "Controller connected. Loading saved calibration profile...";
            TryAutoLoadProfile();
            IsConnected = true;
        };

        _discovery.StartWatching();
        RefreshDevices();
    }

    public ObservableCollection<DeviceIdentity> Devices { get; } = new();
    public ObservableCollection<AxisRowViewModel> AxisRows { get; } = new();
    public ObservableCollection<ButtonRowViewModel> ButtonRows { get; } = new();
    public ObservableCollection<ControlSlotViewModel> ButtonSlots { get; } = new();
    public ObservableCollection<ControlSlotViewModel> DpadSlots { get; } = new();
    public ObservableCollection<ControlSlotViewModel> StickSlots { get; } = new();
    public ObservableCollection<string> Warnings { get; } = new();
    public ObservableCollection<string> LogLines { get; } = new();
    public IReadOnlyList<AxisKind> AxisKinds { get; }

    [ObservableProperty] private DeviceIdentity? _selectedDevice;
    [ObservableProperty] private CalibrationProfile _profile;
    [ObservableProperty] private string _workflowStatus = "";
    [ObservableProperty] private string _connectionStatus = "No controller";
    [ObservableProperty] private string _wizardPrompt = "";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isSamplingCenter;
    [ObservableProperty] private bool _isSamplingRange;
    [ObservableProperty] private bool _isDiscovering;
    [ObservableProperty] private string _discoveryPhase = "";
    [ObservableProperty] private double _leftNormX;
    [ObservableProperty] private double _leftNormY;
    [ObservableProperty] private double _rightNormX;
    [ObservableProperty] private double _rightNormY;
    [ObservableProperty] private double _leftRawX;
    [ObservableProperty] private double _leftRawY;
    [ObservableProperty] private double _rightRawX;
    [ObservableProperty] private double _rightRawY;
    [ObservableProperty] private string _hatLabel = "CENTER";
    [ObservableProperty] private double _hatX;
    [ObservableProperty] private double _hatY;
    [ObservableProperty] private Thickness _hatMargin = new(0);
    [ObservableProperty] private double _leftDotX = 102;
    [ObservableProperty] private double _leftDotY = 102;
    [ObservableProperty] private double _rightDotX = 102;
    [ObservableProperty] private double _rightDotY = 102;
    [ObservableProperty] private string _deviceInfo = "";
    [ObservableProperty] private string _analogModeHelp =
        "ANALOG MODE\n\nThis controller may expose different axes depending on its hardware mode.\n" +
        "If the physical ANALOG button changes axis behavior, press it and run Axis Discovery again.\n\n" +
        "This app cannot press or control the physical ANALOG button electronically.";
    [ObservableProperty] private string _virtualDriverStatus = "";
    [ObservableProperty] private string _finalSummary = "";
    [ObservableProperty] private TestResult _leftStickTest = TestResult.NotTested;
    [ObservableProperty] private TestResult _rightStickTest = TestResult.NotTested;
    [ObservableProperty] private TestResult _hatTest = TestResult.NotTested;
    [ObservableProperty] private TestResult _buttonTest = TestResult.NotTested;
    [ObservableProperty] private bool _isRemapActive;
    [ObservableProperty] private string _remapStatus = "Remap is OFF. Choose what each button does, then press Start Playing.";
    [ObservableProperty] private int _detectedButtonCount = 12;

    partial void OnSelectedDeviceChanged(DeviceIdentity? value)
    {
        if (value is null) return;
        if (_input.Open(value))
        {
            IsConnected = true;
            ConnectionStatus = $"Connected: {value.DisplayLabel}";
            Profile.Device = value;
            Profile.FriendlyName = value.ProductName;
            DeviceInfo =
                $"Name: {value.ProductName}\nManufacturer: {value.Manufacturer}\n" +
                $"VID: {value.VendorId:X4}\nPID: {value.ProductId:X4}\n" +
                $"USB Path: {value.DevicePath}\nHID Usage Page: {value.UsagePage}\nHID Usage: {value.Usage}\n" +
                $"Input Type: {value.InputType}\nXInput: {(value.IsNativeXInput ? "Native" : "Not native")}\n" +
                $"Stable Key: {value.StableKey}";
            VirtualDriverStatus = _virtual.IsDriverAvailable
                ? $"{_virtual.DriverName}: available"
                : _virtual.InstallInstructions;
            TryAutoLoadProfile();
            AppendLog($"Opened {value.DisplayLabel}");
        }
        else
        {
            ConnectionStatus = "Failed to open controller.";
        }
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        Devices.Clear();
        foreach (var d in _discovery.EnumerateControllers())
            Devices.Add(d);
        WorkflowStatus = Devices.Count == 0
            ? "No controllers found. Plug in a USB gamepad."
            : $"Found {Devices.Count} controller(s). Select one to begin.";
        if (Devices.Count == 1)
            SelectedDevice = Devices[0];
    }

    [RelayCommand]
    private async Task RunAxisDiscoveryAsync()
    {
        if (!EnsureConnected()) return;
        IsDiscovering = true;
        Warnings.Clear();
        try
        {
            WizardPrompt = "AXIS DISCOVERY — Release both analog sticks. Do not touch the controller.";
            DiscoveryPhase = "Resting";
            await Task.Delay(800);
            var rest = await SampleAveragesAsync(40);

            WizardPrompt = "Move the LEFT stick fully: LEFT, RIGHT, UP, DOWN, then circle the outer edge.";
            DiscoveryPhase = "Left stick";
            var (leftMin, leftMax) = await SampleRangeAsync(TimeSpan.FromSeconds(5));

            WizardPrompt = "Release sticks. Now move the RIGHT stick fully: LEFT, RIGHT, UP, DOWN, then circle.";
            DiscoveryPhase = "Right stick";
            await Task.Delay(500);
            var (rightMin, rightMax) = await SampleRangeAsync(TimeSpan.FromSeconds(5));

            var leftSpans = AxisDiscoveryEngine.ComputeSpans(leftMin, leftMax);
            var rightSpans = AxisDiscoveryEngine.ComputeSpans(rightMin, rightMax);
            var result = AxisDiscoveryEngine.Resolve(rest, leftSpans, rightSpans, AxisDiscoveryEngine.MinUsefulSpan);

            _mapping.ApplyDiscovery(Profile, result);
            foreach (var w in result.Warnings)
                Warnings.Add(w);

            // Apply measured ranges for axes that moved
            foreach (var axis in Enum.GetValues<AxisKind>())
            {
                if (leftSpans.TryGetValue(axis, out var ls) && ls >= AxisDiscoveryEngine.MinUsefulSpan)
                    _calibration.ApplyRange(Profile, axis, leftMin[axis], leftMax[axis]);
                if (rightSpans.TryGetValue(axis, out var rs) && rs >= AxisDiscoveryEngine.MinUsefulSpan)
                    _calibration.ApplyRange(Profile, axis, rightMin[axis], rightMax[axis]);
                if (rest.TryGetValue(axis, out var c))
                    _calibration.ApplyCenterSamples(Profile, axis, new[] { c });
            }

            OnPropertyChanged(nameof(Profile));
            NotifyStickMappings();
            WizardPrompt = "Discovery complete. Review mapping; correct manually if needed.";
            DiscoveryPhase = "Done";
            AppendLog($"Discovery L:H={Profile.LeftStick.Horizontal} L:V={Profile.LeftStick.Vertical} " +
                      $"R:H={Profile.RightStick.Horizontal} R:V={Profile.RightStick.Vertical}");
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    [RelayCommand]
    private async Task CalibrateCentersAsync()
    {
        if (!EnsureConnected()) return;
        IsSamplingCenter = true;
        WizardPrompt = "CENTER CALIBRATION\n\nRelease both analog sticks.\nDo not touch the controller.\n\nSampling...";
        try
        {
            await Task.Delay(600);
            _centerBuffers.Clear();
            for (var i = 0; i < 60; i++)
            {
                var snap = _input.Poll();
                if (snap?.IsConnected == true)
                {
                    foreach (var a in snap.Axes)
                    {
                        if (!_centerBuffers.TryGetValue(a.Axis, out var list))
                        {
                            list = new List<double>();
                            _centerBuffers[a.Axis] = list;
                        }
                        list.Add(a.Raw);
                    }
                }
                await Task.Delay(16);
            }

            foreach (var (axis, samples) in _centerBuffers)
                _calibration.ApplyCenterSamples(Profile, axis, samples);

            WizardPrompt = "Center calibration complete.\n" +
                           string.Join("\n", Profile.Axes.Select(kv =>
                               $"{kv.Key.ToDisplayName()} Center: {kv.Value.Center:F1}"));
            AppendLog("Centers calibrated");
        }
        finally
        {
            IsSamplingCenter = false;
        }
    }

    [RelayCommand]
    private async Task CalibrateRangesAsync()
    {
        if (!EnsureConnected()) return;
        IsSamplingRange = true;
        WizardPrompt =
            "RANGE CALIBRATION\n\nMove each stick slowly through LEFT, RIGHT, UP, DOWN,\n" +
            "then rotate around the outer edge 2–3 times.\n\nSampling for 8 seconds...";
        try
        {
            _rangeMin.Clear();
            _rangeMax.Clear();
            var end = DateTime.UtcNow + TimeSpan.FromSeconds(8);
            while (DateTime.UtcNow < end)
            {
                var snap = _input.Poll();
                if (snap?.IsConnected == true)
                {
                    foreach (var a in snap.Axes)
                    {
                        if (!_rangeMin.ContainsKey(a.Axis) || a.Raw < _rangeMin[a.Axis]) _rangeMin[a.Axis] = a.Raw;
                        if (!_rangeMax.ContainsKey(a.Axis) || a.Raw > _rangeMax[a.Axis]) _rangeMax[a.Axis] = a.Raw;
                    }
                }
                await Task.Delay(16);
            }

            foreach (var axis in _rangeMin.Keys)
                _calibration.ApplyRange(Profile, axis, _rangeMin[axis], _rangeMax[axis]);

            WizardPrompt = "Range calibration complete.";
            AppendLog("Ranges calibrated");
        }
        finally
        {
            IsSamplingRange = false;
        }
    }

    [RelayCommand]
    private void AutoDetect() => _ = RunAxisDiscoveryAsync();

    [RelayCommand]
    private void ResetMapping()
    {
        _mapping.ResetMapping(Profile);
        NotifyStickMappings();
        AppendLog("Mapping reset");
    }

    [RelayCommand]
    private void SwapSticks()
    {
        _mapping.SwapSticks(Profile);
        NotifyStickMappings();
        AppendLog("Sticks swapped");
    }

    [RelayCommand]
    private void ResetCalibration()
    {
        var result = MessageBox.Show(
            "Reset all calibration data?\n\nThis will remove:\n- center correction\n- min/max calibration\n- dead zone\n- axis mapping\n- inversion",
            "Reset Calibration",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        _calibration.Reset(Profile);
        NotifyStickMappings();
        Warnings.Clear();
        FinalSummary = "";
        AppendLog("Calibration reset to raw defaults");
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (SelectedDevice is not null)
            Profile.Device = SelectedDevice;
        SyncSlotsToProfile();
        EnsureDefaultStickAxes();
        _profiles.Save(Profile);
        WorkflowStatus = $"Saved settings for this gamepad.";
        BuildFinalSummary();
        AppendLog("Profile saved");
    }

    [RelayCommand]
    private void LoadProfile()
    {
        if (SelectedDevice is null) return;
        var loaded = _profiles.FindForDevice(SelectedDevice);
        if (loaded is null)
        {
            MessageBox.Show("No saved settings for this gamepad yet.\nSet your controls and click Save.", "Load");
            return;
        }
        Profile = loaded;
        ApplyProfileToSlots();
        NotifyStickMappings();
        AppendLog("Profile loaded");
    }

    [RelayCommand]
    private void ApplyFpsPreset()
    {
        ApplyFpsPresetToSlots();
        SyncSlotsToProfile();
        AppendLog("Loaded easy FPS layout");
        RemapStatus = "FPS layout loaded. Change any dropdown you want, then Start Playing.";
    }

    [RelayCommand]
    private void ClearAllActions()
    {
        foreach (var s in AllSlots())
            s.SelectedAction = GameActionChoice.All[0];
        SyncSlotsToProfile();
        RemapStatus = "All controls cleared. Pick new actions from the dropdowns.";
    }

    [RelayCommand]
    private void StartRemap()
    {
        if (!EnsureConnected()) return;
        SyncSlotsToProfile();
        EnsureDefaultStickAxes();
        // quick center sample so camera doesn't drift
        _ = QuickCenterAsync();
        Profile.Remap.Enabled = true;
        IsRemapActive = true;
        RemapStatus = "PLAYING — click your game window now. Your gamepad is sending keyboard & mouse.";
        AppendLog("Game remap started");
    }

    [RelayCommand]
    private void StopRemap()
    {
        IsRemapActive = false;
        Profile.Remap.Enabled = false;
        _emulator.ReleaseAll();
        RemapStatus = "Stopped. Your gamepad is no longer controlling the keyboard/mouse.";
        AppendLog("Game remap stopped");
    }

    private async Task QuickCenterAsync()
    {
        RemapStatus = "Hold still… centering sticks…";
        await Task.Delay(200);
        var buffers = Enum.GetValues<AxisKind>().ToDictionary(a => a, _ => new List<double>());
        for (var i = 0; i < 30; i++)
        {
            var snap = _input.Poll();
            if (snap?.IsConnected == true)
                foreach (var a in snap.Axes)
                    buffers[a.Axis].Add(a.Raw);
            await Task.Delay(16);
        }
        foreach (var (axis, samples) in buffers)
            if (samples.Count > 0)
                _calibration.ApplyCenterSamples(Profile, axis, samples);
    }

    private void BuildControlSlots()
    {
        ButtonSlots.Clear();
        DpadSlots.Clear();
        StickSlots.Clear();

        for (var i = 1; i <= 12; i++)
        {
            ButtonSlots.Add(new ControlSlotViewModel(
                $"Button {i}",
                $"Physical button {i} on your gamepad",
                BindingSourceType.Button,
                buttonNumber: i));
        }

        DpadSlots.Add(new ControlSlotViewModel("D-pad Up", "Press up on the D-pad / hat", BindingSourceType.Hat, hat: HatDirection.Up));
        DpadSlots.Add(new ControlSlotViewModel("D-pad Right", "Press right on the D-pad / hat", BindingSourceType.Hat, hat: HatDirection.Right));
        DpadSlots.Add(new ControlSlotViewModel("D-pad Down", "Press down on the D-pad / hat", BindingSourceType.Hat, hat: HatDirection.Down));
        DpadSlots.Add(new ControlSlotViewModel("D-pad Left", "Press left on the D-pad / hat", BindingSourceType.Hat, hat: HatDirection.Left));

        StickSlots.Add(new ControlSlotViewModel("Left stick ↔", "Move stick left and right (camera / look)", BindingSourceType.StickAxis, stickAxis: StickAxisRole.LeftHorizontal));
        StickSlots.Add(new ControlSlotViewModel("Left stick ↕", "Move stick up and down (camera / look)", BindingSourceType.StickAxis, stickAxis: StickAxisRole.LeftVertical));
        StickSlots.Add(new ControlSlotViewModel("Right stick ↔", "Right stick left/right", BindingSourceType.StickAxis, stickAxis: StickAxisRole.RightHorizontal));
        StickSlots.Add(new ControlSlotViewModel("Right stick ↕", "Right stick up/down", BindingSourceType.StickAxis, stickAxis: StickAxisRole.RightVertical));
    }

    private IEnumerable<ControlSlotViewModel> AllSlots() =>
        ButtonSlots.Concat(DpadSlots).Concat(StickSlots);

    private void ApplyFpsPresetToSlots()
    {
        void SetBtn(int n, string actionId)
        {
            var slot = ButtonSlots.FirstOrDefault(s => s.ButtonNumber == n);
            var action = GameActionChoice.Find(actionId);
            if (slot != null && action != null) slot.SelectedAction = action;
        }
        void SetHat(HatDirection h, string actionId)
        {
            var slot = DpadSlots.FirstOrDefault(s => s.Hat == h);
            var action = GameActionChoice.Find(actionId);
            if (slot != null && action != null) slot.SelectedAction = action;
        }
        void SetStick(StickAxisRole r, string actionId)
        {
            var slot = StickSlots.FirstOrDefault(s => s.StickAxis == r);
            var action = GameActionChoice.Find(actionId);
            if (slot != null && action != null) slot.SelectedAction = action;
        }

        foreach (var s in AllSlots())
            s.SelectedAction = GameActionChoice.All[0];

        SetBtn(1, "w");
        SetBtn(2, "d");
        SetBtn(3, "s");
        SetBtn(4, "a");
        SetBtn(5, "r");
        SetBtn(6, "q");
        SetBtn(7, "scope");
        SetBtn(8, "shoot");
        SetHat(HatDirection.Up, "space");
        SetHat(HatDirection.Right, "shift");
        SetHat(HatDirection.Down, "e");
        SetHat(HatDirection.Left, "ctrl");
        SetStick(StickAxisRole.LeftHorizontal, "look_x");
        SetStick(StickAxisRole.LeftVertical, "look_y");
    }

    private void SyncSlotsToProfile()
    {
        Profile.Remap.Bindings = AllSlots().Select(s => s.ToBinding()).ToList();
    }

    private void ApplyProfileToSlots()
    {
        foreach (var s in AllSlots())
            s.SelectedAction = GameActionChoice.All[0];

        foreach (var b in Profile.Remap.Bindings)
        {
            ControlSlotViewModel? slot = b.SourceType switch
            {
                BindingSourceType.Button => ButtonSlots.FirstOrDefault(x => x.ButtonNumber == b.ButtonNumber),
                BindingSourceType.Hat => DpadSlots.FirstOrDefault(x => x.Hat == b.Hat),
                BindingSourceType.StickAxis => StickSlots.FirstOrDefault(x => x.StickAxis == b.StickAxis),
                _ => null
            };
            slot?.ApplyBinding(b);
        }
    }

    private void EnsureDefaultStickAxes()
    {
        if (Profile.LeftStick.Horizontal is null) Profile.LeftStick.Horizontal = AxisKind.X;
        if (Profile.LeftStick.Vertical is null) Profile.LeftStick.Vertical = AxisKind.Y;
        if (Profile.RightStick.Horizontal is null) Profile.RightStick.Horizontal = AxisKind.Z;
        if (Profile.RightStick.Vertical is null) Profile.RightStick.Vertical = AxisKind.RotationZ;
    }

    [RelayCommand]
    private void MarkLeftStickPass() => LeftStickTest = TestResult.Pass;
    [RelayCommand]
    private void MarkLeftStickFail() => LeftStickTest = TestResult.Fail;
    [RelayCommand]
    private void MarkRightStickPass() => RightStickTest = TestResult.Pass;
    [RelayCommand]
    private void MarkRightStickFail() => RightStickTest = TestResult.Fail;
    [RelayCommand]
    private void MarkHatPass() => HatTest = TestResult.Pass;
    [RelayCommand]
    private void MarkHatFail() => HatTest = TestResult.Fail;
    [RelayCommand]
    private void MarkButtonsPass() => ButtonTest = TestResult.Pass;
    [RelayCommand]
    private void MarkButtonsFail() => ButtonTest = TestResult.Fail;

    [RelayCommand]
    private void FinishValidation()
    {
        BuildFinalSummary();
        WorkflowStatus = "Validation recorded. Save the profile when ready.";
    }

    private void BuildFinalSummary()
    {
        FinalSummary =
            $"CALIBRATION SUMMARY\n\nController: {Profile.Device.ProductName}\n" +
            $"VID:PID {Profile.Device.VendorId:X4}:{Profile.Device.ProductId:X4}\n\n" +
            $"LEFT STICK\n  Horizontal: {Profile.LeftStick.Horizontal?.ToDisplayName() ?? "—"}  " +
            $"{(Profile.LeftStick.HorizontalDetected ? "✓" : "⚠")}\n" +
            $"  Vertical:   {Profile.LeftStick.Vertical?.ToDisplayName() ?? "—"}  " +
            $"{(Profile.LeftStick.VerticalDetected ? "✓" : "⚠")}\n\n" +
            $"RIGHT STICK\n  Horizontal: {Profile.RightStick.Horizontal?.ToDisplayName() ?? "—"}  " +
            $"{(Profile.RightStick.HorizontalDetected ? "✓" : "⚠")}\n" +
            $"  Vertical:   {Profile.RightStick.Vertical?.ToDisplayName() ?? "—"}  " +
            $"{(Profile.RightStick.VerticalDetected ? "✓" : "⚠")}\n\n" +
            $"Dead zone L/R: {Profile.LeftStick.DeadZone:P0} / {Profile.RightStick.DeadZone:P0}\n" +
            $"Left stick test: {LeftStickTest}\nRight stick test: {RightStickTest}\n" +
            $"D-pad test: {HatTest}\nButtons test: {ButtonTest}\n\n" +
            $"Profile: {Profile.FriendlyName} - {Profile.ProfileName}\n\n" +
            "Software calibration cannot repair physically broken axes.";
    }

    private void TryAutoLoadProfile()
    {
        if (SelectedDevice is null) return;
        var loaded = _profiles.FindForDevice(SelectedDevice);
        if (loaded is null) return;
        Profile = loaded;
        ApplyProfileToSlots();
        NotifyStickMappings();
        ConnectionStatus = $"Controller connected. Loaded saved settings.";
    }

    private bool EnsureConnected()
    {
        if (IsConnected && SelectedDevice is not null) return true;
        MessageBox.Show("Select and connect a controller first.", "Gamepad Calibrator");
        return false;
    }

    private async Task<Dictionary<AxisKind, double>> SampleAveragesAsync(int samples)
    {
        var buffers = Enum.GetValues<AxisKind>().ToDictionary(a => a, _ => new List<double>());
        for (var i = 0; i < samples; i++)
        {
            var snap = _input.Poll();
            if (snap?.IsConnected == true)
            {
                foreach (var a in snap.Axes)
                    buffers[a.Axis].Add(a.Raw);
            }
            await Task.Delay(16);
        }
        return buffers.ToDictionary(kv => kv.Key, kv => AxisNormalizer.RobustCenter(kv.Value));
    }

    private async Task<(Dictionary<AxisKind, double> mins, Dictionary<AxisKind, double> maxs)> SampleRangeAsync(TimeSpan duration)
    {
        var mins = new Dictionary<AxisKind, double>();
        var maxs = new Dictionary<AxisKind, double>();
        var end = DateTime.UtcNow + duration;
        while (DateTime.UtcNow < end)
        {
            var snap = _input.Poll();
            if (snap?.IsConnected == true)
            {
                foreach (var a in snap.Axes)
                {
                    if (!mins.ContainsKey(a.Axis) || a.Raw < mins[a.Axis]) mins[a.Axis] = a.Raw;
                    if (!maxs.ContainsKey(a.Axis) || a.Raw > maxs[a.Axis]) maxs[a.Axis] = a.Raw;
                }
            }
            await Task.Delay(16);
        }
        foreach (AxisKind axis in Enum.GetValues<AxisKind>())
        {
            mins.TryAdd(axis, 0);
            maxs.TryAdd(axis, 0);
        }
        return (mins, maxs);
    }

    private void Poll()
    {
        var snap = _input.Poll();
        if (snap is null || !snap.IsConnected)
        {
            IsConnected = false;
            return;
        }
        IsConnected = true;

        // Axis table
        AxisRows.Clear();
        foreach (var a in snap.Axes)
        {
            var cal = _calibration.EnsureAxis(Profile, a.Axis);
            var norm = _calibration.EvaluateAxis(Profile, a.Axis, a.Raw);
            var assigned = DescribeAssignment(a.Axis);
            AxisRows.Add(new AxisRowViewModel
            {
                Name = a.Axis.ToDisplayName(),
                Axis = a.Axis,
                Raw = a.Raw,
                Center = cal.Center,
                Min = a.ObservedMin ?? cal.Minimum,
                Max = a.ObservedMax ?? cal.Maximum,
                Normalized = norm,
                DeadZone = cal.DeadZone,
                Invert = cal.Invert,
                Assigned = assigned
            });
        }

        // Buttons + highlight simple slots
        if (ButtonRows.Count != snap.Buttons.Count)
        {
            ButtonRows.Clear();
            for (var i = 0; i < snap.Buttons.Count; i++)
                ButtonRows.Add(new ButtonRowViewModel { Index = i + 1 });
            DetectedButtonCount = snap.Buttons.Count;
        }
        for (var i = 0; i < snap.Buttons.Count; i++)
        {
            ButtonRows[i].IsPressed = snap.Buttons[i];
            ButtonRows[i].Label = snap.Buttons[i] ? "PRESSED" : "Released";
            if (i < ButtonSlots.Count)
                ButtonSlots[i].IsPressed = snap.Buttons[i];
        }

        var hat = RemapEngine.DecodeHat(snap.Pov);
        foreach (var s in DpadSlots)
        {
            s.IsPressed = s.Hat switch
            {
                HatDirection.Up => hat.Up,
                HatDirection.Down => hat.Down,
                HatDirection.Left => hat.Left,
                HatDirection.Right => hat.Right,
                _ => false
            };
        }

        // Sticks
        var left = _calibration.EvaluateStick(Profile, Profile.LeftStick, snap);
        var right = _calibration.EvaluateStick(Profile, Profile.RightStick, snap);
        LeftNormX = left.NormalizedX;
        LeftNormY = left.NormalizedY;
        LeftRawX = left.RawX;
        LeftRawY = left.RawY;
        RightNormX = right.NormalizedX;
        RightNormY = right.NormalizedY;
        RightRawX = right.RawX;
        RightRawY = right.RawY;
        (LeftDotX, LeftDotY) = ToDot(left.NormalizedX, left.NormalizedY);
        (RightDotX, RightDotY) = ToDot(right.NormalizedX, right.NormalizedY);

        // Hat
        if (snap.Pov < 0 || snap.Pov == 65535)
        {
            HatLabel = "CENTER";
            HatX = 0;
            HatY = 0;
            HatMargin = new Thickness(0);
        }
        else
        {
            var ang = snap.Pov / 100.0 * Math.PI / 180.0;
            HatX = Math.Sin(ang);
            HatY = -Math.Cos(ang);
            HatLabel = DescribeHat(snap.Pov);
            HatMargin = new Thickness(HatX * 36, HatY * 36, 0, 0);
        }

        if (IsRemapActive && Profile.Remap.Enabled)
        {
            SyncSlotsToProfile();
            var frame = _remapEngine.Evaluate(Profile, snap);
            _emulator.Apply(frame);
        }
    }

    private static (double x, double y) ToDot(double nx, double ny)
    {
        const double size = 220;
        const double pad = 18;
        const double halfDot = 8;
        var usable = size - pad * 2;
        var mid = size / 2;
        return (mid + nx * (usable / 2) - halfDot, mid + ny * (usable / 2) - halfDot);
    }

    private string DescribeAssignment(AxisKind axis)
    {
        if (Profile.LeftStick.Horizontal == axis) return "Left Horizontal";
        if (Profile.LeftStick.Vertical == axis) return "Left Vertical";
        if (Profile.RightStick.Horizontal == axis) return "Right Horizontal";
        if (Profile.RightStick.Vertical == axis) return "Right Vertical";
        return "Unassigned";
    }

    private static string DescribeHat(int pov)
    {
        var deg = pov / 100;
        return deg switch
        {
            0 => "UP",
            45 => "UP-RIGHT",
            90 => "RIGHT",
            135 => "DOWN-RIGHT",
            180 => "DOWN",
            225 => "DOWN-LEFT",
            270 => "LEFT",
            315 => "UP-LEFT",
            _ => $"{deg}°"
        };
    }

    private void NotifyStickMappings()
    {
        OnPropertyChanged(nameof(Profile));
    }

    private void AppendLog(string line)
    {
        var text = $"{DateTime.Now:HH:mm:ss} {line}";
        LogLines.Insert(0, text);
        while (LogLines.Count > 200) LogLines.RemoveAt(LogLines.Count - 1);
        _log.LogInformation("{Message}", line);
    }
}

public partial class AxisRowViewModel : ObservableObject
{
    public string Name { get; set; } = "";
    public AxisKind Axis { get; set; }
    [ObservableProperty] private double _raw;
    [ObservableProperty] private double _center;
    [ObservableProperty] private double _min;
    [ObservableProperty] private double _max;
    [ObservableProperty] private double _normalized;
    [ObservableProperty] private double _deadZone;
    [ObservableProperty] private bool _invert;
    [ObservableProperty] private string _assigned = "";
}

public partial class ButtonRowViewModel : ObservableObject
{
    public int Index { get; set; }
    [ObservableProperty] private bool _isPressed;
    [ObservableProperty] private string _label = "Released";
}
