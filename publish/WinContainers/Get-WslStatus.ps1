$ErrorActionPreference = 'Stop'

$statusOutput = & wsl --status 2>&1
$statusExitCode = $LASTEXITCODE

$versionOutput = & wsl --version 2>&1
$versionExitCode = $LASTEXITCODE

$statusText = (($statusOutput | Out-String) -replace "`0", '').Trim()
$versionText = (($versionOutput | Out-String) -replace "`0", '').Trim()

[PSCustomObject]@{
    statusExitCode = $statusExitCode
    versionExitCode = $versionExitCode
    status = $statusText
    version = $versionText
} | ConvertTo-Json -Depth 4