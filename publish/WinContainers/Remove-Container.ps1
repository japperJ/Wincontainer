param([string]$Id)

if ([string]::IsNullOrWhiteSpace($Id)) {
	"A container id is required for this action."
	return
}

wsl -u root -d Ubuntu nerdctl container rm $Id
