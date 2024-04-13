param (
    [Parameter(Mandatory = $true)]
    [ValidateScript({Test-Path $_ -PathType Leaf})]
    [string]$exeFilePath
)

$certSubject = "CN=RemoteMaster Development"
$certStoreLocation = "Cert:\CurrentUser\My"

function Write-OutputMessage {
    Param([string]$message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "${timestamp}: $message"
}

function Load-Certificate {
    Write-OutputMessage "Searching for certificate with subject '$certSubject'."
    $certs = Get-ChildItem $certStoreLocation | Where-Object { $_.Subject -eq $certSubject } | Sort-Object NotBefore -Descending
    $cert = $certs | Where-Object { $_.NotAfter -gt (Get-Date) } | Select-Object -First 1

    if (-not $cert) {
        Write-OutputMessage "Error: Valid certificate not found. Check if the certificate has expired or does not exist."
        throw "No valid certificate found."
    }
    Write-OutputMessage "Certificate with Subject '$certSubject' found. Valid until $($cert.NotAfter)."
    return $cert
}

Write-OutputMessage "Script started for signing '$exeFilePath'."

try {
    $cert = Load-Certificate
} catch {
    Write-OutputMessage "Error during certificate loading: $_"
    exit 1
}

try {
    $signingResult = Set-AuthenticodeSignature -FilePath $exeFilePath -Certificate $cert
    Write-OutputMessage "Signing Status: $($signingResult.Status)"
    
    if ($signingResult.Status -ne 'Valid') {
        Write-OutputMessage "Failed to apply a valid digital signature. Error: $($signingResult.Status)"
        exit 1
    } else {
        Write-OutputMessage "Successfully signed the executable: $exeFilePath"
    }
} catch {
    Write-OutputMessage "Exception occurred during signing: $_"
    exit 1
}

Write-OutputMessage "Script completed successfully."
