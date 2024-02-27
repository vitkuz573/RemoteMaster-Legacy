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

        Console.WriteLine("kek");
        Console.WriteLine("kek1");
        Console.WriteLine("kek2");

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

            userInstanceService.Stop();

            var contentBuilder = new StringBuilder();
            contentBuilder.AppendLine("$filesLocked = $true");
            contentBuilder.AppendLine("Write-Host 'Checking if files are locked'");
            contentBuilder.AppendLine("$filesLocked = $true");
            contentBuilder.AppendLine("while ($filesLocked) {");
            contentBuilder.AppendLine("    Write-Host 'Waiting for files to be unlocked'");
            contentBuilder.AppendLine("    Start-Sleep -Seconds 2");
            contentBuilder.AppendLine("    $filesLocked = $false");
            contentBuilder.AppendLine("    Get-ChildItem \"$PSScriptRoot\\Update\" -Recurse | Where-Object { !$_.PSIsContainer } | ForEach-Object {");
            contentBuilder.AppendLine("        try {");
            contentBuilder.AppendLine("            Write-Host \"Attempting to open file: $($_.FullName)\"");
            contentBuilder.AppendLine("            $stream = [System.IO.File]::Open($_.FullName, 'Open', 'Write', 'None')");
            contentBuilder.AppendLine("            $stream.Close()");
            contentBuilder.AppendLine("            Write-Host \"Successfully opened and closed file: $($_.FullName)\" -ForegroundColor Green");
            contentBuilder.AppendLine("        } catch [UnauthorizedAccessException] {");
            contentBuilder.AppendLine("            Write-Host \"Access denied for file: $($_.FullName). Update for this file is skipped.\" -ForegroundColor Red");
            contentBuilder.AppendLine("        } catch {");
            contentBuilder.AppendLine("            Write-Host \"Encountered an error with file: $($_.FullName). Retrying...\" -ForegroundColor Yellow");
            contentBuilder.AppendLine("            $filesLocked = $true");
            contentBuilder.AppendLine("        }");
            contentBuilder.AppendLine("    }");
            contentBuilder.AppendLine("    if (-not $filesLocked) {");
            contentBuilder.AppendLine("        Write-Host 'All files are unlocked, proceeding with copy operation' -ForegroundColor Green");
            contentBuilder.AppendLine("    }");
            contentBuilder.AppendLine("}");
            contentBuilder.AppendLine("Write-Host 'Starting copy operation'");
            contentBuilder.AppendLine("Copy-Item -Path \"$PSScriptRoot\\Update\\*.*\" -Destination $PSScriptRoot -Recurse -Force");
            contentBuilder.AppendLine("Write-Host 'Copy operation completed successfully' -ForegroundColor Green");

            await File.WriteAllTextAsync(_scriptPath, contentBuilder.ToString());
            Log.Information("Updater script created at: {ScriptPath}", _scriptPath);

            var processStartInfo = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -NoProfile -File \"{_scriptPath}\"")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            await process.WaitForExitAsync();

            hostService.Start();

            Log.Information("Executed updater script: {ScriptPath}", _scriptPath);
        }
        catch (Exception ex)
        {
            Log.Error("Error while updating host: {Message}", ex.Message);
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
