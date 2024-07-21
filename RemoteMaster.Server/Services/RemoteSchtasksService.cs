// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class RemoteSchtasksService(INetworkDriveService networkDriveService) : IRemoteSchtasksService
{
    public bool CopyAndExecuteRemoteFile(string sourceFilePath, string remoteMachineName, string destinationFolderPath, string? username = null, string? password = null, string? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(sourceFilePath);
        ArgumentNullException.ThrowIfNull(remoteMachineName);
        ArgumentNullException.ThrowIfNull(destinationFolderPath);

        try
        {
            if (!IsAdministrativeShareAvailable(remoteMachineName, username, password))
            {
                throw new Exception($"Administrative share C$ on {remoteMachineName} is not available.");
            }

            var remoteSharePath = $@"\\{remoteMachineName}\C$";

            var mapResult = networkDriveService.MapNetworkDrive(remoteSharePath, username, password);
            
            if (!mapResult.IsSuccess)
            {
                throw new Exception($"Failed to map network drive. {mapResult.Errors.First().Message}");
            }

            var fullRemoteFilePath = Path.Combine(remoteSharePath, destinationFolderPath, Path.GetFileName(sourceFilePath));
            File.Copy(sourceFilePath, fullRemoteFilePath, true);

            ExecuteRemoteFile(remoteMachineName, fullRemoteFilePath, username, password, arguments);

            var cancelResult = networkDriveService.CancelNetworkDrive(remoteSharePath);
            
            if (!cancelResult.IsSuccess)
            {
                throw new Exception($"Failed to cancel network drive. {cancelResult.Errors.First().Message}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Error: {ex.Message}");
            
            return false;
        }
    }

    private bool IsAdministrativeShareAvailable(string remoteMachineName, string? username, string? password)
    {
        try
        {
            var sharePath = $@"\\{remoteMachineName}\C$";

            var mapResult = networkDriveService.MapNetworkDrive(sharePath, username, password);
            
            if (!mapResult.IsSuccess)
            {
                return false;
            }

            var cancelResult = networkDriveService.CancelNetworkDrive(sharePath);
            
            if (!cancelResult.IsSuccess)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ExecuteRemoteFile(string remoteMachineName, string remoteFilePath, string? username, string? password, string? arguments)
    {
        const string taskName = "RunRemoteFile";

        var taskCommand = $"/create /s {remoteMachineName} /tn {taskName} /tr \"{remoteFilePath} {arguments}\" /sc once /st 00:00 /ru \"SYSTEM\"";
        var runCommand = $"/run /s {remoteMachineName} /tn {taskName}";

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            taskCommand += $" /u {username} /p \"{password}\"";
            runCommand += $" /u {username} /p \"{password}\"";
        }

        ExecuteCommand("schtasks", taskCommand);
        ExecuteCommand("schtasks", runCommand);
    }

    private static void ExecuteCommand(string command, string arguments)
    {
        var processInfo = new ProcessStartInfo(command, arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };

        using var process = Process.Start(processInfo) ?? throw new Exception($"Failed to start process {command} with arguments {arguments}");
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command {command} {arguments} failed with exit code {process.ExitCode}");
        }
    }
}
