$tenYearsFromNow = (Get-Date).AddYears(10)
$cert = New-SelfSignedCertificate -Subject "CN=RemoteMaster Development" -Type CodeSigning -CertStoreLocation cert:\CurrentUser\My -NotAfter $tenYearsFromNow
$tempFilePath = Join-Path $env:TEMP "tempCert.cer"
Export-Certificate -Cert $cert -FilePath $tempFilePath
Import-Certificate -FilePath $tempFilePath -CertStoreLocation Cert:\LocalMachine\TrustedPublisher
Import-Certificate -FilePath $tempFilePath -CertStoreLocation Cert:\LocalMachine\Root
Remove-Item -Path $tempFilePath -Force
Set-AuthenticodeSignature "C:\Program Files\RemoteMaster\Client\RemoteMaster.Client.exe" -Certificate $cert
Get-AuthenticodeSignature -FilePath "C:\Program Files\RemoteMaster\Client\RemoteMaster.Client.exe"
