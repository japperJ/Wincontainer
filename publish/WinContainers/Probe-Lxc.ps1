$ErrorActionPreference = 'Stop'

$distroName = "Ubuntu-LXC"

$wslOutput = & wsl -d $distroName -u root -- lxc --version 2>&1
$wslExitCode = $LASTEXITCODE

$version = ($wslOutput | Out-String).Trim()

[PSCustomObject]@{
    available = ($wslExitCode -eq 0)
    message = if ($wslExitCode -eq 0) { "LXC is available in '$distroName' WSL distro." } else { "LXC is not available in '$distroName' WSL distro." }
    version = if ($wslExitCode -eq 0) { $version } else { "" }
    distro = $distroName
} | ConvertTo-Json -Depth 4
