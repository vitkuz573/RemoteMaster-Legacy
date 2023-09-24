$tenYearsFromNow = (Get-Date).AddYears(10)
$cert = New-SelfSignedCertificate -Subject "CN=RemoteMaster Authentication" -CertStoreLocation cert:\LocalMachine\My -KeyUsage DigitalSignature, KeyEncipherment -KeySpec KeyExchange -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1") -NotAfter $tenYearsFromNow
$tempFilePath = Join-Path $env:TEMP "tempAuthCert.cer"
Export-Certificate -Cert $cert -FilePath $tempFilePath
Import-Certificate -FilePath $tempFilePath -CertStoreLocation Cert:\LocalMachine\Root
Remove-Item -Path $tempFilePath -Force