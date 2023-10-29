param (
    [string]$exeFilePath
)

$certSubject = "CN=RemoteMaster Development"
$certStoreLocation = "Cert:\CurrentUser\My"
$cert = Get-ChildItem $certStoreLocation | Where-Object { $_.Subject -eq $certSubject }

Set-AuthenticodeSignature $exeFilePath -Certificate $cert

