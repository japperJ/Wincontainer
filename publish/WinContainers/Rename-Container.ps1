param(
  [string]$Id,
  [string]$NewName
)

if ([string]::IsNullOrWhiteSpace($Id)) {
  "A container id is required for this action."
  return
}

if ([string]::IsNullOrWhiteSpace($NewName)) {
  "A new container name is required for this action."
  return
}

wsl -u root -d Ubuntu nerdctl container rename $Id $NewName
