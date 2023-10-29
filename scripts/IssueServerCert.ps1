$subjectName = $env:computername
$ip = (([System.Net.Dns]::GetHostAddresses($subjectName) | Where-Object { $_.AddressFamily -eq 'InterNetwork' })[0]).IPAddressToString
$caName = "RemoteMaster Internal CA"
$caDirectory = "InternalCA"
$serverCertDirectory = "ServerCert"
$opensslPath = "C:\Program Files\OpenSSL-Win64\bin\openssl.exe"

# Check if CA certificate exists
$caCertPath = "$caDirectory\$caName.crt"
if (-not (Test-Path $caCertPath)) {
    Write-Host "CA certificate not found. Please execute CreateInternalCA.ps1 to generate the CA certificate." -ForegroundColor Yellow
    return
}

# Ensure destination directory exists
if (-not (Test-Path $serverCertDirectory)) {
    New-Item -Path $serverCertDirectory -ItemType Directory
    Write-Host "Created directory: $serverCertDirectory" -ForegroundColor Green
}

# Generate a private key for the new certificate
& $opensslPath genpkey -algorithm RSA -out "$serverCertDirectory\$subjectName.key" -pkeyopt rsa_keygen_bits:4096

# Generate a CSR for the new certificate using the private key
& $opensslPath req -new -key "$serverCertDirectory\$subjectName.key" -out "$serverCertDirectory\$subjectName.csr" -subj "/CN=$subjectName"

# Create an OpenSSL configuration file for the new certificate
$config = @"
[ req ]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no
[ req_distinguished_name ]
CN = $subjectName
[ v3_req ]
subjectAltName = @alt_names
extendedKeyUsage = serverAuth
[alt_names]
IP.1 = $ip
"@
$config | Out-File "$serverCertDirectory\openssl.cnf" -Encoding ascii

# Generate a certificate using the CSR and sign it using the CA's private key
& $opensslPath x509 -req -days 365 -in "$serverCertDirectory\$subjectName.csr" -CA "$caDirectory\$caName.crt" -CAkey "$caDirectory\$caName.key" -set_serial 01 -out "$serverCertDirectory\$subjectName.crt" -extfile "$serverCertDirectory\openssl.cnf" -extensions v3_req

# Ask for password to secure the private key
$password = Read-Host -Prompt "Enter password for the private key" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Export the certificate and private key to a PFX file
& $opensslPath pkcs12 -export -out "$serverCertDirectory\$subjectName.pfx" -inkey "$serverCertDirectory\$subjectName.key" -in "$serverCertDirectory\$subjectName.crt" -name "$subjectName" -passout pass:$password

Write-Host "Generated certificate for $subjectName and signed it with $caName." -ForegroundColor Green
