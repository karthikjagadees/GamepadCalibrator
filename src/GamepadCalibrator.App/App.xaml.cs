using System.IO;
using System.Windows;
using GamepadCalibrator.App.ViewModels;
using GamepadCalibrator.Core.Services;
using GamepadCalibrator.Infrastructure.Services;
using GamepadCalibrator.Infrastructure.Virtual;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GamepadCalibrator.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GamepadCalibrator", "Logs");
        Directory.CreateDirectory(logDir);

        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.Debug);
            b.AddDebug();
            b.AddProvider(new FileLoggerProvider(Path.Combine(logDir, "app.log")));
        });

        services.AddSingleton<IDeviceDiscoveryService, DeviceDiscoveryService>();
        services.AddSingleton<IInputService, WinmmInputService>();
        services.AddSingleton<ICalibrationService, CalibrationService>();
        services.AddSingleton<IMappingService, MappingService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IInputEmulator, GamepadCalibrator.Infrastructure.Input.SendInputEmulator>();
        services.AddSingleton<IVirtualGamepadBridge, NullVirtualGamepadBridge>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        _services = services.BuildServiceProvider();
        var window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}

file sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly object _lock = new();

    public FileLoggerProvider(string path) => _path = path;

    public ILogger CreateLogger(string categoryName) => new FileLogger(_path, categoryName, _lock);

    public void Dispose() { }
}

file sealed class FileLogger : ILogger
{
    private readonly string _path;
    private readonly string _category;
    private readonly object _lock;

    public FileLogger(string path, string category, object gate)
    {
        _path = path;
        _category = category;
        _lock = gate;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var line = $"{DateTimeOffset.Now:o} [{logLevel}] {_category}: {formatter(state, exception)}{Environment.NewLine}";
        lock (_lock)
            File.AppendAllText(_path, line);
    }
}
