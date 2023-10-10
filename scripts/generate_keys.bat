@echo off

REM Path to your OpenSSL
set OPENSSL_PATH="C:\Program Files\OpenSSL-Win64\bin\openssl.exe"

REM Create the 'keys' directory if it doesn't exist
if not exist keys mkdir keys

REM Generate the private key (keys/private_key.pem)
%OPENSSL_PATH% genpkey -algorithm RSA -out keys/private_key.pem

REM Generate the public key (keys/public_key.pem) based on the private key
%OPENSSL_PATH% pkey -in keys/private_key.pem -pubout -out keys/public_key.pem

echo.
echo Keys successfully generated in the 'keys' directory!
pause
