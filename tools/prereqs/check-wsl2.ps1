$wslStatus = wsl --status 2>&1
if ($LASTEXITCODE -eq 0 -and $wslStatus -match "Default Version: 2") {
    Write-Output "OK: WSL2 is installed"
    exit 0
} else {
    Write-Output "MISSING: WSL2 is not installed or not configured"
    exit 1
}
