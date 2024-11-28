// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class InstanceManagerService(INativeProcessFactory nativeProcessFactory, IFileSystem fileSystem, ILogger<InstanceManagerService> logger) : IInstanceManagerService
{
    public int StartNewInstance(string? destinationPath, INativeProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        var executablePath = PrepareExecutable(destinationPath);

        var process = nativeProcessFactory.Create();
        startInfo.ProcessStartInfo.FileName = executablePath;
        process.StartInfo = startInfo;

        try
        {
            process.Start();
            logger.LogInformation("Started a new instance of the host with NativeProcess. Process ID: {ProcessId}", process.Id);

            return process.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting new instance of the host. Executable path: {Path}", executablePath);
            throw;
        }
    }

    public int StartNewInstance(string? destinationPath, ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        var executablePath = PrepareExecutable(destinationPath);

        var process = new Process { StartInfo = startInfo };
        process.StartInfo.FileName = executablePath;

        try
        {
            process.Start();
            logger.LogInformation("Started a new instance of the host with Process. Process ID: {ProcessId}", process.Id);
            return process.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting new instance of the host. Executable path: {Path}", executablePath);
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

        var destinationDirectory = fileSystem.Path.GetDirectoryName(destinationPath);

        if (destinationDirectory != null && !fileSystem.Directory.Exists(destinationDirectory))
        {
            logger.LogInformation("Creating directory {DestinationDirectory} for the executable.", destinationDirectory);
            fileSystem.Directory.CreateDirectory(destinationDirectory);
        }

        try
        {
            logger.LogInformation("Copying executable from {ExecutablePath} to {DestinationPath}", executablePath, destinationPath);
            fileSystem.File.Copy(executablePath, destinationPath, true);
            logger.LogInformation("Successfully copied the executable.");
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
