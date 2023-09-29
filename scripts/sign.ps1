$certSubject = "CN=RemoteMaster Development"
$certStoreLocation = "Cert:\CurrentUser\My"
$cert = Get-ChildItem $certStoreLocation | Where-Object { $_.Subject -eq $certSubject }

Set-AuthenticodeSignature "\\SERVER-DC02\Win\RemoteMaster\Client\RemoteMaster.Client.exe" -Certificate $cert
Get-AuthenticodeSignature -FilePath "\\SERVER-DC02\Win\RemoteMaster\Client\RemoteMaster.Client.exe"