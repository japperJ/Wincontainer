Write-Output "Installing WSL 2.9.3 with WSLC (WSL Containers)..."
$url = "https://github.com/microsoft/WSL/releases/download/2.9.3/wsl.2.9.3.0.x64.msi"
$path = Join-Path $env:TEMP "wsl.2.9.3.0.x64.msi"
$expectedHash = "7281640D2DC64BAE2044A466A336A9460B497F964BFB3E949B270D2F4CFCD48D"

Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $path
$hash = (Get-FileHash -Algorithm SHA256 $path).Hash
if ($hash -ne $expectedHash) {
    throw "WSL installer hash verification failed."
}

$installer = Start-Process msiexec.exe -ArgumentList "/i", $path, "/qn", "/norestart" -Wait -PassThru
if ($installer.ExitCode -notin @(0, 3010)) {
    throw "WSL MSI installation failed with exit code $($installer.ExitCode)."
}

Write-Output "WSL 2.9.3 installed. MSI exit code: $($installer.ExitCode)"
Write-Output "Restart Windows if wslc is not available yet."
