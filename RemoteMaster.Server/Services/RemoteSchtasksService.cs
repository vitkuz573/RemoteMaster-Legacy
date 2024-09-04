// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class RemoteSchtasksService(INetworkDriveService networkDriveService) : IRemoteSchtasksService
{
    public Result CopyAndExecuteRemoteFile(string sourceFilePath, string remoteMachineName, string destinationFolderPath, string? username = null, string? password = null, string? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(sourceFilePath);
        ArgumentNullException.ThrowIfNull(remoteMachineName);
        ArgumentNullException.ThrowIfNull(destinationFolderPath);

        try
        {
            var adminShareResult = IsAdministrativeShareAvailable(remoteMachineName, username, password);
            
            if (adminShareResult.IsFailed)
            {
                return Result.Fail($"Administrative share C$ on {remoteMachineName} is not available.");
            }

            var remoteSharePath = $@"\\{remoteMachineName}\C$";
            var mapResult = networkDriveService.MapNetworkDrive(remoteSharePath, username, password);

            if (mapResult.IsFailed)
            {
                return Result.Fail($"Failed to map network drive. {mapResult.Errors.First().Message}");
            }

            var fullRemoteFilePath = Path.Combine(remoteSharePath, destinationFolderPath, Path.GetFileName(sourceFilePath));
            
            File.Copy(sourceFilePath, fullRemoteFilePath, true);

            var executeResult = ExecuteRemoteFile(remoteMachineName, fullRemoteFilePath, username, password, arguments);
            
            if (executeResult.IsFailed)
            {
                return executeResult;
            }

            var cancelResult = networkDriveService.CancelNetworkDrive(remoteSharePath);
            
            return cancelResult.IsFailed ? Result.Fail($"Failed to cancel network drive. {cancelResult.Errors.First().Message}") : Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error: {Message}", ex.Message);

            return Result.Fail($"Error: {ex.Message}").WithError(ex.Message);
        }
    }

    private Result IsAdministrativeShareAvailable(string remoteMachineName, string? username, string? password)
    {
        try
        {
            var sharePath = $@"\\{remoteMachineName}\C$";
            var mapResult = networkDriveService.MapNetworkDrive(sharePath, username, password);

            if (mapResult.IsFailed)
            {
                return Result.Fail("Failed to map network drive.");
            }

            var cancelResult = networkDriveService.CancelNetworkDrive(sharePath);
            
            return cancelResult.IsFailed ? Result.Fail("Failed to cancel network drive.") : Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error checking administrative share: {ex.Message}").WithError(ex.Message);
        }
    }

    private static Result ExecuteRemoteFile(string remoteMachineName, string remoteFilePath, string? username, string? password, string? arguments)
    {
        try
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

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to execute remote file. {ex.Message}").WithError(ex.Message);
        }
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
