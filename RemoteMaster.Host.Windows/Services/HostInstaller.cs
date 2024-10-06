// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;
using Serilog;
using Windows.Win32.Foundation;

namespace RemoteMaster.Host.Windows.Services;

public class HostInstaller(INetworkDriveService networkDriveService, IHostInformationService hostInformationService, IHostConfigurationService hostConfigurationService, IServiceFactory serviceFactory, IHostLifecycleService hostLifecycleService, IFileSystem fileSystem) : IHostInstaller
{
    private readonly string _applicationDirectory = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");

    public async Task InstallAsync(HostInstallRequest installRequest)
    {
        ArgumentNullException.ThrowIfNull(installRequest);

        try
        {
            if (installRequest.ModulesPath != null)
            {
                var isNetworkPath = installRequest.ModulesPath.StartsWith(@"\\");

                if (isNetworkPath)
                {
                    if (!MapNetworkDriveAsync(installRequest.ModulesPath, installRequest.UserCredentials))
                    {
                        Log.Information("Install aborted.");

                        return;
                    }
                }

                var modulesDirectory = Path.Combine(installRequest.ModulesPath, "Modules");

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
                    await InstallOrUpdateModuleAsync(zipFile, modulesFolderPath);
                }

                if (isNetworkPath)
                {
                    UnmapNetworkDriveAsync(installRequest.ModulesPath);
                }
            }

            var hostInformation = hostInformationService.GetHostInformation();

            Log.Information("Starting installation...");
            Log.Information("Server: {Server}", installRequest.Server);
            Log.Information("Host Name: {HostName}, IP Address: {IPAddress}, MAC Address: {MacAddress}", hostInformation.Name, hostInformation.IpAddress, hostInformation.MacAddress);
            Log.Information("Distinguished Name: CN={CommonName}, O={Organization}, OU={OrganizationalUnit}", hostInformation.Name, installRequest.Organization, installRequest.OrganizationalUnit);

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

            var subject = new SubjectDto(installRequest.Organization, [installRequest.OrganizationalUnit]);

            var hostConfiguration = new HostConfiguration(installRequest.Server, subject, hostInformation);

            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

            Log.Information("{ServiceName} installed and started successfully.", hostService.Name);

            await hostLifecycleService.RegisterAsync();
            await hostLifecycleService.GetCaCertificateAsync();

            var organizationAddress = await hostLifecycleService.GetOrganizationAddressAsync(installRequest.Organization);

            await hostLifecycleService.IssueCertificateAsync(hostConfiguration, organizationAddress);

            hostService.Start();
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    private static async Task InstallOrUpdateModuleAsync(string zipFilePath, string modulesFolderPath)
    {
        try
        {
            var moduleInfo = await GetModuleInfoFromZipAsync(zipFilePath);

            if (moduleInfo == null)
            {
                Log.Error("No module info found in zip file: {ZipFilePath}", zipFilePath);
                
                return;
            }

            using (var zipArchive = ZipFile.OpenRead(zipFilePath))
            {
                var entryPoint = zipArchive.GetEntry(moduleInfo.EntryPoint);

                if (entryPoint == null)
                {
                    Log.Error("Module {ModuleName} does not contain the required entry point: {EntryPoint}", moduleInfo.Name, moduleInfo.EntryPoint);
                    
                    return;
                }
            }

            var moduleTargetPath = Path.Combine(modulesFolderPath, moduleInfo.Name);
            var installedModuleInfo = await GetInstalledModuleInfoAsync(moduleTargetPath);

            if (installedModuleInfo != null && moduleInfo.Version <= installedModuleInfo.Version)
            {
                Log.Information("Module {ModuleName} is already up-to-date. Skipping.", moduleInfo.Name);
                
                return;
            }

            if (Directory.Exists(moduleTargetPath))
            {
                Directory.Delete(moduleTargetPath, true);
            }

            ZipFile.ExtractToDirectory(zipFilePath, moduleTargetPath);
            
            Log.Information("Module {ModuleName} extracted to {TargetPath}", moduleInfo.Name, moduleTargetPath);
            Log.Information("Module {ModuleName} installed successfully with entry point {Executable}.", moduleInfo.Name, moduleInfo.EntryPoint);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to install module from {ZipFilePath}: {ErrorMessage}", zipFilePath, ex.Message);
        }
    }

    private bool MapNetworkDriveAsync(string folderPath, Credentials? userCredentials)
    {
        Log.Information($"Attempting to map network drive with remote path: {folderPath}");

        var isMapped = networkDriveService.MapNetworkDrive(folderPath, userCredentials?.UserName, userCredentials?.Password);

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

    private static bool VerifyChecksum(string sourceFilePath, string destFilePath)
    {
        var sourceChecksum = GenerateChecksum(sourceFilePath);
        var destChecksum = GenerateChecksum(destFilePath);

        Log.Information($"Verifying checksum: {sourceFilePath} [Source Checksum: {sourceChecksum}] -> {destFilePath} [Destination Checksum: {destChecksum}].");

        if (sourceChecksum != destChecksum)
        {
            Log.Error("Checksum mismatch. The files may have been tampered with or corrupted.");

            return false;
        }

        Log.Information("Checksum verification successful. No differences found.");

        return true;
    }

    private static string GenerateChecksum(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static async Task<ModuleInfo?> GetModuleInfoFromZipAsync(string zipFilePath)
    {
        try
        {
            using var zipArchive = ZipFile.OpenRead(zipFilePath);
            var entry = zipArchive.GetEntry("module-info.json");

            if (entry == null)
            {
                return null;
            }

            await using var stream = entry.Open();

            return await JsonSerializer.DeserializeAsync<ModuleInfo>(stream);
        }
        catch (Exception ex)
        {
            Log.Error("Error reading module info from {ZipFilePath}: {ErrorMessage}", zipFilePath, ex.Message);

            return null;
        }
    }

    private static async Task<ModuleInfo?> GetInstalledModuleInfoAsync(string installedModulePath)
    {
        var moduleInfoPath = Path.Combine(installedModulePath, "module-info.json");

        if (!File.Exists(moduleInfoPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(moduleInfoPath);

        return JsonSerializer.Deserialize<ModuleInfo>(json);
    }
}
