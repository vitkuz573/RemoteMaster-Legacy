$caName = "RemoteMaster Internal CA"
$destDirectory = "InternalCA"
$opensslPath = "C:\Program Files\OpenSSL-Win64\bin\openssl.exe"

# Ensure destination directory exists
if (-not (Test-Path $destDirectory)) {
    New-Item -Path $destDirectory -ItemType Directory
    Write-Host "Created directory: $destDirectory" -ForegroundColor Green
}

# Generate a private key
& $opensslPath genpkey -algorithm RSA -out "$destDirectory\$caName.key" -pkeyopt rsa_keygen_bits:4096

# Generate a CSR using the private key
& $opensslPath req -new -key "$destDirectory\$caName.key" -out "$destDirectory\$caName.csr" -subj "/CN=$caName"

# Create an OpenSSL configuration file
$config = @"
[ req ]
distinguished_name = req_distinguished_name
x509_extensions = v3_req
prompt = no
[ req_distinguished_name ]
CN = $caName
[ v3_req ]
basicConstraints = CA:TRUE
"@
$config | Out-File "$destDirectory\openssl.cnf" -Encoding ascii

# Generate a self-signed certificate with Basic Constraints set to CA:TRUE
& $opensslPath x509 -req -days 3650 -in "$destDirectory\$subjectName.csr" -CA "..\..\$caName.crt" -CAkey "..\..\$caName.key" -set_serial 01 -out "$destDirectory\$subjectName.crt" -extfile "$destDirectory\$subjectName.cnf" -extensions v3_req

# Convert the certificate and private key to a PFX file
& $opensslPath pkcs12 -export -out "$destDirectory\$caName.pfx" -inkey "$destDirectory\$caName.key" -in "$destDirectory\$caName.crt" -name $caName

Write-Host "Generated self-signed root CA certificate and exported to PFX." -ForegroundColor Green

# Import the certificate to the Trusted Root Certification Authorities store
Import-Certificate -FilePath "$destDirectory\$caName.crt" -CertStoreLocation "Cert:\CurrentUser\Root"
Write-Host "Added CA certificate to Trusted Root Certification Authorities store." -ForegroundColor Green