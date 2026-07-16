$ErrorActionPreference = 'Stop'

$distroOutput = & wsl --list --verbose 2>&1
$distroExitCode = $LASTEXITCODE

$distroText = (($distroOutput | Out-String) -replace "`0", '').Trim()

[PSCustomObject]@{
    exitCode = $distroExitCode
    output = $distroText
} | ConvertTo-Json -Depth 4