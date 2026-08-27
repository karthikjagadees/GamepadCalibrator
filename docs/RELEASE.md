# Release / build instructions

```powershell
cd $env:USERPROFILE\GamepadCalibrator
dotnet restore GamepadCalibrator.sln
dotnet test GamepadCalibrator.sln -c Release
dotnet publish src\GamepadCalibrator.App\GamepadCalibrator.App.csproj -c Release -r win-x64 --self-contained false -o .\publish
```

Output: `publish\GamepadCalibrator.App.exe`

For a self-contained single-folder distribution:

```powershell
dotnet publish src\GamepadCalibrator.App\GamepadCalibrator.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish-sc
```

Sign binaries with your organization certificate for production distribution (optional).
