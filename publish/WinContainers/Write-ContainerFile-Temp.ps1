param(
  [string]$Id,
  [string]$Path,
  [string]$TempFilePath,
  [string]$ParentDir
)

$nerdctl = '/usr/local/bin/nerdctl'

$drive = $TempFilePath[0].ToString().ToLower()
$wslPath = "/mnt/$drive/" + $TempFilePath.Substring(3).Replace("\", "/")

& wsl -u root --exec $nerdctl exec $Id sh -c "mkdir -p '$ParentDir'" 2>$null

$cpPsi = New-Object System.Diagnostics.ProcessStartInfo
$cpPsi.FileName = "wsl"
$cpPsi.Arguments = "-u root --exec $nerdctl cp `"$wslPath`" `"$Id`:$Path`""
$cpPsi.RedirectStandardOutput = $true
$cpPsi.RedirectStandardError = $true
$cpPsi.UseShellExecute = $false
$cpPsi.CreateNoWindow = $true

$p = [System.Diagnostics.Process]::Start($cpPsi)
$output = $p.StandardOutput.ReadToEnd()
$errorOut = $p.StandardError.ReadToEnd()
$p.WaitForExit()
$exitCode = $p.ExitCode

if ($exitCode -ne 0) {
  $errorMsg = "$output $errorOut".Trim()
  if ([string]::IsNullOrWhiteSpace($errorMsg)) { $errorMsg = "Failed to write file." }
  Write-Error $errorMsg
  exit $exitCode
}

"ok"
