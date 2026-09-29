[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PolicyPath,
    [Parameter(Mandatory = $false)][string]$ChecksumPath
)

$ErrorActionPreference = "Stop"
# Strict SemVer: numeric identifiers must not carry leading zeros, otherwise vpk
# normalizes the version and the packed asset names stop matching the release tag.
$strictNumber = '0|[1-9]\d*'
$semverPattern = "($strictNumber)\.($strictNumber)\.($strictNumber)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?"
$policy = Get-Content $PolicyPath -Raw | ConvertFrom-Json
if ($policy.schemaVersion -ne 1) { throw "Unsupported update policy schema." }
if ($policy.minimumSupportedVersion -notmatch "^$semverPattern$") { throw "Invalid minimumSupportedVersion." }
if ([string]::IsNullOrWhiteSpace($policy.message)) { throw "Update policy message is required." }
if ([string]::IsNullOrWhiteSpace($policy.updatedAt)) { throw "Update policy updatedAt is required." }

if ($ChecksumPath) {
    $manifest = Get-Content $ChecksumPath -Raw | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1 -or $manifest.algorithm -ne "sha256") { throw "Invalid checksum manifest header." }
    if ($manifest.releaseTag -notmatch "^v$semverPattern$") { throw "Invalid checksum release tag." }
    $names = @($manifest.assets | ForEach-Object { $_.name })
    if ($names.Count -eq 0 -or $names.Count -ne (@($names | Sort-Object -Unique).Count)) { throw "Checksum assets must be non-empty and unique." }
    foreach ($asset in $manifest.assets) {
        if ($asset.sha256 -notmatch '^[a-f0-9]{64}$') { throw "Invalid SHA-256 for $($asset.name)." }
        if ($asset.name -match 'checksums\.json$') { throw "Checksum manifest must not checksum itself." }
    }
    # A release without the full update package is uninstallable in-app even
    # though every other asset is present, so require it explicitly.
    $version = $manifest.releaseTag.Substring(1)
    $channel = if ($version.Contains("-")) { "beta" } else { "stable" }
    $required = "WinContainers-$version-$channel-full.nupkg"
    if ($names -notcontains $required) { throw "Checksum manifest must publish the full update package: $required" }
}

Write-Output "Release contracts valid."
