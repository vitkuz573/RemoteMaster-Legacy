$subjectName = "RemoteMaster Development"
$caName = "RemoteMaster Internal CA"
$codeSignCertDirectory = "CodeSignCert"
$opensslPath = "C:\Program Files\OpenSSL-Win64\bin\openssl.exe"

# Ensure destination directory exists
if (-not (Test-Path $codeSignCertDirectory)) {
    New-Item -Path $codeSignCertDirectory -ItemType Directory
    Write-Host "Created directory: $codeSignCertDirectory" -ForegroundColor Green
}

# Generate a private key for the new certificate
& $opensslPath genpkey -algorithm RSA -out "$codeSignCertDirectory\$subjectName.key" -pkeyopt rsa_keygen_bits:4096

# Create an OpenSSL configuration file
$config = @"
[ req ]
default_bits        = 4096
default_keyfile     = privkey.pem
distinguished_name  = req_distinguished_name
req_extensions     = req_ext

[ req_distinguished_name ]
commonName                  = Common Name (e.g. server FQDN or YOUR name)

[ req_ext ]
keyUsage = critical,digitalSignature
extendedKeyUsage = codeSigning
"@
$config | Out-File "$codeSignCertDirectory\openssl.cnf" -Encoding ascii

# Generate a CSR for the new certificate using the private key
& $opensslPath req -new -key "$codeSignCertDirectory\$subjectName.key" -out "$codeSignCertDirectory\$subjectName.csr" -subj "/CN=$subjectName" -config "$codeSignCertDirectory\openssl.cnf"

# Find and use the CA certificate from the Windows Certificate Store
$caCert = Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object { $_.Subject -like "*CN=$caName*" }
if ($null -eq $caCert) {
    Write-Host "CA certificate not found in the LocalMachine\Root store." -ForegroundColor Red
    return
} else {
    Write-Host "CA certificate found: $($caCert.Thumbprint)" -ForegroundColor Green
}

# Export the CA certificate and its private key to PEM files
$caCertPath = "$codeSignCertDirectory\ca_cert.pem"
$caKeyPath = "$codeSignCertDirectory\ca_key.pem"
$caCert.Export('Cert') | Set-Content $caCertPath -Encoding Byte
$caCert.PrivateKey.ExportCspBlob($true) | Set-Content $caKeyPath -Encoding Byte

# Sign the CSR using the exported CA certificate and key
& $opensslPath x509 -req -days 365 -in "$codeSignCertDirectory\$subjectName.csr" -CA $caCertPath -CAkey $caKeyPath -out "$codeSignCertDirectory\$subjectName.crt" -CAcreateserial -extfile "$codeSignCertDirectory\openssl.cnf" -extensions req_ext

# Continue as previously to create the PFX and import it
$password = Read-Host -Prompt "Enter password for the private key" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

& $opensslPath pkcs12 -export -out "$codeSignCertDirectory\$subjectName.pfx" -inkey "$codeSignCertDirectory\$subjectName.key" -in "$codeSignCertDirectory\$subjectName.crt" -name "$subjectName" -passout pass:$password
Write-Host "Generated certificate for $subjectName and signed it with $caName." -ForegroundColor Green

$pfxFilePath = "$codeSignCertDirectory\$subjectName.pfx"
$certPassword = ConvertTo-SecureString -String $password -Force -AsPlainText
Import-PfxCertificate -FilePath $pfxFilePath -CertStoreLocation Cert:\CurrentUser\My -Password $certPassword
Write-Host "Imported certificate into the CurrentUser\My store." -ForegroundColor Green
