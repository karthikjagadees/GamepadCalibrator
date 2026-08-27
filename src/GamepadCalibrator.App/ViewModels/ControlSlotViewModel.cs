using CommunityToolkit.Mvvm.ComponentModel;
using GamepadCalibrator.Core.Models;

namespace GamepadCalibrator.App.ViewModels;

/// <summary>One simple row: "Button 8" → pick what it does.</summary>
public partial class ControlSlotViewModel : ObservableObject
{
    public ControlSlotViewModel(
        string title,
        string hint,
        BindingSourceType sourceType,
        int buttonNumber = 1,
        HatDirection hat = HatDirection.Up,
        StickAxisRole stickAxis = StickAxisRole.LeftHorizontal,
        GameActionChoice? initial = null)
    {
        Title = title;
        Hint = hint;
        SourceType = sourceType;
        ButtonNumber = buttonNumber;
        Hat = hat;
        StickAxis = stickAxis;
        _selectedAction = initial ?? GameActionChoice.All[0];
    }

    public string Title { get; }
    public string Hint { get; }
    public BindingSourceType SourceType { get; }
    public int ButtonNumber { get; }
    public HatDirection Hat { get; }
    public StickAxisRole StickAxis { get; }

    public IReadOnlyList<GameActionChoice> Actions { get; } = GameActionChoice.All;

    [ObservableProperty] private GameActionChoice _selectedAction;
    [ObservableProperty] private bool _isPressed;

    public ControlBinding ToBinding() => new()
    {
        Label = Title,
        SourceType = SourceType,
        ButtonNumber = ButtonNumber,
        Hat = Hat,
        StickAxis = StickAxis,
        Output = SelectedAction.Output,
        KeyName = SelectedAction.KeyName,
        Invert = SelectedAction.InvertAxis,
        Enabled = SelectedAction.Output != OutputActionType.None
    };

    public void ApplyBinding(ControlBinding? b)
    {
        if (b is null)
        {
            SelectedAction = GameActionChoice.All[0];
            return;
        }
        SelectedAction = GameActionChoice.FromBinding(b);
    }
}
