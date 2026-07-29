param(
    [string]$Configuration = "Release",
    [string]$Version = "0.0.1",
    [string]$PfxPath = "",
    [ValidateSet("Stable", "Beta")]
    [string]$Channel = "Stable",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$solutionDir = Split-Path $PSScriptRoot -Parent
$solutionFile = Join-Path $solutionDir "WinContainers.slnx"
$appProject = Join-Path $solutionDir "src\WinContainers.App\WinContainers.App.csproj"
$channelName = $Channel.ToLowerInvariant()

# Clean up any previously published WinContainers process that could lock build output
Get-Process -Name "WinContainers.App" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  Stopping running WinContainers.App process (PID $($_.Id))..." -ForegroundColor Yellow
    $_.Kill()
}

# Default PFX path if not specified
if (-not $PfxPath) {
    $PfxPath = Join-Path $PSScriptRoot "WinContainers-dev.pfx"
}

Write-Host "=== Building WinContainers v$Version ===" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration"
Write-Host "  Solution dir:  $solutionDir"
Write-Host ""

# 1. Build BuildTasks first (custom MSBuild task required by App project)
Write-Host "--- Step 1: Building BuildTasks ---" -ForegroundColor Yellow
$buildTasksProject = Join-Path $solutionDir "src\BuildTasks\BuildTasks.csproj"
dotnet build $buildTasksProject -c $Configuration -p:UseSharedCompilation=false --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "BuildTasks build failed" }

# 2. Build the app project. BuildTasks was built separately so the custom task
# assembly is not rebuilt while the app project loads it.
Write-Host "--- Step 2: Building app ---" -ForegroundColor Yellow
dotnet build $appProject -c $Configuration -p:UseSharedCompilation=false -p:Version=$Version -p:InformationalVersion=$Version --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# 3. Publish self-contained folder (required for reliable WinUI 3 unpackaged deployment)
Write-Host "--- Step 3: Publishing self-contained folder ---" -ForegroundColor Yellow
$publishDir = Join-Path $solutionDir "publish\win-x64"
# publish/ is generated output and ignored by git; release/ is the distributable output.

# Clean previous publish
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $appProject `
    -c $Configuration `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -p:Version=$Version `
    -p:InformationalVersion=$Version `
    -p:BuildProjectReferences=false `
    -p:UseSharedCompilation=false `
    -o $publishDir `
    --nologo -v q

if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

$exePath = Join-Path $publishDir "WinContainers.App.exe"
if (-not (Test-Path $exePath)) { throw "Published EXE not found at $exePath" }

$exeSize = (Get-Item $exePath).Length / 1MB
Write-Host "  Published: $exePath" -ForegroundColor Green
Write-Host "  EXE size:  $([math]::Round($exeSize, 1)) MB"
Write-Host "  Publish:   folder deployment (Windows App SDK native files preserved)"

# 4. Pack with Velopack
Write-Host "--- Step 4: Packing with Velopack ---" -ForegroundColor Yellow

# Find or install vpk
$vpk = Get-Command vpk -ErrorAction SilentlyContinue
if (-not $vpk) {
    $vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
    if (Test-Path $vpkPath) {
        $vpk = $vpkPath
    } else {
        Write-Host "  Installing Velopack CLI..." -ForegroundColor Yellow
        dotnet tool install -g vpk
        if ($LASTEXITCODE -ne 0) { throw "Failed to install Velopack CLI" }
        $vpk = "vpk"
    }
} else {
    $vpk = $vpk.Source
}

$releaseDir = Join-Path $solutionDir "release"
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

if ($Force) {
    Write-Host "  Clearing generated $channelName Velopack state before packing" -ForegroundColor Yellow
    Get-ChildItem $releaseDir -File | Where-Object {
        $_.Name -in @("assets.$channelName.json", "releases.$channelName.json", "RELEASES-$channelName") -or
        $_.Name -match "^WinContainers-$channelName-" -or
        $_.Name -match "^WinContainers-[0-9].*-$channelName-(full|delta)\.nupkg$"
    } | Remove-Item -Force
}

$signArgs = @()
if (Test-Path $PfxPath) {
    Write-Host "  Signing with: $PfxPath" -ForegroundColor Yellow
    $signArgs = @("--signParams", "sign /fd SHA256 /a /f `"$PfxPath`" /p WinContainers-dev")
} else {
    Write-Host "  No PFX found at $PfxPath - building unsigned" -ForegroundColor Yellow
    Write-Host "  Run: pwsh tools/generate-cert.ps1" -ForegroundColor Yellow
}

& $vpk pack `
    --packVersion $Version `
    --packId WinContainers `
    --mainExe WinContainers.App.exe `
    --packDir $publishDir `
    --outputDir $releaseDir `
    --channel $channelName `
    @signArgs

if ($LASTEXITCODE -ne 0) { throw "Velopack pack failed" }

# 5. Wrap the Velopack setup so it can close a running WinContainers process
# before Velopack renames the installed application directory.
Write-Host "--- Step 5: Building installer bootstrapper ---" -ForegroundColor Yellow
$setupPath = Join-Path $releaseDir "WinContainers-$channelName-Setup.exe"
$payloadPath = Join-Path $releaseDir "WinContainers-$channelName-Setup.payload.exe"
$bootstrapperProject = Join-Path $solutionDir "tools\InstallerBootstrapper\InstallerBootstrapper.csproj"
$bootstrapperDir = Join-Path $solutionDir "publish\InstallerBootstrapper"

Move-Item $setupPath $payloadPath -Force
if (Test-Path $bootstrapperDir) {
    Remove-Item $bootstrapperDir -Recurse -Force
}

dotnet publish $bootstrapperProject `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:BootstrapPayloadPath=$payloadPath `
    -p:UseSharedCompilation=false `
    -o $bootstrapperDir `
    --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Installer bootstrapper build failed" }

Copy-Item (Join-Path $bootstrapperDir "InstallerBootstrapper.exe") $setupPath -Force
Remove-Item $payloadPath -Force

# 6. Clean up dotnet build server processes (MSBuild node reuse leaves dotnet.exe alive)
Write-Host "--- Step 6: Cleaning up build server processes ---" -ForegroundColor Yellow
dotnet build-server shutdown *> $null
Write-Host "  Build servers shut down."

Write-Host ""
Write-Host "=== Release built successfully ===" -ForegroundColor Green
Write-Host "  Installer: $releaseDir\"
Get-ChildItem $releaseDir | ForEach-Object {
    Write-Host "    $($_.Name) ($([math]::Round($_.Length / 1MB, 1)) MB)"
}
