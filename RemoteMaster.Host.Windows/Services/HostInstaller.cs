// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Compression;
using System.Security.Cryptography;
using Windows.Win32.Foundation;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostInstaller(INetworkDriveService networkDriveService, IHostInformationService hostInformationService, IHostConfigurationService hostConfigurationService, IServiceFactory serviceFactory, IHostLifecycleService hostLifecycleService, IFileSystem fileSystem) : IHostInstaller
{
    private readonly string _applicationDirectory = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");

    public async Task InstallAsync(string? modulesPath, string? username, string? password)
    {
        try
        {
            if (modulesPath != null)
            {
                var isNetworkPath = modulesPath.StartsWith(@"\\");

                if (isNetworkPath)
                {
                    if (!MapNetworkDriveAsync(modulesPath, username, password))
                    {
                        Log.Information("Install aborted.");

                        return;
                    }
                }

                var modulesDirectory = Path.Combine(modulesPath, "Modules");

                if (!Directory.Exists(modulesDirectory))
                {
                    Log.Error("Modules directory not found at path: {ModulesDirectory}", modulesDirectory);
                    return;
                }

                var modulesFolderPath = Path.Combine(_applicationDirectory, "Modules");

                if (!Directory.Exists(modulesFolderPath))
                {
                    Directory.CreateDirectory(modulesFolderPath);
                }

                var zipFiles = Directory.GetFiles(modulesDirectory, "*.zip");

                foreach (var zipFile in zipFiles)
                {
                    InstallModuleAsync(zipFile, modulesFolderPath);
                }

                if (isNetworkPath)
                {
                    UnmapNetworkDriveAsync(modulesPath);
                }
            }

            var hostInformation = hostInformationService.GetHostInformation();
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();

            Log.Information("Starting installation...");
            Log.Information("Server: {Server}", hostConfiguration.Server);
            Log.Information("Host Name: {HostName}, IP Address: {IPAddress}, MAC Address: {MacAddress}", hostInformation.Name, hostInformation.IpAddress, hostInformation.MacAddress);

            var organizationalUnits = string.Join(", ", hostConfiguration.Subject.OrganizationalUnit.Select(ou => $"OU={ou}"));
            Log.Information("Distinguished Name: CN={CommonName}, O={Organization}, {OrganizationalUnits}", hostInformation.Name, hostConfiguration.Subject.Organization, organizationalUnits);

            var hostService = serviceFactory.GetService("RCHost");

            if (hostService.IsInstalled)
            {
                hostService.Stop();
                CopyToTargetPath(_applicationDirectory);
            }
            else
            {
                CopyToTargetPath(_applicationDirectory);
                hostService.Create();
            }

            hostConfiguration.Host = hostInformation;

            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

            Log.Information("{ServiceName} installed and started successfully.", hostService.Name);

            await hostLifecycleService.RegisterAsync();
            await hostLifecycleService.GetCaCertificateAsync();

            var organizationAddress = await hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration);

            await hostLifecycleService.IssueCertificateAsync(hostConfiguration, organizationAddress);

            hostService.Start();
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    private void InstallModuleAsync(string zipFilePath, string modulesFolderPath)
    {
        try
        {
            var moduleName = Path.GetFileNameWithoutExtension(zipFilePath);

            var moduleTargetPath = Path.Combine(modulesFolderPath, moduleName);

            if (Directory.Exists(moduleTargetPath))
            {
                Directory.Delete(moduleTargetPath, true);
            }

            ZipFile.ExtractToDirectory(zipFilePath, moduleTargetPath);

            Log.Information("Module {ModuleName} extracted to {TargetPath}", moduleName, moduleTargetPath);

            var exeFileName = $"RemoteMaster.{moduleName}.exe";
            var exeFilePath = Path.Combine(moduleTargetPath, exeFileName);

            if (!fileSystem.File.Exists(exeFilePath))
            {
                throw new InvalidOperationException($"Module {moduleName} does not contain the required executable file: {exeFileName}");
            }

            Log.Information("Module {ModuleName} installed successfully with executable {Executable}.", moduleName, exeFileName);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to install module from {ZipFilePath}: {ErrorMessage}", zipFilePath, ex.Message);
        }
    }

    private bool MapNetworkDriveAsync(string folderPath, string? username, string? password)
    {
        Log.Information($"Attempting to map network drive with remote path: {folderPath}");
        var isMapped = networkDriveService.MapNetworkDrive(folderPath, username, password);

        if (!isMapped)
        {
            Log.Error($"Failed to map network drive with remote path {folderPath}.");
            Log.Error("Unable to map network drive with the provided credentials.");
        }
        else
        {
            Log.Information($"Successfully mapped network drive with remote path: {folderPath}");
        }

        return isMapped;
    }

    private void UnmapNetworkDriveAsync(string folderPath)
    {
        Log.Information($"Attempting to unmap network drive with remote path: {folderPath}");
        var isCancelled = networkDriveService.CancelNetworkDrive(folderPath);

        if (!isCancelled)
        {
            Log.Error($"Failed to unmap network drive with remote path {folderPath}.");
        }
        else
        {
            Log.Information($"Successfully unmapped network drive with remote path: {folderPath}");
        }
    }

    private void CopyToTargetPath(string targetDirectoryPath)
    {
        if (!fileSystem.Directory.Exists(targetDirectoryPath))
        {
            fileSystem.Directory.CreateDirectory(targetDirectoryPath);
        }

        var targetExecutablePath = fileSystem.Path.Combine(targetDirectoryPath, "RemoteMaster.Host.exe");

        try
        {
            fileSystem.File.Copy(Environment.ProcessPath!, targetExecutablePath, true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy the executable to {targetExecutablePath}. Details: {ex.Message}", ex);
        }

        var sourceDirectoryPath = fileSystem.Path.GetDirectoryName(Environment.ProcessPath)!;

        if (fileSystem.File.Exists(sourceDirectoryPath))
        {
            fileSystem.File.Copy(sourceDirectoryPath, targetDirectoryPath, true);
        }
    }

    private static async Task<bool> CopyDirectoryAsync(string sourceDir, string destDir, bool overwrite = false)
    {
        try
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}.");
            }

            var dirs = dir.GetDirectories();

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in dir.GetFiles())
            {
                if (file.Name.Equals("RemoteMaster.Host.json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var tempPath = Path.Combine(destDir, file.Name);
                var copiedSuccessfully = await TryCopyFileAsync(file.FullName, tempPath, overwrite);

                if (copiedSuccessfully)
                {
                    continue;
                }

                Log.Error($"File {file.Name} copied with errors. Checksum does not match.");

                return false;
            }

            foreach (var subdir in dirs)
            {
                var tempPath = Path.Combine(destDir, subdir.Name);

                if (!await CopyDirectoryAsync(subdir.FullName, tempPath, overwrite))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to copy directory {sourceDir} to {destDir}: {ex.Message}");

            return false;
        }
    }

    private static async Task<bool> TryCopyFileAsync(string sourceFile, string destFile, bool overwrite)
    {
        var attempts = 0;

        while (true)
        {
            try
            {
                File.Copy(sourceFile, destFile, overwrite);

                if (VerifyChecksum(sourceFile, destFile))
                {
                    break;
                }

                Log.Error($"Checksum verification failed for file {sourceFile}.");

                return false;
            }
            catch (IOException ex) when (ex.HResult == (int)WIN32_ERROR.ERROR_SHARING_VIOLATION)
            {
                if (++attempts == 5)
                {
                    throw;
                }

                Log.Warning($"File {sourceFile} is currently in use. Retrying in 1 second...");
                await Task.Delay(1000);
            }
        }

        return true;
    }

    private static bool VerifyChecksum(string sourceFilePath, string destFilePath, bool expectDifference = false)
    {
        var sourceChecksum = GenerateChecksum(sourceFilePath);
        var destChecksum = GenerateChecksum(destFilePath);

        var checksumMatch = sourceChecksum == destChecksum;

        Log.Information($"Verifying checksum: {sourceFilePath} [Source Checksum: {sourceChecksum}] -> {destFilePath} [Destination Checksum: {destChecksum}].");

        switch (expectDifference)
        {
            case true when !checksumMatch:
                Log.Information("Checksums do not match as expected for an update. An update is needed.");
                return false;
            case false when !checksumMatch:
                Log.Error("Unexpected checksum mismatch. The files may have been tampered with or corrupted.");
                return false;
            default:
                Log.Information("Checksum verification successful. No differences found.");
                return true;
        }
    }

    private static string GenerateChecksum(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
