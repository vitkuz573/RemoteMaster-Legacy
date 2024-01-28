$OPENSSL_PATH = "C:\Program Files\OpenSSL-Win64\bin\openssl.exe"
$destDirectory = "C:\ProgramData\RemoteMaster\Security\JWT"

# Create a directory for the keys if it doesn't exist
if (-not (Test-Path $destDirectory)) {
    New-Item -ItemType Directory -Path $destDirectory
    Write-Host "Created directory: $destDirectory"
}

# Generate RSA private key
& $OPENSSL_PATH genpkey -algorithm RSA -out "$destDirectory\private_key.pem" -pkeyopt rsa_keygen_bits:4096

# Extract RSA public key from private key
& $OPENSSL_PATH rsa -in "$destDirectory\private_key.pem" -pubout -out "$destDirectory\public_key.pem"

Write-Host "RSA keys successfully generated in the '$destDirectory' directory!"
