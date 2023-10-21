$OPENSSL_PATH = "C:\Program Files\OpenSSL-Win64\bin\openssl.exe"

# Create a directory for the keys if it doesn't exist
if (-not (Test-Path .\keys)) {
    New-Item -ItemType Directory -Path .\keys
}

# Generate ECDSA private key
& $OPENSSL_PATH ecparam -name prime256v1 -genkey -noout -out .\keys\private_key.pem

# Extract ECDSA public key from private key
& $OPENSSL_PATH ec -in .\keys\private_key.pem -pubout -out .\keys\public_key.pem

Write-Host "Keys successfully generated in the 'keys' directory!"
