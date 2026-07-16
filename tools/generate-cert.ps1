param(
    [string]$Password = "WinContainers-dev"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Generating self-signed code signing certificate ===" -ForegroundColor Cyan

$cert = New-SelfSignedCertificate `
    -DnsName "WinContainers" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(2) `
    -Type CodeSigningCert

$securePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText
$pfxPath = Join-Path $PSScriptRoot "WinContainers-dev.pfx"
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword

Write-Host "Certificate created successfully" -ForegroundColor Green
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host "  PFX saved:  $pfxPath"
Write-Host "  Expires:    $($cert.NotAfter)"
Write-Host ""
Write-Host "Add this to .gitignore:" -ForegroundColor Yellow
Write-Host "  tools/WinContainers-dev.pfx"
Write-Host "  *.pfx"
