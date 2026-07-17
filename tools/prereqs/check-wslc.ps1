$wslcVersion = wslc --version 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Output "OK: WSLC is installed - $wslcVersion"
    exit 0
} else {
    Write-Output "MISSING: WSLC is not installed"
    exit 1
}
