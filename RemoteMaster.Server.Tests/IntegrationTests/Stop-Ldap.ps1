# Stop-Ldap.ps1

Write-Output "Stopping LDAP container for integration tests..."
docker-compose -f ./docker-compose.yml down
