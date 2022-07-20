
# Development Pre-reqs

Only the .NET 6 SDK is required to compile the project.  This can be installed through one of the following methods

## Using Chocolatey
```powershell
choco install dotnet-6.0-sdk
# Needs to be removed, in order to resolve issue with Nuget being preconfigured wrong.  Will 
# auto-regenerate on first run.
Remove-Item "C:\Users\$Env:USERNAME\AppData\Roaming\NuGet\nuget.config"
```

## Manually
The latest .NET 6.0 SDK can be found here : [.NET 6.0 SDK - Windows x64 Installer]( https://download.visualstudio.microsoft.com/download/pr/deb4711b-7bbc-4afa-8884-9f2b964797f2/fb603c451b2a6e0a2cb5372d33ed68b9/dotnet-sdk-6.0.300-win-x64.exe )

# Compiling

The project can be compiled by running the following in the repository root (the directory with the .sln file).  This will generate an .exe that can be run locally.  Subsequent `dotnet build` commands will perform incremental compilation.

```powershell
dotnet build
```

# Running the project

Typically, for development you will want to run the project in `Debug` mode.  This mode will run dramatically slower than `Release`, however it will leave useful debugging information in the compiled assembly.  Running the following will detect and changes, and both `build` and `run` the project :
```powershell
dotnet run --project .\BattleNetPrefill\BattleNetPrefill.csproj
```

Alternatively, to run the project at full speed with all compilation optimizations enabled, add the additional `--configuration Release` flag:
```powershell
dotnet run --project .\BattleNetPrefill\BattleNetPrefill.csproj --configuration Release
```

# Executing Unit Tests

To compile and run all tests in the entire repo, run the following command:
```powershell
dotnet test
```
