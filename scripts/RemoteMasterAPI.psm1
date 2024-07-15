$Global:BaseUrl = ""

function Set-RMBaseUrl {
    param (
        [string]$baseUrl
    )
    $Global:BaseUrl = $baseUrl
}

function Get-RMCertificateCA {
    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Certificate/ca" -Method Get -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json" }
        return $response
    } catch {
        Write-Error "Failed to get Certificate CA: $_"
    }
}

function New-RMCertificate {
    param (
        [string]$certificateData
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Certificate/issue" -Method Post -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json"; "Content-Type" = "application/vnd.remotemaster.v1+json" } -Body $certificateData
        return $response
    } catch {
        Write-Error "Failed to issue Certificate: $_"
    }
}

function Get-RMCrl {
    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Crl" -Method Get -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json" }
        return $response
    } catch {
        Write-Error "Failed to get CRL: $_"
    }
}

function Get-RMCrlMetadata {
    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Crl/metadata" -Method Get -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json" }
        return $response
    } catch {
        Write-Error "Failed to get CRL metadata: $_"
    }
}

function Register-RMHost {
    param (
        [PSCustomObject]$hostConfiguration
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Host/register" -Method Post -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json"; "Content-Type" = "application/vnd.remotemaster.v1+json" } -Body ($hostConfiguration | ConvertTo-Json -Depth 10)
        return $response
    } catch {
        Write-Error "Failed to register host: $_"
    }
}

function Get-RMHostStatus {
    param (
        [string]$macAddress
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Host/status?macAddress=$macAddress" -Method Get -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json" }
        return $response
    } catch {
        Write-Error "Failed to get host status: $_"
    }
}

function Unregister-RMHost {
    param (
        [PSCustomObject]$hostUnregisterRequest
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Host/unregister" -Method Delete -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json"; "Content-Type" = "application/vnd.remotemaster.v1+json" } -Body ($hostUnregisterRequest | ConvertTo-Json -Depth 10)
        return $response
    } catch {
        Write-Error "Failed to unregister host: $_"
    }
}

function Update-RMHost {
    param (
        [PSCustomObject]$hostUpdateRequest
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Host/update" -Method Put -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json"; "Content-Type" = "application/vnd.remotemaster.v1+json" } -Body ($hostUpdateRequest | ConvertTo-Json -Depth 10)
        return $response
    } catch {
        Write-Error "Failed to update host: $_"
    }
}

function Get-RMHostConfiguration {
    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/HostConfiguration/downloadHost" -Method Get -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json" }
        return $response
    } catch {
        Write-Error "Failed to download host configuration: $_"
    }
}

function New-RMHostConfiguration {
    param (
        [PSCustomObject]$hostConfigurationRequest
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/HostConfiguration/generate" -Method Post -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json"; "Content-Type" = "application/vnd.remotemaster.v1+json" } -Body ($hostConfigurationRequest | ConvertTo-Json -Depth 10)
        return $response
    } catch {
        Write-Error "Failed to generate host configuration: $_"
    }
}

function Get-RMHostMove {
    param (
        [string]$macAddress
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/HostMove?macAddress=$macAddress" -Method Get -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json" }
        return $response
    } catch {
        Write-Error "Failed to get host move: $_"
    }
}

function New-RMHostMoveAcknowledgement {
    param (
        [string]$hostMoveData
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/HostMove/acknowledge" -Method Post -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json"; "Content-Type" = "application/vnd.remotemaster.v1+json" } -Body $hostMoveData
        return $response
    } catch {
        Write-Error "Failed to acknowledge host move: $_"
    }
}

function Get-RMJwtToken {
    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/api/Jwt" -Method Get -Headers @{ "Accept" = "application/vnd.remotemaster.v1+json" }
        return $response
    } catch {
        Write-Error "Failed to get JWT: $_"
    }
}

function Remove-RMAccount {
    param (
        [string]$returnUrl
    )

    try {
        $response = Invoke-RestMethod -Uri "$Global:BaseUrl/Account/Logout" -Method Post -Headers @{ "Accept" = "multipart/form-data"; "Content-Type" = "application/x-www-form-urlencoded" } -Body @{ "returnUrl" = $returnUrl }
        return $response
    } catch {
        Write-Error "Failed to logout account: $_"
    }
}

Export-ModuleMember -Function *