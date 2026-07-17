param([string]$Term, [int]$Limit = 20)

$results = wsl -u root -d Ubuntu nerdctl search --format json --limit $Limit $Term 2>&1

if ($null -eq $results -or [string]::IsNullOrWhiteSpace(($results | Out-String))) {
  "[]"
}
else {
  $results
}
