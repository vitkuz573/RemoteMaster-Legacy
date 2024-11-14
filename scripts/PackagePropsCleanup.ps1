# Solution Directory
$currentScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionDir = Resolve-Path "$currentScriptDir\.."

# Hashtable to store unique packages
$packages = @{}

# Function to extract packages from XML files
function Extract-PackagesFromXml {
    param (
        [string]$xmlFilePath
    )

    [xml]$xmlContent = Get-Content -Path $xmlFilePath
    $packageReferences = $xmlContent.Project.ItemGroup.PackageReference

    Write-Host "Analyzing $($xmlFilePath):" -ForegroundColor Magenta

    # Check if there are no package references in the file
    if ($packageReferences.Count -eq 0) {
        Write-Host "  No packages found in this file." -ForegroundColor Yellow
        return
    }

    foreach ($packageRef in $packageReferences) {
        if ($packageRef.NodeType -eq "Element" -and $null -ne $packageRef.Include) {
            $packages[$packageRef.Include] = $true
            Write-Host "  Found package: $($packageRef.Include)" -ForegroundColor DarkCyan
        }
    }
}

# Search for all csproj files and Directory.Build.props
$xmlFiles = @(
    Get-ChildItem -Path $solutionDir -Recurse -Filter "*.csproj" |
    Select-Object -ExpandProperty FullName
)
$directoryBuildProps = Get-ChildItem -Path $solutionDir -Recurse -Filter "Directory.Build.props" |
    Select-Object -ExpandProperty FullName

if ($null -ne $directoryBuildProps) {
    $xmlFiles += $directoryBuildProps
}

foreach ($xmlFile in $xmlFiles) {
    Extract-PackagesFromXml -xmlFilePath $xmlFile
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

Write-Host ""

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
        $settings.IndentChars = "  "
        $settings.NewLineChars = "`r`n"
        $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
        $memoryStream = New-Object System.IO.MemoryStream
        $xmlWriter = [System.Xml.XmlWriter]::Create($memoryStream, $settings)

        $packagePropsXml.Save($xmlWriter)
        $xmlWriter.Flush()
        $memoryStream.Position = 0

        $formattedXml = New-Object System.IO.StreamReader -ArgumentList $memoryStream
        $formattedXmlContent = $formattedXml.ReadToEnd().TrimEnd()

        Set-Content -Path $packagePropsPath -Value $formattedXmlContent

        Write-Host "Removed the specified packages. Directory.Packages.props has been updated." -ForegroundColor Green
    } else {
        Write-Host "No changes made to Directory.Packages.props." -ForegroundColor Yellow
    }
} else {
    Write-Host "No unused packages found. Directory.Packages.props remains unchanged." -ForegroundColor Yellow
}
