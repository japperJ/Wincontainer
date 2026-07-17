param(
  [string]$Name
)

if ([string]::IsNullOrWhiteSpace($Name)) {
  "A network name is required."
  return
}

wsl -u root -d Ubuntu nerdctl network create $Name 2>&1
