// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class InstanceStarterService(INativeProcessFactory nativeProcessFactory, IFileSystem fileSystem) : IInstanceStarterService
{
    public int StartNewInstance(string executablePath, string? destinationPath, NativeProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        try
        {
            if (destinationPath != null)
            {
                var destinationDirectory = fileSystem.Path.GetDirectoryName(destinationPath);

                if (destinationDirectory != null && !fileSystem.Directory.Exists(destinationDirectory))
                {
                    Log.Information("Creating directory {DestinationDirectory} for the executable.", destinationDirectory);
                    fileSystem.Directory.CreateDirectory(destinationDirectory);
                }

                Log.Information("Copying executable from {ExecutablePath} to {DestinationPath}", executablePath, destinationPath);
                fileSystem.File.Copy(executablePath, destinationPath, true);
                Log.Information("Successfully copied the executable.");

                executablePath = destinationPath;
            }

            var process = nativeProcessFactory.Create();

            startInfo.FileName = executablePath;
            process.StartInfo = startInfo;

            process.Start();
            Log.Information("Started a new instance of the host with NativeProcess. Process ID: {ProcessId}", process.Id);

            return process.Id;
        }
        catch (IOException ioEx)
        {
            Log.Error(ioEx, "IO error occurred while copying the executable. Source: {SourcePath}, Destination: {DestinationPath}", executablePath, destinationPath);

            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {Path}", executablePath);

            throw;
        }
    }
}