// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class InstanceManagerService(INativeProcessFactory nativeProcessFactory, IFileSystem fileSystem, ILogger<InstanceManagerService> logger) : IInstanceManagerService
{
    private string _executablePath = Environment.ProcessPath!;

    public int StartNewInstance(string? destinationPath, NativeProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        try
        {
            if (destinationPath != null)
            {
                var destinationDirectory = fileSystem.Path.GetDirectoryName(destinationPath);

                if (destinationDirectory != null && !fileSystem.Directory.Exists(destinationDirectory))
                {
                    logger.LogInformation("Creating directory {DestinationDirectory} for the executable.", destinationDirectory);
                    fileSystem.Directory.CreateDirectory(destinationDirectory);
                }

                logger.LogInformation("Copying executable from {ExecutablePath} to {DestinationPath}", _executablePath, destinationPath);
                fileSystem.File.Copy(_executablePath, destinationPath, true);
                logger.LogInformation("Successfully copied the executable.");

                _executablePath = destinationPath;
            }

            var process = nativeProcessFactory.Create();

            startInfo.FileName = _executablePath;
            process.StartInfo = startInfo;

            process.Start();
            logger.LogInformation("Started a new instance of the host with NativeProcess. Process ID: {ProcessId}", process.Id);

            return process.Id;
        }
        catch (IOException ioEx)
        {
            logger.LogError(ioEx, "IO error occurred while copying the executable. Source: {SourcePath}, Destination: {DestinationPath}", _executablePath, destinationPath);

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting new instance of the host. Executable path: {Path}", _executablePath);

            throw;
        }
    }
}
