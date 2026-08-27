namespace GamepadCalibrator.Infrastructure.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using GamepadCalibrator.Core.Models;
using GamepadCalibrator.Core.Services;
using Microsoft.Extensions.Logging;

public sealed class ProfileService : IProfileService
{
    private readonly ILogger<ProfileService>? _log;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ProfilesDirectory { get; }

    public ProfileService(ILogger<ProfileService>? log = null, string? profilesDirectory = null)
    {
        _log = log;
        ProfilesDirectory = profilesDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GamepadCalibrator", "Profiles");
        Directory.CreateDirectory(ProfilesDirectory);
    }

    public IReadOnlyList<string> ListProfiles() =>
        Directory.Exists(ProfilesDirectory)
            ? Directory.GetFiles(ProfilesDirectory, "*.json").Select(Path.GetFileName).Where(f => f != null).Cast<string>().OrderBy(f => f).ToList()
            : Array.Empty<string>();

    public void Save(CalibrationProfile profile, string? fileName = null)
    {
        profile.UpdatedUtc = DateTimeOffset.UtcNow;
        fileName ??= Sanitize($"{profile.Device.StableKey}_{profile.ProfileName}.json");
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            fileName += ".json";
        var path = Path.Combine(ProfilesDirectory, fileName);
        var dto = ProfileDto.FromModel(profile);
        File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
        _log?.LogInformation("Saved profile {Path}", path);
    }

    public CalibrationProfile? Load(string fileName)
    {
        var path = Path.IsPathRooted(fileName) ? fileName : Path.Combine(ProfilesDirectory, fileName);
        if (!File.Exists(path)) return null;
        var dto = JsonSerializer.Deserialize<ProfileDto>(File.ReadAllText(path), JsonOptions);
        return dto?.ToModel();
    }

    public CalibrationProfile? FindForDevice(DeviceIdentity device)
    {
        foreach (var name in ListProfiles())
        {
            var p = Load(name);
            if (p != null && p.Device.SameHardware(device))
                return p;
        }
        return null;
    }

    public void Delete(string fileName)
    {
        var path = Path.Combine(ProfilesDirectory, fileName);
        if (File.Exists(path)) File.Delete(path);
    }

    public void Export(CalibrationProfile profile, string path)
    {
        var dto = ProfileDto.FromModel(profile);
        File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    public CalibrationProfile Import(string path)
    {
        var dto = JsonSerializer.Deserialize<ProfileDto>(File.ReadAllText(path), JsonOptions)
                  ?? throw new InvalidDataException("Invalid profile JSON");
        var model = dto.ToModel();
        Save(model);
        return model;
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}

file sealed class ProfileDto
{
    public string ProfileName { get; set; } = "Default";
    public string FriendlyName { get; set; } = "USB Gamepad";
    public DeviceDto Device { get; set; } = new();
    public StickDto LeftStick { get; set; } = new();
    public StickDto RightStick { get; set; } = new();
    public List<AxisDto> Axes { get; set; } = new();
    public RemapDto? Remap { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public string Notes { get; set; } = "";

    public static ProfileDto FromModel(CalibrationProfile p) => new()
    {
        ProfileName = p.ProfileName,
        FriendlyName = p.FriendlyName,
        Device = new DeviceDto
        {
            VendorId = p.Device.VendorId,
            ProductId = p.Device.ProductId,
            Name = p.Device.ProductName,
            Manufacturer = p.Device.Manufacturer,
            UsagePage = p.Device.UsagePage,
            Usage = p.Device.Usage,
            DevicePath = p.Device.DevicePath
        },
        LeftStick = StickDto.From(p.LeftStick),
        RightStick = StickDto.From(p.RightStick),
        Axes = p.Axes.Values.Select(AxisDto.From).ToList(),
        Remap = RemapDto.From(p.Remap),
        UpdatedUtc = p.UpdatedUtc,
        Notes = p.Notes
    };

    public CalibrationProfile ToModel()
    {
        var profile = new CalibrationProfile
        {
            ProfileName = ProfileName,
            FriendlyName = FriendlyName,
            Device = new DeviceIdentity
            {
                VendorId = Device.VendorId,
                ProductId = Device.ProductId,
                ProductName = Device.Name,
                Manufacturer = Device.Manufacturer,
                UsagePage = Device.UsagePage,
                Usage = Device.Usage,
                DevicePath = Device.DevicePath
            },
            LeftStick = LeftStick.ToModel(),
            RightStick = RightStick.ToModel(),
            Remap = Remap?.ToModel() ?? RemapSettings.CreateFpsCrazyGamesPreset(),
            UpdatedUtc = UpdatedUtc,
            Notes = Notes
        };
        foreach (var a in Axes)
            profile.Axes[a.Axis] = a.ToModel();
        return profile;
    }
}

file sealed class DeviceDto
{
    public int VendorId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? Manufacturer { get; set; }
    public int? UsagePage { get; set; }
    public int? Usage { get; set; }
    public string? DevicePath { get; set; }
}

file sealed class StickDto
{
    public AxisKind? Horizontal { get; set; }
    public AxisKind? Vertical { get; set; }
    public bool InvertHorizontal { get; set; }
    public bool InvertVertical { get; set; }
    public double DeadZone { get; set; } = 0.05;

    public static StickDto From(StickMapping m) => new()
    {
        Horizontal = m.Horizontal,
        Vertical = m.Vertical,
        InvertHorizontal = m.InvertHorizontal,
        InvertVertical = m.InvertVertical,
        DeadZone = m.DeadZone
    };

    public StickMapping ToModel() => new()
    {
        Horizontal = Horizontal,
        Vertical = Vertical,
        InvertHorizontal = InvertHorizontal,
        InvertVertical = InvertVertical,
        DeadZone = DeadZone
    };
}

file sealed class AxisDto
{
    public AxisKind Axis { get; set; }
    public double Center { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; }
    public double DeadZone { get; set; }
    public double AntiDeadZone { get; set; }
    public bool Invert { get; set; }
    public double Sensitivity { get; set; } = 1.0;
    public bool IsCalibrated { get; set; }

    public static AxisDto From(AxisCalibration c) => new()
    {
        Axis = c.Axis,
        Center = c.Center,
        Minimum = c.Minimum,
        Maximum = c.Maximum,
        DeadZone = c.DeadZone,
        AntiDeadZone = c.AntiDeadZone,
        Invert = c.Invert,
        Sensitivity = c.Sensitivity,
        IsCalibrated = c.IsCalibrated
    };

    public AxisCalibration ToModel() => new()
    {
        Axis = Axis,
        Center = Center,
        Minimum = Minimum,
        Maximum = Maximum,
        DeadZone = DeadZone,
        AntiDeadZone = AntiDeadZone,
        Invert = Invert,
        Sensitivity = Sensitivity,
        IsCalibrated = IsCalibrated
    };
}

file sealed class RemapDto
{
    public bool Enabled { get; set; }
    public double CameraSpeed { get; set; } = 22;
    public double StickDeadZone { get; set; } = 0.12;
    public List<BindingDto> Bindings { get; set; } = new();

    public static RemapDto From(RemapSettings s) => new()
    {
        Enabled = s.Enabled,
        CameraSpeed = s.CameraSpeed,
        StickDeadZone = s.StickDeadZone,
        Bindings = s.Bindings.Select(BindingDto.From).ToList()
    };

    public RemapSettings ToModel() => new()
    {
        Enabled = Enabled,
        CameraSpeed = CameraSpeed,
        StickDeadZone = StickDeadZone,
        Bindings = Bindings.Select(b => b.ToModel()).ToList()
    };
}

file sealed class BindingDto
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public BindingSourceType SourceType { get; set; }
    public int ButtonNumber { get; set; }
    public HatDirection Hat { get; set; }
    public StickAxisRole StickAxis { get; set; }
    public OutputActionType Output { get; set; }
    public string KeyName { get; set; } = "W";
    public bool Invert { get; set; }
    public bool Enabled { get; set; } = true;

    public static BindingDto From(ControlBinding b) => new()
    {
        Id = b.Id,
        Label = b.Label,
        SourceType = b.SourceType,
        ButtonNumber = b.ButtonNumber,
        Hat = b.Hat,
        StickAxis = b.StickAxis,
        Output = b.Output,
        KeyName = b.KeyName,
        Invert = b.Invert,
        Enabled = b.Enabled
    };

    public ControlBinding ToModel() => new()
    {
        Id = string.IsNullOrEmpty(Id) ? Guid.NewGuid().ToString("N") : Id,
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
