$caName = "RemoteMaster Internal CA"
$destDirectory = "InternalCA"

# Ensure destination directory exists
if (-not (Test-Path $destDirectory)) {
    New-Item -Path $destDirectory -ItemType Directory
    Write-Host "Created directory: $destDirectory" -ForegroundColor Green
}

# Certificate parameters
$param = @{
    DnsName           = $caName
    KeyLength         = 4096
    KeyAlgorithm      = "RSA"
    HashAlgorithm     = "SHA256"
    CertStoreLocation = "Cert:\CurrentUser\My"
    KeyExportPolicy   = "Exportable"
    NotAfter          = (Get-Date).AddYears(10) # Validity of 10 years
    KeyUsage          = "CertSign"
    FriendlyName      = $caName
}

# Prompt for password
$password = Read-Host -Prompt "Enter a secure password for the CA private key" -AsSecureString

# Create the self-signed root CA certificate
$caCert = New-SelfSignedCertificate @param
Write-Host "Generated self-signed root CA certificate." -ForegroundColor Green

# Export root CA certificate to a PFX file with password
$pfxPath = "$destDirectory\$caName.pfx"
$caCert | Export-PfxCertificate -Password $password -FilePath $pfxPath
Write-Host "Exported PFX to: $pfxPath" -ForegroundColor Green

# Export root CA certificate to a CER file (public key only)
$cerPath = "$destDirectory\$caName.cer"
$caCert | Export-Certificate -FilePath $cerPath
Write-Host "Exported CER to: $cerPath" -ForegroundColor Green

# Add root CA certificate to Trusted Root Certification Authorities store
Import-Certificate -FilePath $cerPath -CertStoreLocation "Cert:\CurrentUser\Root"
Write-Host "Added CA certificate to Trusted Root Certification Authorities store." -ForegroundColor Green
