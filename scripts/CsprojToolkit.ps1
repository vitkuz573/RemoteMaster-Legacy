# Finds all .csproj files in the sibling folders of the script's directory
function Find-CsprojFiles {
    $parentDir = Split-Path $PSScriptRoot -Parent
    $csprojFiles = Get-ChildItem -Path $parentDir -Recurse -Filter *.csproj
    return $csprojFiles
}

# Reads and analyzes the content of a .csproj file with comprehensive formatting
function Read-Csproj {
    param (
        [string]$Path
    )

    # Enhanced CSS for HTML Report
	$css = @"
<style>
    body { font-family: Arial, sans-serif; margin: 20px; }
    h1 { color: #333366; overflow-wrap: anywhere; }
    h2 { color: #666699; }
    table { border-collapse: collapse; width: 100%; margin-top: 20px; }
    th, td { border: 1px solid #999; padding: 8px; text-align: left; }
    th { background-color: #f2f2f2; }
    td { 
        max-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    td:hover {
        overflow: visible;
        white-space: normal;
        word-break: break-word;
    }
    .source { font-size: 0.85em; color: #707070; }
</style>
"@

    # Find Directory.Build.props in the solution directory or above
    function FindDirectoryBuildProps {
        param (
            [string]$currentDir
        )

        $dirBuildPropsPath = Join-Path $currentDir "Directory.Build.props"
        while ($currentDir -ne [IO.Path]::GetPathRoot($currentDir)) {
            if (Test-Path $dirBuildPropsPath) {
                return $dirBuildPropsPath
            }
            $currentDir = [IO.Path]::GetDirectoryName($currentDir)
            $dirBuildPropsPath = Join-Path $currentDir "Directory.Build.props"
        }
        return $null
    }

    $dirPackagesPath = Join-Path (Split-Path $Path -Parent) "..\Directory.Packages.props"
    $dirBuildPropsPath = FindDirectoryBuildProps (Split-Path $Path -Parent)

    $dirPackages = @{}
    if (Test-Path $dirPackagesPath) {
        [xml]$dirPackagesContent = Get-Content $dirPackagesPath
        foreach ($packageVersion in $dirPackagesContent.Project.ItemGroup.PackageVersion) {
            $dirPackages[$packageVersion.Include] = $packageVersion.Version
        }
    }

    # Reading Directory.Build.props if exists
    $dirBuildProps = @{}
    if ($dirBuildPropsPath -and (Test-Path $dirBuildPropsPath)) {
        [xml]$dirBuildPropsContent = Get-Content $dirBuildPropsPath
        foreach ($prop in $dirBuildPropsContent.Project.PropertyGroup.GetEnumerator()) {
            if ($prop -is [System.Xml.XmlElement]) {
                $dirBuildProps[$prop.Name] = $prop.InnerText
            }
        }
    }

    [xml]$csprojContent = Get-Content $Path
    Write-Host "Reading .csproj file: $Path" -ForegroundColor Cyan

    # Analyzing PropertyGroup
    $csprojProperties = @{}
    foreach ($group in $csprojContent.Project.PropertyGroup) {
        foreach ($prop in $group.GetEnumerator()) {
            if ($prop.InnerText.Trim() -ne '') {
                $csprojProperties[$prop.Name] = $prop.InnerText
                $propSource = if ($dirBuildProps[$prop.Name]) { " (from Directory.Build.props)" } else { " (from .csproj)" }
                $csprojProperties[$prop.Name] += $propSource
            }
        }
    }

    # Add properties from Directory.Build.props if not defined in .csproj
    foreach ($propName in $dirBuildProps.Keys) {
        if (-not $csprojProperties.ContainsKey($propName)) {
            $csprojProperties[$propName] = $dirBuildProps[$propName] + " (globally set in Directory.Build.props)"
        }
    }

	# Analyzing ItemGroup (for PackageReferences, etc.)
	$packageReferences = @()
	foreach ($group in $csprojContent.Project.ItemGroup) {
		foreach ($item in $group.GetEnumerator()) {
			if ($item.Name -eq "PackageReference") {
				$versionSource = ""
				$version = $dirPackages[$item.Include]
				if ($version) {
					$versionSource = " (from Directory.Packages.props)"
				} else {
					$version = $item.Version
					if ($version) {
						$versionSource = " (from .csproj)"
					} else {
						$versionSource = " (Not specified)"
					}
				}
				$packageReferences += [PSCustomObject]@{
					"Package" = $item.Include
					"Version" = $version + $versionSource
				}
			}
		}
	}

    # Generate HTML Report
	$htmlContent = "<html><head><title>CSProj Analysis</title>$css</head><body>"
	$htmlContent += "<h2>Analysis of .csproj File: <span style='overflow-wrap: anywhere;'>$Path</span></h2>"

    # Properties Section
    $htmlContent += "<h2>Properties</h2>"
    $htmlContent += "<table><tr><th>Property</th><th>Value</th><th>Source</th></tr>"
    foreach ($propName in $csprojProperties.Keys) {
        $valueAndSource = $csprojProperties[$propName] -split ' \(', 2
        $htmlContent += "<tr><td>$propName</td><td>$($valueAndSource[0])</td><td class='source'>($($valueAndSource[1])</td></tr>"
    }
    $htmlContent += "</table>"

	# Package References Section
	$htmlContent += "<h2>Package References</h2>"
	$htmlContent += "<table><tr><th>Package</th><th>Version</th><th>Source</th><th>NuGet Link</th></tr>"
	foreach ($packageRef in $packageReferences) {
		$versionAndSource = $packageRef.Version -split ' \(', 2
		$nugetUrl = "https://www.nuget.org/packages/$($packageRef.Package)/$($versionAndSource[0])"
		$htmlContent += "<tr><td>$($packageRef.Package)</td><td>$($versionAndSource[0])</td><td class='source'>($($versionAndSource[1])</td><td><a href='$nugetUrl'>NuGet Page</a></td></tr>"
	}
	$htmlContent += "</table>"

	# Closing HTML Tags
	$htmlContent += "</body></html>"

	# Save HTML Report
	$htmlFilePath = "$Path-analysis.html"
	$htmlContent | Out-File $htmlFilePath

	# Optional: Open HTML report in a browser
	Start-Process "chrome.exe" $htmlFilePath

	return $htmlFilePath
}

# Modifies a .csproj file based on provided modifications
function Modify-Csproj {
    param (
        [string]$Path,
        [hashtable]$Modifications
    )

    [xml]$csprojContent = Get-Content $Path

    # Apply modifications like adding or changing properties
    foreach ($key in $Modifications.Keys) {
        # Custom code for modifications goes here
    }

    $csprojContent.Save($Path)
}

# Automates build and deployment tasks
function Automate-Build {
    param (
        [string]$Path
    )

    dotnet build $Path
    # Uncomment the following line to include publishing
    # dotnet publish $Path
}

# Integrates with version control systems, such as Git
function Integrate-VersionControl {
    param (
        [string]$Path
    )

    # Example: updating version and committing changes
    git add $Path
    git commit -m "Auto-update version"
    git push
}

# Generates reports, e.g., dependency reports
function Generate-Report {
    param (
        [string]$Path
    )

    # Path to Directory.Packages.props
    $dirPackagesPath = Join-Path (Split-Path $Path -Parent) "..\Directory.Packages.props"
    $dirPackages = @{}
    if (Test-Path $dirPackagesPath) {
        [xml]$dirPackagesContent = Get-Content $dirPackagesPath
        foreach ($packageVersion in $dirPackagesContent.Project.ItemGroup.PackageVersion) {
            $dirPackages[$packageVersion.Include] = $packageVersion.Version
        }
    }

    [xml]$csprojContent = Get-Content $Path

    # Create a report about dependencies
    $report = @()
    foreach ($packageRef in $csprojContent.Project.ItemGroup.PackageReference) {
        # Ensure that $packageRef.Include is not null
        if ($packageRef.Include) {
            $version = if ($dirPackages.ContainsKey($packageRef.Include)) { $dirPackages[$packageRef.Include] } else { $packageRef.Version }
            if (-not $version) { $version = "Not specified in Directory.Packages.props or .csproj" }

            $versionSource = if ($dirPackages.ContainsKey($packageRef.Include)) { " (from Directory.Packages.props)" } elseif ($packageRef.Version) { " (specified in .csproj)" } else { "" }
            $reportLine = "Package: $($packageRef.Include), Version: $version$versionSource"
            $report += $reportLine
        }
    }

    # Output report
    foreach ($line in $report) {
        Write-Host $line -ForegroundColor White
    }
}

# Show interactive menu
function Show-Menu {
    param (
        [string]$Title = 'Csproj Toolkit Menu'
    )
    Clear-Host
    Write-Host "================ $Title ================" -ForegroundColor Cyan

    Write-Host "1: Find .csproj files" -ForegroundColor Yellow
    Write-Host "2: Read and analyze a .csproj file" -ForegroundColor Yellow
    Write-Host "3: Modify a .csproj file" -ForegroundColor Yellow
    Write-Host "4: Automate build and deployment" -ForegroundColor Yellow
    Write-Host "5: Integrate with version control" -ForegroundColor Yellow
    Write-Host "6: Generate a report" -ForegroundColor Yellow
    Write-Host "Q: Quit" -ForegroundColor Yellow
}

function Select-CsprojFile {
    $csprojFiles = Find-CsprojFiles
    Write-Host "Available .csproj files:" -ForegroundColor Cyan
    $index = 1
    foreach ($file in $csprojFiles) {
        Write-Host "${index}: $($file.FullName)" -ForegroundColor Green
        $index++
    }
    $selected = Read-Host "Select a .csproj file by number"
    if ($selected -ge 1 -and $selected -le $csprojFiles.Count) {
        return $csprojFiles[$selected - 1].FullName
    } else {
        Write-Host "Invalid selection, please try again." -ForegroundColor Red
        return $null
    }
}

function Invoke-MenuChoice {
    param (
        [char]$Choice
    )

    switch ($Choice) {
        '1' {
            $csprojFiles = Find-CsprojFiles
            Write-Host "Found .csproj files:" -ForegroundColor Green
            $csprojFiles | ForEach-Object { Write-Host $_.FullName -ForegroundColor White }
        }
        '2' {
            $path = Select-CsprojFile
            $output = Read-Csproj -Path $path
            Write-Host $output -ForegroundColor White
        }
        '3' {
            $path = Select-CsprojFile
            # Example modifications, this should be customized
            $modifications = @{ "PropertyExample" = "NewValue" }
            Modify-Csproj -Path $path -Modifications $modifications
            Write-Host "Modifications applied to $path" -ForegroundColor Green
        }
        '4' {
            $path = Select-CsprojFile
            Automate-Build -Path $path
        }
        '5' {
            $path = Select-CsprojFile
            Integrate-VersionControl -Path $path
        }
        '6' {
            $path = Select-CsprojFile
            $output = Generate-Report -Path $path
            Write-Host $output -ForegroundColor White
        }
        'Q' {
            return $true
        }
        default {
            Write-Host "Invalid choice, please try again." -ForegroundColor Red
        }
    }

    Write-Host "Press any key to return to the menu..." -ForegroundColor Magenta
    $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    return $false
}

# Main script execution
do {
    Show-Menu
    $choice = Read-Host "Please select an option"
    $quit = Invoke-MenuChoice -Choice $choice
} while (-not $quit)