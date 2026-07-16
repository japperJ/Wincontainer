# Restore containers with auto-start policies (always, unless-stopped).
$file = "$env:TEMP\WinContainers\restart-policies.json"
if (-not (Test-Path $file)) { return }

$policies = Get-Content $file | ConvertFrom-Json -AsHashtable
foreach ($id in $policies.Keys)
{
    $policy = $policies[$id]
    if ($policy -in @("always", "unless-stopped"))
    {
        wsl -u root -d Ubuntu nerdctl container start $id 2>&1 | Out-Null
    }
}
