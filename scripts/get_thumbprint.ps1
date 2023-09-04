$password = ConvertTo-SecureString -String "password" -Force -AsPlainText
$cert = New-Object -TypeName System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList "certificate.pfx", $password
Write-Output $cert.Thumbprint
