$certPath = "C:\Users\vitaly\certs\certificate.pfx"
$password = ConvertTo-SecureString -String "password" -Force -AsPlainText
$cert = New-Object -TypeName System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList $certPath, $password
Set-AuthenticodeSignature -FilePath "C:\Users\vitaly\source\repos\RemoteMaster\RemoteMaster.Server\bin\Release\net7.0-windows\RemoteMaster.Server.exe" -Certificate $cert
