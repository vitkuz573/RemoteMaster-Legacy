// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Text;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Helpers;
using RemoteMaster.Host.Models;

namespace RemoteMaster.Host.Services;

public class UpdaterService : IUpdaterService
{
    private const string SHARED_FOLDER = @"\\SERVER-DC02\Win\RemoteMaster";
    private const string LOGIN = "support@it-ktk.local";
    private const string PASSWORD = "bonesgamer123!!";

    private readonly string _baseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");
    private readonly string _scriptPath;
    private readonly string _updateFolderPath;

    private readonly HostServiceConfig _config;
    private readonly ILogger<UpdaterService> _logger;

    public UpdaterService(HostServiceConfig config, ILogger<UpdaterService> logger)
    {
        _scriptPath = Path.Combine(_baseFolderPath, "update.ps1");
        _updateFolderPath = Path.Combine(_baseFolderPath, "Update");

        _config = config;
        _logger = logger;
    }

    public void Download()
    {
        try
        {
            var sourceFolder = Path.Combine(SHARED_FOLDER, "Host");

            NetworkDriveHelper.MapNetworkDrive(SHARED_FOLDER, LOGIN, PASSWORD);
            _logger.LogInformation($"Mapped network drive: {SHARED_FOLDER}");

            NetworkDriveHelper.DirectoryCopy(sourceFolder, _updateFolderPath, true, true);
            _logger.LogInformation($"Copied from {sourceFolder} to {_updateFolderPath}");

            NetworkDriveHelper.CancelNetworkDrive(SHARED_FOLDER);
            _logger.LogInformation($"Unmapped network drive: {SHARED_FOLDER}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in Download method: {ex.Message}");
        }
    }

    public void Execute()
    {
        try
        {
            var contentBuilder = new StringBuilder();
            contentBuilder.AppendLine("Stop-Service -Name \"" + _config.Name + "\"");
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
            contentBuilder.AppendLine("Start-Service -Name \"" + _config.Name + "\"");

            File.WriteAllText(_scriptPath, contentBuilder.ToString());
            _logger.LogInformation($"Updater script created at: {_scriptPath}");

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

            _logger.LogInformation("Executed updater script: {ScriptPath}", _scriptPath);
            _logger.LogInformation("{Output}", output);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in Execute method: {ex.Message}");
        }
    }

    public void Clean()
    {
        try
        {
            if (File.Exists(_scriptPath))
            {
                File.Delete(_scriptPath);
                _logger.LogInformation("Updater script successfully deleted.");
            }
            else
            {
                _logger.LogInformation("Updater script doesn't exists");
            }

            if (Directory.Exists(_updateFolderPath))
            {
                Directory.Delete(_updateFolderPath, true);
                _logger.LogInformation("Update folder successfully deleted.");
            }
            else
            {
                _logger.LogInformation("Update folder doesn't exists");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in Clean method: {ex.Message}");
        }
    }
}
