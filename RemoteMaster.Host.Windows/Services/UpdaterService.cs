// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Text;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterService : IUpdaterService
{
    private readonly string _baseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");
    private readonly string _scriptPath;
    private readonly string _updateFolderPath;

    private readonly INetworkDriveService _networkDriveService;
    private readonly IServiceConfiguration _hostServiceConfig;

    public UpdaterService(INetworkDriveService networkDriveService, IServiceConfiguration hostServiceConfig)
    {
        _scriptPath = Path.Combine(_baseFolderPath, "update.ps1");
        _updateFolderPath = Path.Combine(_baseFolderPath, "Update");

        _networkDriveService = networkDriveService;
        _hostServiceConfig = hostServiceConfig;
    }

    public void Execute(string folderPath, string username, string password, bool isLocalFolder)
    {
        try
        {
            var sourceFolderPath = Path.Combine(folderPath, "Host");

            if (!isLocalFolder)
            {
                _networkDriveService.MapNetworkDrive(folderPath, username, password);
            }

            var isDownloaded = DirectoryCopy(sourceFolderPath, _updateFolderPath, true, true);

            Log.Information("Copied from {SourceFolder} to {DestinationFolder}", sourceFolderPath, _updateFolderPath);

            if (!isLocalFolder)
            {
                _networkDriveService.CancelNetworkDrive(folderPath);
            }

            if (isDownloaded)
            {
                var contentBuilder = new StringBuilder();
                contentBuilder.AppendLine($"Stop-Service -Name \"{_hostServiceConfig.Name}\"");
                contentBuilder.AppendLine("Get-Process -Name \"RemoteMaster.Host\" -ErrorAction SilentlyContinue | Stop-Process -Force");
                contentBuilder.AppendLine("$filesLocked = $true");
                contentBuilder.AppendLine("while ($filesLocked) {");
                contentBuilder.AppendLine("    Start-Sleep -Seconds 2");
                contentBuilder.AppendLine("    $filesLocked = $false");
                contentBuilder.AppendLine("    Get-ChildItem \"$PSScriptRoot\\Update\" -Recurse | Where-Object { !$_.PSIsContainer } | ForEach-Object {");
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
                contentBuilder.AppendLine("Start-Sleep -Seconds 2");
                contentBuilder.AppendLine($"Start-Service -Name \"{_hostServiceConfig.Name}\"");
                contentBuilder.AppendLine("Start-Sleep -Seconds 2");
                contentBuilder.AppendLine($"Remove-Item -Path \"{_updateFolderPath}\" -Recurse -Force");
                contentBuilder.AppendLine($"Remove-Item -Path \"{_scriptPath}\" -Force");

                File.WriteAllText(_scriptPath, contentBuilder.ToString());
                Log.Information("Updater script created at: {ScriptPath}", _scriptPath);

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{_scriptPath}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Log.Information("Executed updater script: {ScriptPath}", _scriptPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error while update host: {Message}", ex.Message);
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

            if (!File.Exists(destPath) || overwriteExisting)
            {
                try
                {
                    file.CopyTo(destPath, true);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        if (copySubDirs)
        {
            foreach (var subdir in sourceDir.GetDirectories())
            {
                var destSubDir = Path.Combine(destDirName, subdir.Name);

                if (!DirectoryCopy(subdir.FullName, destSubDir, true, overwriteExisting))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
