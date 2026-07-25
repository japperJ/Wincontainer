[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Tag,
    [string]$OutputDirectory = (Join-Path (Get-Location) "dist\release-$Tag"),
    [switch]$Publish,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
if ($Tag -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') { throw "Tag must be SemVer: $Tag" }
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

$assets = @(Get-ChildItem $output -File | Where-Object {
    $_.Name -notmatch "\.iso$" -and (
    $_.Name -match "^WinContainers-$version(?:[-.]|$)" -or
    $_.Name -in @("WinContainers-$($channel.ToLowerInvariant())-Setup.exe", "WinContainers-$($channel.ToLowerInvariant())-Portable.zip")
    )
})
if ($assets.Count -eq 0) { throw "No release assets found in $releaseDir." }
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
