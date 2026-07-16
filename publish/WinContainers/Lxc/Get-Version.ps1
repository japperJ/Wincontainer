$distroName = "Ubuntu-LXC"

function Normalize-Line($value) {
    if ($null -eq $value) { return "" }
    return ([string]$value).Replace("`0", "").Trim()
}

$wslList = & wsl --list --quiet 2>$null
$distroLines = @($wslList | ForEach-Object { Normalize-Line $_ } | Where-Object { $_ -ne "" })
$distroExists = ($distroLines | Where-Object { $_ -eq $distroName }).Count -gt 0

if (-not $distroExists) {
    Write-Output "not available"
    exit 0
}

$lxcOutput = & wsl -d $distroName -u root -- bash -lc "if command -v lxc >/dev/null 2>&1; then lxc --version; else echo not available; exit 127; fi" 2>&1
$lines = @($lxcOutput | ForEach-Object { Normalize-Line $_ } | Where-Object { $_ -ne "" })

$versionLine = $lines | Where-Object {
    $_ -notmatch "^wsl:" -and $_ -match "^\d+(\.\d+){1,}"
} | Select-Object -First 1

if ($versionLine) {
    Write-Output $versionLine
} else {
    Write-Output "not available"
}
exit 0
