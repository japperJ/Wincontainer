[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Tag,
    [string]$OutputDirectory = (Join-Path (Get-Location) "dist\release-$Tag"),
    [switch]$Publish,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
# Strict SemVer: numeric identifiers must not carry leading zeros. A tag such as
# v0.2.00-beta.1a is normalized to 0.2.0-beta.1a by vpk, which makes the packed
# asset names disagree with the version the updater derives from the tag.
$strictNumber = '0|[1-9]\d*'
$semverPattern = "($strictNumber)\.($strictNumber)\.($strictNumber)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?"
if ($Tag -notmatch "^v$semverPattern$") {
    throw "Tag must be a normalized SemVer value without leading zeros: $Tag"
}
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw "GitHub CLI (gh) is required." }
& gh auth status *> $null
if ($LASTEXITCODE -ne 0) { throw "GitHub CLI authentication is required. Run gh auth login first." }

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$channel = if ($Tag.Contains("-")) { "Beta" } else { "Stable" }
$isPrerelease = $channel -eq "Beta"
$version = $Tag.Substring(1)
$output = [System.IO.Path]::GetFullPath($OutputDirectory)

& gh release view $Tag *> $null
if ($LASTEXITCODE -eq 0) { throw "A GitHub release already exists for $Tag." }
if (Test-Path $output) {
    if (@(Get-ChildItem $output -Force).Count -gt 0 -and -not $Force) { throw "Output directory is not empty: $output" }
    if ($Force) { Remove-Item $output -Recurse -Force }
}
New-Item -ItemType Directory -Path $output -Force | Out-Null

$buildParams = @{
    Version = $version
    Channel = $channel
}
if ($Force) { $buildParams.Force = $true }
& (Join-Path $root "tools\build-release.ps1") @buildParams
if ($LASTEXITCODE -ne 0) { throw "Release build failed." }
$releaseDir = Join-Path $root "release"
Copy-Item (Join-Path $root "update-policy.json") $releaseDir -Force
Copy-Item (Join-Path $releaseDir "*") $output -Recurse -Force

$channelName = $channel.ToLowerInvariant()
$assetManifestPath = Join-Path $output "assets.$channelName.json"
if (-not (Test-Path $assetManifestPath)) { throw "Missing Velopack asset manifest: $assetManifestPath" }
$declared = @(Get-Content $assetManifestPath -Raw | ConvertFrom-Json)
if ($declared.Count -eq 0) { throw "Velopack asset manifest is empty: $assetManifestPath" }

# vpk writes the exact file names the updater needs into assets.<channel>.json.
# Select from that manifest instead of matching names against the version string:
# vpk normalizes versions, so a name-based filter silently drops the update
# packages and ships a release the in-app updater can never install.
$full = @($declared | Where-Object { $_.Type -eq "Full" })
if ($full.Count -ne 1) { throw "Velopack asset manifest must declare exactly one Full package; found $($full.Count)." }
$expectedFull = "WinContainers-$version-$channelName-full.nupkg"
if ($full[0].RelativeFileName -ne $expectedFull) {
    throw "Velopack packed the full update package as '$($full[0].RelativeFileName)' but the tag requires '$expectedFull'. Retag with a normalized SemVer version before publishing."
}

$declaredNames = @($declared | ForEach-Object { $_.RelativeFileName })
$notProduced = @($declaredNames | Where-Object { -not (Test-Path (Join-Path $output $_)) })
if ($notProduced.Count -gt 0) { throw "Velopack declared assets that were not produced: $($notProduced -join ', ')." }

$assets = @(foreach ($name in @($declaredNames + @("releases.$channelName.json", "RELEASES-$channelName", "assets.$channelName.json"))) {
    $path = Join-Path $output $name
    if (-not (Test-Path $path)) { throw "Missing release asset: $path" }
    Get-Item $path
})

# A package for a different version or channel must never reach a release
# unannounced; fail instead of publishing an updater that cannot install.
$stray = @(Get-ChildItem $output -File | Where-Object {
    $_.Extension -in @(".nupkg", ".zip", ".exe") -and $_.Name -notin $assets.Name
})
if ($stray.Count -gt 0) { throw "Release output contains artifacts that would not be published: $($stray.Name -join ', ')." }
$checksumEntries = foreach ($asset in $assets) {
    $hash = (Get-FileHash $asset.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    [ordered]@{ name = $asset.Name; sizeBytes = $asset.Length; sha256 = $hash }
}
$checksumPath = Join-Path $output "WinContainers-$Tag-checksums.json"
[ordered]@{ schemaVersion = 1; releaseTag = $Tag; algorithm = "sha256"; assets = @($checksumEntries) } |
    ConvertTo-Json -Depth 5 | Set-Content $checksumPath -Encoding utf8NoBOM
& (Join-Path $root "tools\test-release-contracts.ps1") -PolicyPath (Join-Path $output "update-policy.json") -ChecksumPath $checksumPath
if ($LASTEXITCODE -ne 0) { throw "Release contract validation failed." }

$remoteTagExists = $false
& git ls-remote --exit-code --quiet origin "refs/tags/$Tag" *> $null
if ($LASTEXITCODE -eq 0) {
    $remoteTagExists = $true
}
if (-not $remoteTagExists) {
    & git show-ref --verify --quiet "refs/tags/$Tag" *> $null
    if ($LASTEXITCODE -ne 0) {
        & git tag -a $Tag -m "Release $Tag"
        if ($LASTEXITCODE -ne 0) { throw "Unable to create local tag $Tag." }
    }
    & git push origin "refs/tags/$Tag"
    if ($LASTEXITCODE -ne 0) { throw "Unable to push tag $Tag." }
}

$releaseArgs = @("release", "create", $Tag, "--verify-tag", "--title", "$Tag $channel", "--generate-notes")
if (-not $Publish) { $releaseArgs += "--draft" }
if ($isPrerelease) { $releaseArgs += "--prerelease" }
$releaseArgs += @($assets.FullName, $checksumPath, (Join-Path $output "update-policy.json"))
& gh @releaseArgs
if ($LASTEXITCODE -ne 0) { throw "GitHub release creation failed." }
