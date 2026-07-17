param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [string]$PfxPath = ""
)

$ErrorActionPreference = "Stop"
$solutionDir = Split-Path $PSScriptRoot -Parent
$solutionFile = Join-Path $solutionDir "WinContainers.slnx"
$appProject = Join-Path $solutionDir "src\WinContainers.App\WinContainers.App.csproj"

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
dotnet build $buildTasksProject -c $Configuration --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "BuildTasks build failed" }

# 2. Restore + Build solution
Write-Host "--- Step 2: Building solution ---" -ForegroundColor Yellow
dotnet build $solutionFile -c $Configuration --nologo -v q
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
    --channel stable `
    @signArgs

if ($LASTEXITCODE -ne 0) { throw "Velopack pack failed" }

# 5. Create ISO containing the installer and portable package
Write-Host "--- Step 5: Creating ISO ---" -ForegroundColor Yellow
$oscdimgCandidates = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\x86\Oscdimg\oscdimg.exe"
)
$oscdimg = $oscdimgCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $oscdimg) {
    throw "oscdimg.exe was not found. Install the Windows ADK Deployment Tools to create an ISO."
}

$isoStagingDir = Join-Path $releaseDir "iso-staging"
$isoPath = Join-Path $releaseDir "WinContainers-$Version.iso"
if (Test-Path $isoStagingDir) {
    Remove-Item $isoStagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $isoStagingDir -Force | Out-Null

Copy-Item (Join-Path $releaseDir "WinContainers-stable-Setup.exe") `
    (Join-Path $isoStagingDir "WinContainers-Setup-$Version.exe")
Copy-Item (Join-Path $releaseDir "WinContainers-stable-Portable.zip") `
    (Join-Path $isoStagingDir "WinContainers-Portable-$Version.zip")

if (Test-Path $isoPath) {
    Remove-Item $isoPath -Force
}

& $oscdimg -m -o -u2 -udfver102 -l"WinContainers" $isoStagingDir $isoPath | Out-Null
if ($LASTEXITCODE -ne 0) { throw "ISO creation failed" }
Remove-Item $isoStagingDir -Recurse -Force

Write-Host ""
Write-Host "=== Release built successfully ===" -ForegroundColor Green
Write-Host "  Installer: $releaseDir\"
Get-ChildItem $releaseDir | ForEach-Object {
    Write-Host "    $($_.Name) ($([math]::Round($_.Length / 1MB, 1)) MB)"
}
