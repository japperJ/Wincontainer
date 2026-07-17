param(
  [string]$Id
)

if ([string]::IsNullOrWhiteSpace($Id)) {
  "A container id is required."
  return
}

$output = wsl -u root -d Ubuntu timeout 25 nerdctl container logs --tail=1000 $Id 2>&1
if ($null -eq $output) {
  return
}

$output
