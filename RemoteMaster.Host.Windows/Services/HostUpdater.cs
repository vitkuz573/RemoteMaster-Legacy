// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Text;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostUpdater(INetworkDriveService networkDriveService, IUserInstanceService userInstanceService, IServiceFactory serviceFactory) : IHostUpdater
{
    private static readonly string BaseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");
    
    private readonly string _scriptPath = Path.Combine(BaseFolderPath, "update.ps1");
    private readonly string _updateFolderPath = Path.Combine(BaseFolderPath, "Update");

    public async Task UpdateAsync(string folderPath, string? username, string? password)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        try
        {
            var sourceFolderPath = Path.Combine(folderPath, "Host");
            var isNetworkPath = folderPath.StartsWith(@"\\");

            if (isNetworkPath)
            {
                networkDriveService.MapNetworkDrive(folderPath, username, password);
            }

            var isDownloaded = DirectoryCopy(sourceFolderPath, _updateFolderPath, true, true);

            Log.Information("Copied from {SourceFolder} to {DestinationFolder}", sourceFolderPath, _updateFolderPath);

            if (isNetworkPath)
            {
                networkDriveService.CancelNetworkDrive(folderPath);
            }

            if (!isDownloaded)
            {
                return;
            }

            var hostService = serviceFactory.GetService("RCHost");

            hostService.Stop();

            Console.WriteLine($"{hostService.Name} sucessfully stopped. Starting update...");

            userInstanceService.Stop();

            var contentBuilder = new StringBuilder();
            contentBuilder.AppendLine("$filesLocked = $true");
            contentBuilder.AppendLine("while ($filesLocked) {");
            contentBuilder.AppendLine("    Start-Sleep -Seconds 2");
            contentBuilder.AppendLine("    $filesLocked = $false");
            contentBuilder.AppendLine("    Get-ChildItem $PSScriptRoot | Where-Object { !$_.PSIsContainer } | ForEach-Object {");
            contentBuilder.AppendLine("        try {");
            contentBuilder.AppendLine("            $stream = [System.IO.File]::Open($_.FullName, 'Open', 'Write', 'None')");
            contentBuilder.AppendLine("            $stream.Close()");
            contentBuilder.AppendLine("        } catch [UnauthorizedAccessException] {");
            contentBuilder.AppendLine("            Write-Host \"Access denied for file: $($_.FullName). Update for this file is skipped.\" -ForegroundColor Red");
            contentBuilder.AppendLine("        } catch {");
            contentBuilder.AppendLine("            $filesLocked = $true");
            contentBuilder.AppendLine("        }");
            contentBuilder.AppendLine("    }");
            contentBuilder.AppendLine("}");
            contentBuilder.AppendLine("Copy-Item -Path \"$PSScriptRoot\\Update\\*.*\" -Destination $PSScriptRoot -Recurse -Force");
            await File.WriteAllTextAsync(_scriptPath, contentBuilder.ToString());
            Log.Information("Updater script created at: {ScriptPath}", _scriptPath);

            var processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -NoProfile -File \"{_scriptPath}\"")
            {
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            await process.WaitForExitAsync();

            hostService.Start();

            var servicesToStart = new IRunnable[] { hostService, userInstanceService };
            await EnsureServicesRunning(servicesToStart, 5, 5);

            Log.Information("Executed updater script: {ScriptPath}", _scriptPath);
        }
        catch (Exception ex)
        {
            Log.Error("Error while updating host: {Message}", ex.Message);
        }
    }

    private async Task EnsureServicesRunning(IEnumerable<IRunnable> services, int delayInSeconds, int attempts)
    {
        var allServicesRunning = false;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            Log.Information($"Attempt {attempt}: Checking if services are running...");
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));

            allServicesRunning = services.All(service => service.IsRunning);

            if (!allServicesRunning)
            {
                Log.Warning("Not all services are running. Waiting and retrying...");
            }
            else
            {
                Log.Information("All services have been successfully started.");
                break;
            }
        }

        if (!allServicesRunning)
        {
            Log.Error("Failed to start all services after {Attempts} attempts. Initiating emergency recovery...", attempts);

            AttemptEmergencyRecovery();
        }
    }

    private void AttemptEmergencyRecovery()
    {
        try
        {
            var sourceExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");
            var destinationExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");

            File.Copy(sourceExePath, destinationExePath, true);

            Log.Information("Emergency recovery completed successfully. Attempting to restart services...");

            var hostService = serviceFactory.GetService("RCHost");

            hostService.Start();
        }
        catch (Exception ex)
        {
            Log.Error($"Emergency recovery failed: {ex.Message}");
        }
    }

    private static bool DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true, bool overwriteExisting = false)
    {
        var sourceDir = new DirectoryInfo(sourceDirName);

        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        foreach (var file in sourceDir.GetFiles())
        {
            var destPath = Path.Combine(destDirName, file.Name);

            if (File.Exists(destPath) && !overwriteExisting)
            {
                continue;
            }

            try
            {
                file.CopyTo(destPath, true);
            }
            catch (Exception)
            {
                return false;
            }
        }

        if (!copySubDirs)
        {
            return true;
        }

        foreach (var subdir in sourceDir.GetDirectories())
        {
            var destSubDir = Path.Combine(destDirName, subdir.Name);

            if (!DirectoryCopy(subdir.FullName, destSubDir, true, overwriteExisting))
            {
                return false;
            }
        }

        return true;
    }
}
