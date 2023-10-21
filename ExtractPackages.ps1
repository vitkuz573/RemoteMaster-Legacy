# Directory containing the script and csproj files
$solutionDir = "C:\Users\vitaly\source\repos\RemoteMaster"

# Hashtable to store unique packages
$packages = @{}

# Function to extract packages from a csproj file
function Extract-Packages {
    param (
        [string]$csprojPath
    )

    [xml]$csproj = Get-Content -Path $csprojPath
    $packageReferences = $csproj.Project.ItemGroup.PackageReference

    Write-Host "Analyzing $($csprojPath):" -ForegroundColor Magenta

    foreach ($packageRef in $packageReferences) {
        if ($null -ne $packageRef.Include) {
            $packages[$packageRef.Include] = $true
            Write-Host "  Found package: $($packageRef.Include)" -ForegroundColor DarkCyan
        } else {
            Write-Host "  Warning: Package reference without an 'Include' attribute found." -ForegroundColor Yellow
        }
    }
}

# Search for all csproj files in the directory
$csprojFiles = Get-ChildItem -Path $solutionDir -Recurse -Filter "*.csproj"

foreach ($csprojFile in $csprojFiles) {
    Extract-Packages -csprojPath $csprojFile.FullName
}

# Load the Directory.Packages.props file as XML
$packagePropsPath = "$solutionDir\Directory.Packages.props"
[xml]$packagePropsXml = Get-Content -Path $packagePropsPath

# Extract all PackageVersion elements
$packageVersions = $packagePropsXml.Project.ItemGroup.PackageVersion

# List to hold packages to remove
$packagesToRemove = @()

foreach ($packageVersion in $packageVersions) {
    # Check if the package is not in the hashtable of unique packages
    if (-not $packages.ContainsKey($packageVersion.Include)) {
        $packagesToRemove += $packageVersion
    }
}

if ($packagesToRemove.Count -gt 0) {
    Write-Host "The following unused packages are identified:" -ForegroundColor Yellow
    $packagesToRemove | ForEach-Object { Write-Host $_.Include -ForegroundColor Red }

    # Ask for confirmation
    $confirmation = Read-Host "Do you want to remove these packages? [y/N]"
    if ($confirmation -eq 'y') {
        # Remove unused packages from the XML
        $packagesToRemove | ForEach-Object { $packagePropsXml.Project.ItemGroup.RemoveChild($_) }

        # Formatting XML for better readability
        $settings = New-Object System.Xml.XmlWriterSettings
        $settings.Indent = $true
        $settings.IndentChars = "    "
        $settings.NewLineChars = "`r`n"
        $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
        $memoryStream = New-Object System.IO.MemoryStream
        $xmlWriter = [System.Xml.XmlWriter]::Create($memoryStream, $settings)

        $packagePropsXml.Save($xmlWriter)
        $xmlWriter.Flush()
        $memoryStream.Position = 0

        $formattedXml = New-Object System.IO.StreamReader -ArgumentList $memoryStream
        Set-Content -Path $packagePropsPath -Value $formattedXml.ReadToEnd()

        Write-Host "Removed the specified packages. Directory.Packages.props has been updated." -ForegroundColor Green
    } else {
        Write-Host "No changes made to Directory.Packages.props." -ForegroundColor Yellow
    }
} else {
    Write-Host "No unused packages found. Directory.Packages.props remains unchanged." -ForegroundColor Yellow
}
