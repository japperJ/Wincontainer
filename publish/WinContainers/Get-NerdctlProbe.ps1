$ErrorActionPreference = 'Stop'

$versionOutput = & wsl -u root -d Ubuntu nerdctl --version 2>&1
$versionExitCode = $LASTEXITCODE

[PSCustomObject]@{
    available = ($versionExitCode -eq 0)
    message = if ($versionExitCode -eq 0) { 'nerdctl is available in WSL.' } else { 'nerdctl is not available in WSL.' }
    version = ($versionOutput | Out-String).Trim()
} | ConvertTo-Json -Depth 4
