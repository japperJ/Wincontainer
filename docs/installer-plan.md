# WinContainers Installer Plan

## Goal

Make WinContainers installable on any clean Windows 11 PC (no dev tools, no .NET SDK, no WSL).

## Decisions

| Decision | Choice |
|----------|--------|
| Target | Fresh Windows 11, no dev tools |
| Installer | Velopack (per-user) |
| Publish | Self-contained folder, untrimmed; Velopack packages the folder |
| Prerequisites | Onboarding wizard (WSL2 + WSLC detection + install) |
| Code signing | Self-signed `.pfx` (PowerShell-generated) |
| Installer scope | App + prerequisite scripts |
| Install path | Velopack default (`%LOCALAPPDATA%\Velopack\`) |
| Auto-update | GitHub Releases |

## Current State

- **Publish**: Folder of ~1,100 files (~266 MB), not single-file
- **Csproj**: Has `--self-contained` in publish command, but `PublishSingleFile` NOT in csproj. `PublishTrimmed=true` for Release.
- **Velopack**: Not integrated
- **Onboarding wizard**: Does not exist
- **Self-signed cert**: Does not exist
- **Build script**: Does not exist

## Steps

### Step 1: Configure self-contained WinUI deployment

**File**: `src/WinContainers.App/WinContainers.App.csproj`

In "Publish Properties" `<PropertyGroup>` (line 104-110), add:
```xml
<PublishSingleFile>false</PublishSingleFile>
<SelfContained>true</SelfContained>
<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
<WindowsAppSdkBootstrapInitialize>false</WindowsAppSdkBootstrapInitialize>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

Change `PublishTrimmed` from `true` to `false` for Release (line 109):
```xml
<PublishTrimmed Condition="'$(Configuration)' != 'Debug'">False</PublishTrimmed>
```

**Why**: WinUI 3 and Windows App SDK native components are more reliable when preserved as a publish folder. Velopack still produces a single installer and portable archive. Trimming breaks WinUI 3.

### Step 2: Create self-signed certificate

**New file**: `tools/generate-cert.ps1`

```powershell
$cert = New-SelfSignedCertificate `
    -DnsName "WinContainers" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Type CodeSigningCert

$password = ConvertTo-SecureString -String "WinContainers-dev" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "$PSScriptRoot\WinContainers-dev.pfx" -Password $password

Write-Host "Certificate created. Thumbprint: $($cert.Thumbprint)"
Write-Host "PFX saved to: tools\WinContainers-dev.pfx"
```

Add `tools/WinContainers-dev.pfx` and `*.pfx` to `.gitignore`.

### Step 3: Create build-release.ps1

**New file**: `tools/build-release.ps1`

```powershell
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [string]$PfxPath = "$PSScriptRoot\WinContainers-dev.pfx"
)

$ErrorActionPreference = "Stop"
$solutionDir = "$PSScriptRoot\..\"
$appProject = "$solutionDir\src\WinContainers.App\WinContainers.App.csproj"

Write-Host "=== Building WinContainers v$Version ===" -ForegroundColor Cyan

# 1. Restore + Build
dotnet build $solutionDir -c $Configuration --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# 2. Publish single-file self-contained
$publishDir = "$solutionDir\publish\win-x64"
dotnet publish $appProject `
    -c $Configuration `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o $publishDir `
    --nologo -v q

if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

Write-Host "Published to: $publishDir" -ForegroundColor Green
Write-Host "Single EXE: $publishDir\WinContainers.App.exe"

# 3. Pack with Velopack
$vpk = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
if (-not (Test-Path $vpk)) {
    Write-Host "Installing Velopack CLI..." -ForegroundColor Yellow
    dotnet tool install -g velopack
    $vpk = "vpk"
}

& $vpk pack `
    --appVersion $Version `
    --appId WinContainers `
    --appExe WinContainers.App.exe `
    --packDir $publishDir `
    --outputDir "$solutionDir\release" `
    --channel stable `
    $(if (Test-Path $PfxPath) { "--sign $PfxPath" })

if ($LASTEXITCODE -ne 0) { throw "Velopack pack failed" }

Write-Host "=== Release built successfully ===" -ForegroundColor Green
Write-Host "Installer: $solutionDir\release\"
```

### Step 4: Add Velopack to the app

**`Directory.Packages.props`**: Add:
```xml
<PackageVersion Include="Velopack" Version="0.0.1041" />
```

**`WinContainers.App.csproj`**: Add:
```xml
<PackageReference Include="Velopack" />
```

**New file**: `src/WinContainers.App/UpdateService.cs`

```csharp
using Velopack;

namespace WinContainers_App;

public static class UpdateService
{
    public static void CheckForUpdates()
    {
        var updateManager = new UpdateManager(
            new GithubSource("https://github.com/YOUR_USER/WinContainers"));

        var newVersion = updateManager.CheckForUpdates();
        if (newVersion != null)
        {
            updateManager.DownloadUpdates(newVersion);
            updateManager.ApplyUpdates(newVersion);
        }
    }
}
```

**Modify** `App.xaml.cs` `OnLaunched`: call `UpdateService.CheckForUpdates()` on background thread before showing main window.

### Step 5: Create prerequisite check scripts

**New files**:
- `tools/prereqs/check-wsl2.ps1`
- `tools/prereqs/check-wslc.ps1`
- `tools/prereqs/install-wsl2.ps1`
- `tools/prereqs/install-wslc.ps1`

These scripts get embedded as resources in the App project and extracted to `%APPDATA%\WinContainers\Scripts\` on first run.

### Step 6: Build the onboarding wizard

**New files**:
- `src/WinContainers.App/Pages/OnboardingPage.xaml` + `.cs`
- `src/WinContainers.App/ViewModels/OnboardingViewModel.cs`

**Onboarding flow**:
1. On first launch (check `%LOCALAPPDATA%\WinContainers\.first-run-complete`), show OnboardingPage
2. Four status cards: WSL2, WSLC, Virtualization, Windows version
3. Each missing card has "Install" button running corresponding script
4. "Verify All" re-checks everything
5. "Continue to App" enabled when WSL2 + WSLC pass, writes `.first-run-complete`

**Modify** `App.xaml.cs`: Check for `.first-run-complete` before creating MainWindow.

### Step 7: Update .gitignore

Add:
```
tools/WinContainers-dev.pfx
*.pfx
release/
```

### Step 8: Update AGENTS.md

Add "Release Build" section.

## Dependency Order

| Step | Depends on | Blocks |
|------|-----------|--------|
| 1 (csproj) | — | 3, 4 |
| 2 (cert) | — | 3 |
| 3 (build script) | 1, 2 | — |
| 4 (Velopack in app) | 1 | — |
| 5 (prereq scripts) | — | 6 |
| 6 (onboarding wizard) | 5 | — |
| 7 (.gitignore) | — | — |
| 8 (AGENTS.md) | 3 | — |

## Verification

```powershell
pwsh tools/build-release.ps1 -Version 1.0.0
```

Expected:
- `publish\win-x64\WinContainers.App.exe` — single file, ~110-130 MB
- `release\WinContainers-1.0.0-full.nupkg` — Velopack package
- `release\WinContainers-Setup.exe` — installer

Test on clean Windows 11 VM:
1. Run `WinContainers-Setup.exe`
2. App installs to `%LOCALAPPDATA%\Velopack\`
3. Onboarding wizard shows WSL2/WSLC status
4. Click "Install" → WSL2/WSLC installed
5. Click "Continue" → Dashboard loads
6. Quit + re-run installer → auto-updates
