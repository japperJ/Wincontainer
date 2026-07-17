param([string]$Id)
$diagFile = "$env:TEMP\WinContainers\script-diag.log"
$null = New-Item -ItemType Directory -Force -Path (Split-Path $diagFile -Parent)

function Write-Diag($msg) {
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')] Get-ContainerRestartPolicy: $msg"
    Add-Content -Path $diagFile -Value $line
}

Write-Diag "START Id='$Id'"

if (-not $Id) {
    Write-Diag "END: Id empty, returning 'no'"
    "no"
    return
}

# Read from nerdctl inspect as the source of truth (reads the actual container config).
$inspect = wsl -u root -d Ubuntu nerdctl inspect $Id 2>$null
if ($inspect) {
    try {
        $json = $inspect | ConvertFrom-Json
        if ($json -is [array]) { $json = $json[0] }
        $policy = $json.HostConfig.RestartPolicy.Name
        if ($policy) {
            Write-Diag "END: from nerdctl inspect, policy='$policy'"
            $policy
            return
        }
    } catch {
        Write-Diag "Inspect parse error: $_"
    }
} else {
    Write-Diag "nerdctl inspect failed or empty"
}

# Fallback: read from the local file.
$file = "$env:TEMP\WinContainers\restart-policies.json"
Write-Diag "File='$file' FileExists=$(Test-Path $file)"

if (-not (Test-Path $file)) {
    Write-Diag "END: file missing, returning 'no'"
    "no"
    return
}

$content = Get-Content $file -Raw
Write-Diag "File content='$content'"

$policies = $content | ConvertFrom-Json -AsHashtable
$policy = $policies[$Id]
Write-Diag "Lookup: Id='$Id' result='$policy'"

if ($policy) {
    Write-Diag "END: from file, returning '$policy'"
    $policy
} else {
    Write-Diag "END: not found, returning 'no'"
    "no"
}
