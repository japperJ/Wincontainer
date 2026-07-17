param(
  [string]$Id,
  [string]$Path,
  [string]$Content
)

if ([string]::IsNullOrWhiteSpace($Id)) {
  "A container id is required."
  return
}

if ([string]::IsNullOrWhiteSpace($Path)) {
  "A file path is required."
  return
}

if ([string]::IsNullOrWhiteSpace($Content)) {
  "Content is required."
  return
}

$nerdctl = '/usr/local/bin/nerdctl'

$bytes = [System.Convert]::FromBase64String($Content)
$tempFile = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllBytes($tempFile, $bytes)

$parentDir = [System.IO.Path]::GetDirectoryName($Path) -replace "\\", "/"
if ([string]::IsNullOrWhiteSpace($parentDir)) { $parentDir = "/" }

$mkdirPsi = New-Object System.Diagnostics.ProcessStartInfo
$mkdirPsi.FileName = "wsl"
$mkdirPsi.Arguments = "-u root --exec $nerdctl exec $Id sh -c 'mkdir -p $parentDir'"
$mkdirPsi.RedirectStandardOutput = $true
$mkdirPsi.RedirectStandardError = $true
$mkdirPsi.UseShellExecute = $false
$mkdirPsi.CreateNoWindow = $true
$mp = [System.Diagnostics.Process]::Start($mkdirPsi)
$mkdirOut = $mp.StandardOutput.ReadToEnd()
$mkdirErr = $mp.StandardError.ReadToEnd()
$mp.WaitForExit()

$wslPath = "/mnt/c/" + $tempFile.Substring(3).Replace("\", "/")

$cpPsi = New-Object System.Diagnostics.ProcessStartInfo
$cpPsi.FileName = "wsl"
$cpPsi.Arguments = "-u root --exec $nerdctl cp $wslPath `"$Id`:$Path`""
$cpPsi.RedirectStandardOutput = $true
$cpPsi.RedirectStandardError = $true
$cpPsi.UseShellExecute = $false
$cpPsi.CreateNoWindow = $true

$p = [System.Diagnostics.Process]::Start($cpPsi)
$output = $p.StandardOutput.ReadToEnd()
$errorOut = $p.StandardError.ReadToEnd()
$p.WaitForExit()
$exitCode = $p.ExitCode

[System.IO.File]::Delete($tempFile)

if ($exitCode -ne 0) {
  $errorMsg = "$output $errorOut".Trim()
  if ([string]::IsNullOrWhiteSpace($errorMsg)) { $errorMsg = "Failed to write file." }
  Write-Error $errorMsg
  exit $exitCode
}

"ok"
