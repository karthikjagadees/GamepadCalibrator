using System.Windows;
using GamepadCalibrator.App.ViewModels;

namespace GamepadCalibrator.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
