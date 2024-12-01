// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.EventArguments;

namespace RemoteMaster.Host.Core.Services;

public class InstanceManagerService(INativeProcessFactory nativeProcessFactory, IProcessWrapperFactory processWrapperFactory, IFileService fileService, ILogger<InstanceManagerService> logger) : IInstanceManagerService
{
    public event EventHandler<InstanceStartedEventArgs>? InstanceStarted;

    public int StartNewInstance(string? destinationPath, LaunchModeBase launchMode, ProcessStartInfo startInfo, INativeProcessOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(launchMode);
        ArgumentNullException.ThrowIfNull(startInfo);

        var executablePath = PrepareExecutable(destinationPath);

        startInfo.FileName = executablePath;
        startInfo.Arguments = GenerateArguments(launchMode);

        var process = options != null
            ? nativeProcessFactory.Create(options)
            : processWrapperFactory.Create();

        var processId = StartProcess(process, startInfo);

        OnInstanceStarted(new InstanceStartedEventArgs(processId, launchMode));

        return processId;
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

    private static string GenerateArguments(LaunchModeBase launchMode)
    {
        var arguments = new List<string>
        {
            $"--launch-mode={launchMode.Name.ToLower()}"
        };

        arguments.AddRange(launchMode.Parameters
            .GroupBy(p => p.Value)
            .Select(g =>
            {
                var parameter = g.First();
                var value = parameter.Value.Value;

                if (value is null)
                {
                    if (parameter.Value.IsRequired)
                    {
                        return $"--{parameter.Key}=";
                    }

                    return null;
                }

                if (value is bool boolValue)
                {
                    return boolValue ? $"--{parameter.Key}" : null;
                }

                if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                {
                    return parameter.Value.IsRequired ? $"--{parameter.Key}=" : null;
                }

                return $"--{parameter.Key}={value}";
            })
            .Where(arg => arg is not null)
            .Cast<string>());

        return string.Join(" ", arguments);
    }

    protected virtual void OnInstanceStarted(InstanceStartedEventArgs e)
    {
        InstanceStarted?.Invoke(this, e);
    }
}
