param(
  [string]$Id,
  [string]$Command
)

if ([string]::IsNullOrWhiteSpace($Id)) {
  "A container id is required."
  return
}

if ([string]::IsNullOrWhiteSpace($Command)) {
  $Command = "/bin/sh"
}

wsl -u root -d Ubuntu nerdctl exec $Id $Command 2>&1
