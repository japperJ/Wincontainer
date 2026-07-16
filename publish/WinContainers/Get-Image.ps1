param([string]$Image = "")

if ($Image) {
  wsl -u root -d Ubuntu nerdctl image ls --format json --filter "reference=$Image"
}
else {
  wsl -u root -d Ubuntu nerdctl image ls --format json
}
