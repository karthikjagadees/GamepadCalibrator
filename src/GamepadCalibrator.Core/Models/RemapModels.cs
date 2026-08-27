namespace GamepadCalibrator.Core.Models;

/// <summary>Where an input comes from on the physical controller.</summary>
public enum BindingSourceType
{
    Button,
    Hat,
    StickAxis
}

public enum HatDirection
{
    Up,
    Down,
    Left,
    Right
}

public enum StickAxisRole
{
    LeftHorizontal,
    LeftVertical,
    RightHorizontal,
    RightVertical
}

/// <summary>What the binding outputs to the PC.</summary>
public enum OutputActionType
{
    None,
    Key,
    MouseLeft,
    MouseRight,
    MouseMiddle,
    MouseMoveX,
    MouseMoveY
}

/// <summary>One user-editable mapping from a stick/button/hat to a key or mouse action.</summary>
public sealed class ControlBinding
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = "";
    public BindingSourceType SourceType { get; set; } = BindingSourceType.Button;

    /// <summary>1-based button number as shown to the user.</summary>
    public int ButtonNumber { get; set; } = 1;

    public HatDirection Hat { get; set; } = HatDirection.Up;
    public StickAxisRole StickAxis { get; set; } = StickAxisRole.LeftHorizontal;

    public OutputActionType Output { get; set; } = OutputActionType.None;

    /// <summary>Key name when Output == Key (e.g. W, Space, Shift, R, Q, Ctrl).</summary>
    public string KeyName { get; set; } = "W";

    public bool Invert { get; set; }
    public bool Enabled { get; set; } = true;

    public ControlBinding Clone() => new()
    {
        Id = Id,
        Label = Label,
        SourceType = SourceType,
        ButtonNumber = ButtonNumber,
        Hat = Hat,
        StickAxis = StickAxis,
        Output = Output,
        KeyName = KeyName,
        Invert = Invert,
        Enabled = Enabled
    };
}

public sealed class RemapSettings
{
    public bool Enabled { get; set; }
    public double CameraSpeed { get; set; } = 22.0;
    public double StickDeadZone { get; set; } = 0.12;
    public List<ControlBinding> Bindings { get; set; } = new();

    public RemapSettings Clone() => new()
    {
        Enabled = Enabled,
        CameraSpeed = CameraSpeed,
        StickDeadZone = StickDeadZone,
        Bindings = Bindings.Select(b => b.Clone()).ToList()
    };

    /// <summary>Preset matching the CrazyGames FPS layout the user configured.</summary>
    public static RemapSettings CreateFpsCrazyGamesPreset() => new()
    {
        Enabled = false,
        CameraSpeed = 22,
        StickDeadZone = 0.12,
        Bindings =
        {
            new() { Label = "Camera look X", SourceType = BindingSourceType.StickAxis, StickAxis = StickAxisRole.LeftHorizontal, Output = OutputActionType.MouseMoveX },
            new() { Label = "Camera look Y", SourceType = BindingSourceType.StickAxis, StickAxis = StickAxisRole.LeftVertical, Output = OutputActionType.MouseMoveY },
            new() { Label = "Move forward", SourceType = BindingSourceType.Button, ButtonNumber = 1, Output = OutputActionType.Key, KeyName = "W" },
            new() { Label = "Move right", SourceType = BindingSourceType.Button, ButtonNumber = 2, Output = OutputActionType.Key, KeyName = "D" },
            new() { Label = "Move back", SourceType = BindingSourceType.Button, ButtonNumber = 3, Output = OutputActionType.Key, KeyName = "S" },
            new() { Label = "Move left", SourceType = BindingSourceType.Button, ButtonNumber = 4, Output = OutputActionType.Key, KeyName = "A" },
            new() { Label = "Jump", SourceType = BindingSourceType.Hat, Hat = HatDirection.Up, Output = OutputActionType.Key, KeyName = "Space" },
            new() { Label = "Sprint", SourceType = BindingSourceType.Hat, Hat = HatDirection.Right, Output = OutputActionType.Key, KeyName = "Shift" },
            new() { Label = "Interact", SourceType = BindingSourceType.Hat, Hat = HatDirection.Down, Output = OutputActionType.Key, KeyName = "E" },
            new() { Label = "Crouch", SourceType = BindingSourceType.Hat, Hat = HatDirection.Left, Output = OutputActionType.Key, KeyName = "Ctrl" },
            new() { Label = "Reload", SourceType = BindingSourceType.Button, ButtonNumber = 5, Output = OutputActionType.Key, KeyName = "R" },
            new() { Label = "Change weapon", SourceType = BindingSourceType.Button, ButtonNumber = 6, Output = OutputActionType.Key, KeyName = "Q" },
            new() { Label = "Scope", SourceType = BindingSourceType.Button, ButtonNumber = 7, Output = OutputActionType.MouseRight },
            new() { Label = "Shoot", SourceType = BindingSourceType.Button, ButtonNumber = 8, Output = OutputActionType.MouseLeft },
        }
    };
}
