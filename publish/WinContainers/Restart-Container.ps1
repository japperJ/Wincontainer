param(
  [string]$Id
)

if ([string]::IsNullOrWhiteSpace($Id)) {
  "A container id is required."
  return
}

wsl -u root -d Ubuntu nerdctl container restart $Id 2>&1
