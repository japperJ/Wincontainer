Write-Output "Installing WSL 2.9.4 with WSLC (WSL Containers)..."
$url = "https://github.com/microsoft/WSL/releases/download/2.9.4/wsl.2.9.4.0.x64.msi"
$path = Join-Path $env:TEMP "wsl.2.9.4.0.x64.msi"
$expectedHash = "826D71865B3A45BEE03B8D9BD100D7217DD7389761D75AFA7C77106EAC5CD78E"

Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $path
$hash = (Get-FileHash -Algorithm SHA256 $path).Hash
if ($hash -ne $expectedHash) {
    throw "WSL installer hash verification failed."
}

$installer = Start-Process msiexec.exe -ArgumentList "/i", $path, "/qn", "/norestart" -Wait -PassThru
if ($installer.ExitCode -notin @(0, 3010)) {
    throw "WSL MSI installation failed with exit code $($installer.ExitCode)."
}

Write-Output "WSL 2.9.4 installed. MSI exit code: $($installer.ExitCode)"
Write-Output "Restart Windows if wslc is not available yet."
