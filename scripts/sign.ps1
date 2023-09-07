$certSubject = "CN=RemoteMaster Development"
$certStoreLocation = "Cert:\CurrentUser\My"
$cert = Get-ChildItem $certStoreLocation | Where-Object { $_.Subject -eq $certSubject }

Set-AuthenticodeSignature "C:\Program Files\RemoteMaster\Client\RemoteMaster.Client.exe" -Certificate $cert
Get-AuthenticodeSignature -FilePath "C:\Program Files\RemoteMaster\Client\RemoteMaster.Client.exe"