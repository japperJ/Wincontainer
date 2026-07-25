[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PolicyPath,
    [Parameter(Mandatory = $false)][string]$ChecksumPath
)

$ErrorActionPreference = "Stop"
$policy = Get-Content $PolicyPath -Raw | ConvertFrom-Json
if ($policy.schemaVersion -ne 1) { throw "Unsupported update policy schema." }
if ($policy.minimumSupportedVersion -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') { throw "Invalid minimumSupportedVersion." }
if ([string]::IsNullOrWhiteSpace($policy.message)) { throw "Update policy message is required." }
if ([string]::IsNullOrWhiteSpace($policy.updatedAt)) { throw "Update policy updatedAt is required." }

if ($ChecksumPath) {
    $manifest = Get-Content $ChecksumPath -Raw | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1 -or $manifest.algorithm -ne "sha256") { throw "Invalid checksum manifest header." }
    if ($manifest.releaseTag -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') { throw "Invalid checksum release tag." }
    $names = @($manifest.assets | ForEach-Object { $_.name })
    if ($names.Count -eq 0 -or $names.Count -ne (@($names | Sort-Object -Unique).Count)) { throw "Checksum assets must be non-empty and unique." }
    foreach ($asset in $manifest.assets) {
        if ($asset.sha256 -notmatch '^[a-f0-9]{64}$') { throw "Invalid SHA-256 for $($asset.name)." }
        if ($asset.name -match 'checksums\.json$') { throw "Checksum manifest must not checksum itself." }
    }
}

Write-Output "Release contracts valid."
