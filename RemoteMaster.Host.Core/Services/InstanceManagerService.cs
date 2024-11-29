// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class InstanceManagerService(INativeProcessFactory nativeProcessFactory, IProcessWrapperFactory processWrapperFactory, IFileService fileService, ILogger<InstanceManagerService> logger) : IInstanceManagerService
{
    public int StartNewInstance(string? destinationPath, ProcessStartInfo startInfo, INativeProcessOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        var executablePath = PrepareExecutable(destinationPath);
        startInfo.FileName = executablePath;

        var process = options != null
            ? nativeProcessFactory.Create(options)
            : processWrapperFactory.Create();

        return StartProcess(process, startInfo);
    }

    private int StartProcess(IProcess process, ProcessStartInfo startInfo)
    {
        try
        {
            process.Start(startInfo);
            logger.LogInformation("Started a new instance of the host. Process ID: {ProcessId}, Process Type: {ProcessType}", process.Id, process.GetType().Name);

            return process.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting a new instance of the host. Process Type: {ProcessType}", process.GetType().Name);
            throw;
        }
    }

    private string PrepareExecutable(string? destinationPath)
    {
        var executablePath = Environment.ProcessPath!;

        if (destinationPath == null)
        {
            return executablePath;
        }

        try
        {
            fileService.CopyFile(executablePath, destinationPath, true);
            executablePath = destinationPath;
        }
        catch (IOException ioEx)
        {
            logger.LogError(ioEx, "IO error occurred while copying the executable. Source: {SourcePath}, Destination: {DestinationPath}", executablePath, destinationPath);
            throw;
        }

        return executablePath;
    }
}
