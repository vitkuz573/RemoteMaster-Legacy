$subjectName = $env:computername
$ip = (([System.Net.Dns]::GetHostAddresses($subjectName) | Where-Object { $_.AddressFamily -eq 'InterNetwork' })[0]).IPAddressToString
$caName = "RemoteMaster Internal CA"
$destDirectory = "InternalCA\ServerCert"
$opensslPath = "C:\Program Files\OpenSSL-Win64\bin\openssl.exe"

# Check if CA certificate exists
$caCertPath = "InternalCA\$caName.crt"
if (-not (Test-Path $caCertPath)) {
    Write-Host "CA certificate not found. Please execute CreateInternalCA.ps1 to generate the CA certificate." -ForegroundColor Yellow
    return
}

# Ensure destination directory exists
if (-not (Test-Path $destDirectory)) {
    New-Item -Path $destDirectory -ItemType Directory
    Write-Host "Created directory: $destDirectory" -ForegroundColor Green
}

# Generate a private key for the new certificate
& $opensslPath genpkey -algorithm RSA -out "$destDirectory\$subjectName.key" -pkeyopt rsa_keygen_bits:4096

# Generate a CSR for the new certificate using the private key
& $opensslPath req -new -key "$destDirectory\$subjectName.key" -out "$destDirectory\$subjectName.csr" -subj "/CN=$subjectName"

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
$config | Out-File "$destDirectory\$subjectName.cnf" -Encoding ascii

# Generate a certificate using the CSR and sign it using the CA's private key
& $opensslPath x509 -req -days 3650 -in "$destDirectory\$subjectName.csr" -CA "InternalCA\$caName.crt" -CAkey "InternalCA\$caName.key" -set_serial 01 -out "$destDirectory\$subjectName.crt" -extfile "$destDirectory\$subjectName.cnf" -extensions v3_req

# Ask for password to secure the private key
$password = Read-Host -Prompt "Enter password for the private key" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Export the certificate and private key to a PFX file
& $opensslPath pkcs12 -export -out "$destDirectory\$subjectName.pfx" -inkey "$destDirectory\$subjectName.key" -in "$destDirectory\$subjectName.crt" -name "$subjectName" -passout pass:$password

Write-Host "Generated certificate for $subjectName and signed it with $caName." -ForegroundColor Green
