param(
  [string]$Name,
  [string]$Image,
  [string]$Ports,
  [string]$Volumes,
  [string]$EnvVars,
  [string]$RestartPolicy,
  [string]$Network,
  [string]$ProjectName
)

if ([string]::IsNullOrWhiteSpace($Name)) {
  "A container name is required for this action."
  return
}

if ([string]::IsNullOrWhiteSpace($Image)) {
  "An image name is required for this action."
  return
}

# Auto-deduplicate: if the name already exists, append a timestamp suffix
$existing = wsl -u root -d Ubuntu nerdctl container inspect $Name --format "{{.ID}}" 2>$null
if (-not [string]::IsNullOrWhiteSpace($existing)) {
  $suffix = Get-Date -Format "HHmmss"
  "Container name '$Name' is already in use. Using '$Name-$suffix' instead."
  $Name = "$Name-$suffix"
}

$nerdctlArgs = @("container", "create", "--name", $Name)

# Ports
if (-not [string]::IsNullOrWhiteSpace($Ports)) {
  try {
    $portList = $Ports | ConvertFrom-Json
    foreach ($port in $portList) {
      $hostPort = $port.host
      $containerPort = $port.container
      if (-not [string]::IsNullOrWhiteSpace($hostPort) -and -not [string]::IsNullOrWhiteSpace($containerPort)) {
        $nerdctlArgs += "--publish"
        $nerdctlArgs += "$hostPort`:$containerPort"
      }
    }
  } catch {
    "Warning: Could not parse Ports JSON: $_"
  }
}

# Volumes
if (-not [string]::IsNullOrWhiteSpace($Volumes)) {
  try {
    $volumeList = $Volumes | ConvertFrom-Json
    foreach ($volume in $volumeList) {
      $source = $volume.source
      $target = $volume.target
      if (-not [string]::IsNullOrWhiteSpace($source) -and -not [string]::IsNullOrWhiteSpace($target)) {
        # Create named volume if source doesn't look like a path
        $isBindMount = $source -match '^[/\\]|^[A-Za-z]:[/\\]|^\.\.?[/\\]'
        if (-not $isBindMount) {
          wsl -u root -d Ubuntu nerdctl volume create $source 2>$null
        }
        $nerdctlArgs += "--volume"
        $nerdctlArgs += "$source`:$target"
      }
    }
  } catch {
    "Warning: Could not parse Volumes JSON: $_"
  }
}

# Environment variables
if (-not [string]::IsNullOrWhiteSpace($EnvVars)) {
  try {
    $envList = $EnvVars | ConvertFrom-Json
    foreach ($env in $envList) {
      $name = $env.name
      $value = $env.value
      if (-not [string]::IsNullOrWhiteSpace($name) -and $null -ne $value) {
        $nerdctlArgs += "--env"
        $nerdctlArgs += "$name=$value"
      }
    }
  } catch {
    "Warning: Could not parse EnvVars JSON: $_"
  }
}

# Restart policy
if (-not [string]::IsNullOrWhiteSpace($RestartPolicy) -and $RestartPolicy -ne "no") {
  $nerdctlArgs += "--restart"
  $nerdctlArgs += $RestartPolicy
}

# Project label for compose-project grouping
if (-not [string]::IsNullOrWhiteSpace($ProjectName)) {
    $nerdctlArgs += "--label"
    $nerdctlArgs += "com.docker.compose.project=$ProjectName"
}

# Network
if (-not [string]::IsNullOrWhiteSpace($Network)) {
  $nerdctlArgs += "--network"
  $nerdctlArgs += $Network
}

$nerdctlArgs += $Image

wsl -u root -d Ubuntu nerdctl @nerdctlArgs
