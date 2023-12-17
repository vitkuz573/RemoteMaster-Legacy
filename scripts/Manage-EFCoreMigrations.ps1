# Ensure dotnet-ef tool is installed
if (-not (Get-Command "dotnet-ef" -ErrorAction SilentlyContinue)) {
    Write-Host "Installing dotnet-ef tool..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    $env:PATH += ";$($env:USERPROFILE)\.dotnet\tools"
}

function Invoke-EFCoreCommand {
    param (
        [string]$projectPath,
        [string]$command,
        [string]$context
    )
    $fullCommand = "dotnet ef $command --project $projectPath"
    if ($context) {
        $fullCommand += " --context $context"
    }
    if ($command -match "database drop") {
        $confirmForce = Read-Host "Are you sure you want to proceed with this operation? This will apply changes directly. [y/N]"
        if ($confirmForce -ne 'y') {
            Write-Host "Operation cancelled." -ForegroundColor Yellow
            return
        }
        $fullCommand += " --force"
    } else {
        $confirm = Read-Host "Are you sure you want to proceed with this operation? [y/N]"
        if ($confirm -ne 'y') {
            Write-Host "Operation cancelled." -ForegroundColor Yellow
            return
        }
    }

    Clear-Host
	
    Write-Host "Selected DbContext: $context" -ForegroundColor Green
    Write-Host "Running command: $fullCommand" -ForegroundColor Cyan

    try {
        $output = Invoke-Expression $fullCommand
        if ($command -eq "migrations list") {
            $migrations = $output -split "`r`n" | Where-Object { $_ -match '^[0-9]{14}_' }
            $formattedOutput = foreach ($migration in $migrations) {
                if ($migration -match "\(Pending\)") {
                    $status = "Pending"
                    $migration = $migration -replace "\(Pending\)", ""
                } else {
                    $status = "Applied"
                }
                [PSCustomObject]@{
                    Migration = $migration
                    Status    = $status
                }
            }
            $formattedOutput | Format-Table -AutoSize
        } else {
            [PSCustomObject]@{
                Result = $output
            } | Format-List
        }
    } catch {
        Write-Host "An error occurred during the operation." -ForegroundColor Red
    }

    # Prompt to continue
    $null = Read-Host "Press Enter to continue..."
}

function Get-EFCoreDbContexts {
    param (
        [string]$projectPath
    )
    Write-Host "Discovering EF Core DbContexts in project $projectPath..." -ForegroundColor Magenta
    $dbContexts = & dotnet ef dbcontext list --project $projectPath
    $dbContextNames = $dbContexts | Where-Object { $_ -and $_ -match '^[A-Za-z0-9._]+\.[A-Za-z0-9._]+$' } | Select-Object -Unique
    return $dbContextNames
}

function Select-DbContext {
    param (
        [string[]]$dbContexts
    )
    $index = 0
    foreach ($context in $dbContexts) {
        Write-Host "[$index] $context"
        $index++
    }
    
    $selectedIndex = Read-Host "Enter the number of the DbContext"
    return $dbContexts[$selectedIndex]
}

Write-Host "Entity Framework Core Migration Management" -ForegroundColor Yellow

# Assume we're in the 'scripts' directory and the solution is one level up.
$solutionPath = Resolve-Path "..\"

# Get all project files
$projectFiles = Get-ChildItem -Path $solutionPath -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName

# Select a project
$index = 0
foreach ($project in $projectFiles) {
    Write-Host "[$index] $project"
    $index++
}
$selectedProjectIndex = Read-Host "Enter the number of the project to manage"
$selectedProject = $projectFiles[$selectedProjectIndex]

# Get DbContexts for the selected project
$dbContexts = Get-EFCoreDbContexts -projectPath $selectedProject

if ($dbContexts -and $dbContexts.Length -gt 0) {
    # Select a DbContext
    $selectedDbContext = Select-DbContext -dbContexts $dbContexts

    # Perform EF Core commands
    $exit = $false
    do {
        Clear-Host
        Write-Host "Selected DbContext: $selectedDbContext" -ForegroundColor Green
        Write-Host "Available actions:" -ForegroundColor Cyan
        Write-Host "1. Add Migration"
        Write-Host "2. Update Database"
        Write-Host "3. Remove Database"
        Write-Host "4. List Migrations"
        Write-Host "5. Exit"
        $action = Read-Host "Select action"


        switch ($action) {
            '1' {
                # Add Migration
                $migrationName = Read-Host "Enter the name for the new migration"
                Invoke-EFCoreCommand -projectPath $selectedProject -command "migrations add $migrationName --output-dir Data/Migrations" -context $selectedDbContext
            }
            '2' {
                # Update Database
                Invoke-EFCoreCommand -projectPath $selectedProject -command "database update" -context $selectedDbContext
            }
            '3' {
                # Remove Database
                Invoke-EFCoreCommand -projectPath $selectedProject -command "database drop" -context $selectedDbContext
            }
            '4' {
                # List Migrations
                Invoke-EFCoreCommand -projectPath $selectedProject -command "migrations list" -context $selectedDbContext
            }
            '5' {
                $exit = $true
            }
        }
    } while (-not $exit)
} else {
    Write-Host "No DbContexts found in project $selectedProject." -ForegroundColor Red
}

Write-Host "Migration management complete." -ForegroundColor Green
