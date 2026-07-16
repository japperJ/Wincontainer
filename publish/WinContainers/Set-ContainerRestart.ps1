param([string]$Id, [string]$Policy)
$diagFile = "$env:TEMP\WinContainers\script-diag.log"
$null = New-Item -ItemType Directory -Force -Path (Split-Path $diagFile -Parent)

function Write-Diag($msg) {
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')] Set-ContainerRestart: $msg"
    Add-Content -Path $diagFile -Value $line
}

Write-Diag "START Id='$Id' Policy='$Policy'"

if (-not $Id) {
    Write-Diag "ERROR: Id is empty"
    "Error: Parameter 'Id' is required."
    return
}
if (-not $Policy) { $Policy = "no" }

$dir = "$env:TEMP\WinContainers"
Write-Diag "Dir='$dir'"
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null; Write-Diag "Created dir" }

$file = "$dir\restart-policies.json"
Write-Diag "File='$file' FileExists=$(Test-Path $file)"

$policies = @{}
if (Test-Path $file) {
    $content = Get-Content $file -Raw
    Write-Diag "Existing content='$content'"
    $policies = $content | ConvertFrom-Json -AsHashtable
}
$policies[$Id] = $Policy
$json = $policies | ConvertTo-Json
Write-Diag "Writing json='$json'"
$json | Set-Content $file

# Also try nerdctl update directly (works on running containers, sets policy immediately).
Write-Diag "Running: nerdctl update --restart $Policy $Id"
$updateOut = wsl -u root -d Ubuntu nerdctl update --restart $Policy $Id 2>&1
if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) {
    Write-Diag "nerdctl update failed (exit=$LASTEXITCODE): $updateOut"
}

Write-Diag "END: ok"
"ok"
