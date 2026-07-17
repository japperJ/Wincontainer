param([string]$Id = "", [string]$Format = "default")

$nerdctlArgs = @("container", "ls", "-a", "--no-trunc")
if ($Format -in "json", "table") {
    $nerdctlArgs += "--format"
    $nerdctlArgs += $Format
}
if ($Id) {
    $nerdctlArgs += "--filter"
    $nerdctlArgs += "name=$Id"
}

$output = wsl -u root -d Ubuntu nerdctl @nerdctlArgs

if ($null -eq $output -or [string]::IsNullOrWhiteSpace(($output | Out-String))) {
    if ($Format -eq "json") { "[]" }
    return
}

if ($Format -ne "json") {
    $output
    return
}

# Enrich JSON output with container labels from inspect for compose-project grouping
$containers = $output | ConvertFrom-Json
if ($null -eq $containers) { "[]"; return }

if ($containers -is [array]) { $ids = $containers | ForEach-Object { $_.ID } }
else { $ids = @($containers.ID); $containers = @($containers) }

if ($ids.Count -gt 0) {
    $inspectOutput = wsl -u root -d Ubuntu nerdctl container inspect @ids 2>$null
    if ($inspectOutput) {
        $inspectData = $inspectOutput | ConvertFrom-Json
        if ($inspectData -isnot [array]) { $inspectData = @($inspectData) }
        $labelMap = @{}
        foreach ($item in $inspectData) {
            if ($null -ne $item.Config -and $null -ne $item.Config.Labels) {
                $labelMap[$item.ID] = $item.Config.Labels
            }
        }
        foreach ($c in $containers) {
            $cId = $c.ID
            if ($labelMap.ContainsKey($cId)) {
                $c | Add-Member -NotePropertyName "Labels" -NotePropertyValue $labelMap[$cId] -Force
            }
        }
    }
}

$containers | ConvertTo-Json -Compress -Depth 5
