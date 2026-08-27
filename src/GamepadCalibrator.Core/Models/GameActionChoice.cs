namespace GamepadCalibrator.Core.Models;

/// <summary>Friendly actions a normal user can pick from a dropdown.</summary>
public sealed class GameActionChoice
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public OutputActionType Output { get; init; }
    public string KeyName { get; init; } = "";
    public bool InvertAxis { get; init; }

    public string Display => string.IsNullOrEmpty(Description) ? Name : $"{Name}  —  {Description}";

    public static IReadOnlyList<GameActionChoice> All { get; } = Build();

    public static GameActionChoice? Find(string? id) =>
        All.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));

    public static GameActionChoice FromBinding(ControlBinding b)
    {
        foreach (var a in All)
        {
            if (a.Output != b.Output) continue;
            if (a.Output == OutputActionType.Key &&
                !string.Equals(a.KeyName, b.KeyName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (a.Output is OutputActionType.MouseMoveX or OutputActionType.MouseMoveY &&
                a.InvertAxis != b.Invert)
                continue;
            return a;
        }
        return All[0]; // Do nothing
    }

    private static List<GameActionChoice> Build() =>
    [
        new() { Id = "none", Name = "Do nothing", Description = "Not used", Output = OutputActionType.None },
        new() { Id = "shoot", Name = "Shoot", Description = "Left mouse click", Output = OutputActionType.MouseLeft },
        new() { Id = "scope", Name = "Scope / Aim", Description = "Right mouse click", Output = OutputActionType.MouseRight },
        new() { Id = "middle", Name = "Middle mouse", Description = "Mouse wheel button", Output = OutputActionType.MouseMiddle },
        new() { Id = "look_x", Name = "Look left / right", Description = "Move mouse sideways", Output = OutputActionType.MouseMoveX },
        new() { Id = "look_y", Name = "Look up / down", Description = "Move mouse up/down", Output = OutputActionType.MouseMoveY },
        new() { Id = "look_y_inv", Name = "Look up / down (inverted)", Description = "Mouse Y flipped", Output = OutputActionType.MouseMoveY, InvertAxis = true },
        new() { Id = "w", Name = "Move forward", Description = "W", Output = OutputActionType.Key, KeyName = "W" },
        new() { Id = "a", Name = "Move left", Description = "A", Output = OutputActionType.Key, KeyName = "A" },
        new() { Id = "s", Name = "Move back", Description = "S", Output = OutputActionType.Key, KeyName = "S" },
        new() { Id = "d", Name = "Move right", Description = "D", Output = OutputActionType.Key, KeyName = "D" },
        new() { Id = "space", Name = "Jump", Description = "Space", Output = OutputActionType.Key, KeyName = "Space" },
        new() { Id = "shift", Name = "Sprint", Description = "Shift", Output = OutputActionType.Key, KeyName = "Shift" },
        new() { Id = "ctrl", Name = "Crouch", Description = "Ctrl", Output = OutputActionType.Key, KeyName = "Ctrl" },
        new() { Id = "e", Name = "Use / Interact", Description = "E", Output = OutputActionType.Key, KeyName = "E" },
        new() { Id = "r", Name = "Reload", Description = "R", Output = OutputActionType.Key, KeyName = "R" },
        new() { Id = "q", Name = "Change weapon", Description = "Q", Output = OutputActionType.Key, KeyName = "Q" },
        new() { Id = "f", Name = "Action F", Description = "F", Output = OutputActionType.Key, KeyName = "F" },
        new() { Id = "c", Name = "Action C", Description = "C", Output = OutputActionType.Key, KeyName = "C" },
        new() { Id = "v", Name = "Action V", Description = "V", Output = OutputActionType.Key, KeyName = "V" },
        new() { Id = "enter", Name = "Enter / Confirm", Description = "Enter", Output = OutputActionType.Key, KeyName = "Enter" },
        new() { Id = "esc", Name = "Menu / Escape", Description = "Esc", Output = OutputActionType.Key, KeyName = "Esc" },
        new() { Id = "tab", Name = "Map / Tab", Description = "Tab", Output = OutputActionType.Key, KeyName = "Tab" },
        new() { Id = "1", Name = "Weapon 1", Description = "1", Output = OutputActionType.Key, KeyName = "1" },
        new() { Id = "2", Name = "Weapon 2", Description = "2", Output = OutputActionType.Key, KeyName = "2" },
        new() { Id = "3", Name = "Weapon 3", Description = "3", Output = OutputActionType.Key, KeyName = "3" },
        new() { Id = "up", Name = "Arrow Up", Description = "Up", Output = OutputActionType.Key, KeyName = "Up" },
        new() { Id = "down", Name = "Arrow Down", Description = "Down", Output = OutputActionType.Key, KeyName = "Down" },
        new() { Id = "left", Name = "Arrow Left", Description = "Left", Output = OutputActionType.Key, KeyName = "Left" },
        new() { Id = "right", Name = "Arrow Right", Description = "Right", Output = OutputActionType.Key, KeyName = "Right" },
    ];
}
