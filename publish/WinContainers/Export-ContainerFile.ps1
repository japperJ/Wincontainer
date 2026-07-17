param(
  [string]$Id,
  [string]$Path
)

if ([string]::IsNullOrWhiteSpace($Id)) {
  "A container id is required."
  return
}

if ([string]::IsNullOrWhiteSpace($Path)) {
  "A file path is required."
  return
}

$nerdctl = '/usr/local/bin/nerdctl'
$output = & wsl -u root --exec $nerdctl exec $Id sh -c 'cat "$0" | base64 -w0' $Path 2>&1
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
  $errorMsg = ($output -join ' ').Trim()
  if ([string]::IsNullOrWhiteSpace($errorMsg)) { $errorMsg = "Failed to export file." }
  Write-Error $errorMsg
  exit $exitCode
}

$output
