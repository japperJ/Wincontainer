param(
  [string]$Id,
  [string]$Path = "/"
)

$nerdctl = '/usr/local/bin/nerdctl'
$target = if ($Path -eq '/' -or $Path -eq '') { '/' } else { $Path.TrimEnd('/') }

$shellScript = 'for f in "$0"/* "$0"/.*; do b=$(basename "$f"); [ "$b" = "." ] && continue; [ "$b" = ".." ] && continue; [ -d "$f" ] && echo "dir|$b" || echo "file|$b"; done'

$output = & wsl -u root --exec $nerdctl exec $Id sh -c $shellScript $target 2>&1
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
  $errorMsg = ($output -join ' ').Trim()
  if ([string]::IsNullOrWhiteSpace($errorMsg)) { $errorMsg = "Container is not running or command failed." }
  Write-Error $errorMsg
  exit $exitCode
}

$lines = $output -split [Environment]::NewLine

$files = foreach ($line in $lines) {
  $line = $line.Trim()
  if ($line -match '^(dir|file)\|(.+)$') {
    [PSCustomObject]@{
      type = $matches[1]
      name = $matches[2]
    }
  }
}

if ($files -and @($files).Count -gt 0) {
  $json = $files | ConvertTo-Json -Compress
  if ($json -notmatch '^\[.*\]$') {
    "[$json]"
  } else {
    $json
  }
} else {
  '[]'
}
