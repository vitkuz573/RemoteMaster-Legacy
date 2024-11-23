// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Runtime.InteropServices;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using Windows.Win32.System.Services;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class RemoteExecutionService(IFileSystem fileSystem, ILogger<RemoteExecutionService> logger) : IRemoteExecutionService
{
    public Result ExecuteApplication(string remoteMachineName, string localExecutablePath, string? args = null)
    {
        if (string.IsNullOrEmpty(remoteMachineName))
        {
            return Result.Fail("Remote machine name cannot be null or empty.");
        }

        if (string.IsNullOrEmpty(localExecutablePath))
        {
            return Result.Fail("Local executable path cannot be null or empty.");
        }

        if (!fileSystem.File.Exists(localExecutablePath))
        {
            return Result.Fail($"Local executable not found: {localExecutablePath}");
        }

        var adminShare = $@"\\{remoteMachineName}\ADMIN$\Temp";
        var remoteExecutablePath = fileSystem.Path.Combine(adminShare, fileSystem.Path.GetFileName(localExecutablePath));

        try
        {
            logger.LogInformation("Copying executable to remote machine: {RemotePath}", remoteExecutablePath);

            fileSystem.File.Copy(localExecutablePath, remoteExecutablePath, true);

            logger.LogInformation("Starting remote service for execution.");

            return CreateAndStartService(remoteMachineName, remoteExecutablePath, args);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during execution on {RemoteMachineName}.", remoteMachineName);

            return Result.Fail(new Error("An error occurred during execution.").CausedBy(ex));
        }
        finally
        {
            try
            {
                logger.LogInformation("Deleting executable from remote machine: {RemotePath}", remoteExecutablePath);

                fileSystem.File.Delete(remoteExecutablePath);
            }
            catch (Exception deleteEx)
            {
                logger.LogWarning(deleteEx, "Failed to delete executable from remote machine: {RemotePath}", remoteExecutablePath);
            }
        }
    }

    private unsafe Result CreateAndStartService(string remoteMachineName, string remoteExecutablePath, string? args)
    {
        using var scmHandle = OpenSCManager(remoteMachineName, null, SC_MANAGER_CREATE_SERVICE);

        if (scmHandle.IsInvalid || scmHandle.IsClosed)
        {
            var error = new Error("Failed to connect to the Service Control Manager.")
                .WithMetadata("RemoteMachineName", remoteMachineName);
            logger.LogError(error.Message);

            return Result.Fail(error);
        }

        const string serviceName = "RemoteExecutionService";

        var serviceExePath = $@"C:\Windows\Temp\{fileSystem.Path.GetFileName(remoteExecutablePath)}";

        using (var existingServiceHandle = OpenService(scmHandle, serviceName, SERVICE_ALL_ACCESS))
        {
            if (!existingServiceHandle.IsInvalid)
            {
                logger.LogInformation("Service {ServiceName} already exists. Deleting existing service.", serviceName);

                if (!DeleteService(existingServiceHandle))
                {
                    var deleteError = Marshal.GetLastWin32Error();
                    var deleteErrorMsg = new Error($"Failed to delete existing service. Error code: {deleteError}")
                        .WithMetadata("ServiceName", serviceName);

                    logger.LogError(deleteErrorMsg.Message);

                    return Result.Fail(deleteErrorMsg);
                }

                logger.LogInformation("Existing service {ServiceName} deleted successfully.", serviceName);
            }
        }

        var serviceExePathWithArgs = string.IsNullOrEmpty(args) ? serviceExePath : $"{serviceExePath} {args}";
        
        logger.LogInformation("Creating service {ServiceName} on remote machine with path {ServiceExePathWithArgs}.", serviceName, serviceExePathWithArgs);

        using var serviceHandle = CreateService(scmHandle, serviceName, serviceName, SERVICE_ALL_ACCESS, ENUM_SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS, SERVICE_START_TYPE.SERVICE_DEMAND_START, SERVICE_ERROR.SERVICE_ERROR_NORMAL, serviceExePathWithArgs, null, null, null, null, null);

        if (serviceHandle == null || serviceHandle.IsInvalid || serviceHandle.IsClosed)
        {
            var errorCode = Marshal.GetLastWin32Error();
            var error = new Error($"Failed to create service. Error code: {errorCode}")
                .WithMetadata("ServiceName", serviceName);

            logger.LogError(error.Message);

            return Result.Fail(error);
        }

        logger.LogInformation("Starting service {ServiceName}.", serviceName);

        if (!StartService((SC_HANDLE)serviceHandle.DangerousGetHandle(), 0, null))
        {
            var errorCode = Marshal.GetLastWin32Error();

            var error = new Error($"Failed to start service {serviceName}. Error code: {errorCode}");

            logger.LogError(error.Message);

            return Result.Fail(error);
        }
        
        logger.LogInformation("Deleting service {ServiceName} after start.", serviceName);

        var deleteResult = DeleteService(serviceHandle);

        if (!deleteResult)
        {
            var error = new Error("Failed to delete service after execution.");
            logger.LogWarning(error.Message);

            return Result.Fail(error);
        }

        return Result.Ok();
    }
}
