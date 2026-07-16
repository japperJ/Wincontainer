param([string]$Image)

wsl -u root -d Ubuntu nerdctl image pull $Image
