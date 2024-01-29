$subjectName = "RemoteMaster Development"
$caName = "RemoteMaster Internal CA"
$caDirectory = "C:\ProgramData\RemoteMaster\Security\InternalCA"
$codeSignCertDirectory = "CodeSignCert"
$opensslPath = "C:\Program Files\OpenSSL-Win64\bin\openssl.exe"

# Check if CA certificate exists
$caCertPath = "$caDirectory\$caName.crt"
if (-not (Test-Path $caCertPath)) {
    Write-Host "CA certificate not found. Please execute CreateInternalCA.ps1 to generate the CA certificate." -ForegroundColor Yellow
    return
}

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

# Generate a certificate using the CSR and sign it using the CA's private key
& $opensslPath x509 -req -days 365 -in "$codeSignCertDirectory\$subjectName.csr" -CA "$caDirectory\$caName.crt" -CAkey "$caDirectory\$caName.key" -set_serial 01 -out "$codeSignCertDirectory\$subjectName.crt" -extfile "$codeSignCertDirectory\openssl.cnf" -extensions req_ext

# Ask for password to secure the private key
$password = Read-Host -Prompt "Enter password for the private key" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Export the certificate and private key to a PFX file
& $opensslPath pkcs12 -export -out "$codeSignCertDirectory\$subjectName.pfx" -inkey "$codeSignCertDirectory\$subjectName.key" -in "$codeSignCertDirectory\$subjectName.crt" -name "$subjectName" -passout pass:$password

Write-Host "Generated certificate for $subjectName and signed it with $caName." -ForegroundColor Green

# Import the PFX file into the CurrentUser\My (Personal) store
$pfxFilePath = "$codeSignCertDirectory\$subjectName.pfx"
$certPassword = ConvertTo-SecureString -String $password -Force -AsPlainText
Import-PfxCertificate -FilePath $pfxFilePath -CertStoreLocation Cert:\CurrentUser\My -Password $certPassword

Write-Host "Imported certificate into the CurrentUser\My store." -ForegroundColor Green
