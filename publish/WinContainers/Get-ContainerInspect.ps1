param($Id)
$psi = [System.Diagnostics.ProcessStartInfo]::new()
$psi.FileName = "wsl"
$psi.Arguments = "-u root -d Ubuntu nerdctl container inspect $Id"
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$p = [System.Diagnostics.Process]::Start($psi)
$stdout = $p.StandardOutput.ReadToEnd()
$stderr = $p.StandardError.ReadToEnd()
$p.WaitForExit()
if ($stderr) { "$stderr`n$stdout" } else { $stdout }
