# Start-Ldap.ps1

Write-Output "Starting LDAP container for integration tests..."

docker-compose -f ./docker-compose.yml up -d

Start-Sleep -Seconds 5

$containerStatus = docker inspect -f '{{.State.Running}}' openldap-server
if ($containerStatus -eq "true") {
    Write-Output "LDAP container started successfully."
} else {
    Write-Error "Failed to start LDAP container."
}
